using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Gem.Abstractions.States;
using Dreamine.Gem.Demo;
using Dreamine.Gem.Host;
using Dreamine.Gem.Protocol.E30;
using Dreamine.Gem.Transport;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Hsms;

namespace Dreamine.Gem.QuickStart;

internal static class E30DemoSample
{
    private const ulong DynamicReportId = 11;

    public static async Task<int> RunAsync(string[] arguments)
    {
        try
        {
            var options = DemoSampleOptions.Parse(arguments);
            if (options.ShowHelp)
            {
                DemoSampleOptions.PrintHelp();
                return 0;
            }

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(options.TimeoutSeconds));
            ConsoleCancelEventHandler cancel = (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                timeout.Cancel();
            };
            Console.CancelKeyPress += cancel;
            try
            {
                Console.WriteLine($"Starting {options.Role} process, SessionId={options.SessionId}, profile='{E30DerivedSubsetManifest.ProfileName}'.");
                return options.Role == SecsRole.Host
                    ? await RunHostAsync(options, timeout.Token).ConfigureAwait(false)
                    : await RunEquipmentAsync(options, timeout.Token).ConfigureAwait(false);
            }
            finally
            {
                Console.CancelKeyPress -= cancel;
            }
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("FAIL: bounded sample timeout or cancellation elapsed.");
            return 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"FAIL: {exception.GetType().Name}: {exception.Message}");
            return 1;
        }
    }

    private static async Task<int> RunHostAsync(DemoSampleOptions options, CancellationToken cancellationToken)
    {
        await using var session = CreateSession(options, SecsConnectionMode.Active, SecsRole.Host);
        await using var wire = new DemoWireEvidence(session);
        using var host = new E30HostClient(session);
        var alarmReceived = new TaskCompletionSource<E30AlarmData>(TaskCreationOptions.RunContinuationsAsynchronously);
        var eventReceived = new TaskCompletionSource<E30EventReport>(TaskCreationOptions.RunContinuationsAsynchronously);
        host.AlarmReceived += (_, alarm) =>
        {
            if (alarm.AlarmId == E30DemoEquipmentProfile.AlarmId) alarmReceived.TrySetResult(alarm);
        };
        host.EventReportReceived += (_, report) =>
        {
            if (report.CollectionEventId == E30DemoEquipmentProfile.CommandCompletedEventId)
                eventReceived.TrySetResult(report);
        };

        await session.ConnectAsync(cancellationToken).ConfigureAwait(false);
        await session.SelectAsync(cancellationToken).ConfigureAwait(false);
        var checks = new List<string>();

        var timedOut = await host.ReadStatusAsync([E30DemoEquipmentProfile.EquipmentStateVariableId], cancellationToken).ConfigureAwait(false);
        RequireOutcome(timedOut, E30CallOutcome.TimedOut, "typed_timeout", checks);
        using (var canceled = new CancellationTokenSource())
        {
            canceled.Cancel();
            var canceledResult = await host.AreYouThereAsync(canceled.Token).ConfigureAwait(false);
            RequireOutcome(canceledResult, E30CallOutcome.Canceled, "typed_cancellation", checks);
        }

        var establish = await host.EstablishCommunicationsAsync(cancellationToken).ConfigureAwait(false);
        RequireAck(establish, 0, "S1F13/F14", checks);
        if (establish.Value?.ModelNumber != E30DemoEquipmentProfile.Create().Identity.ModelNumber)
            throw new InvalidOperationException("S1F14 did not return the selected Demo profile identity.");

        // EN: Equipment transitions itself OnlineRemote after observing the accepted communication.
        // KO: Equipment가 승인된 communication을 관찰한 뒤 스스로 OnlineRemote로 전환한다.
        await Task.Delay(TimeSpan.FromMilliseconds(1500), cancellationToken).ConfigureAwait(false);

        var identity = await host.AreYouThereAsync(cancellationToken).ConfigureAwait(false);
        RequireCompleted(identity, "S1F1/F2", checks);
        if (identity.Value?.SoftwareRevision != "1.0") throw new InvalidOperationException("Unexpected Demo software revision.");
        var status = await host.ReadStatusAsync([E30DemoEquipmentProfile.EquipmentStateVariableId], cancellationToken).ConfigureAwait(false);
        RequireCompleted(status, "S1F3/F4", checks);
        if (AssertSingle(status.Value, "S1F4") is not SecsAsciiItem { Value: "IDLE" })
            throw new InvalidOperationException("S1F4 did not preserve the typed Demo status value.");
        var names = await host.ReadStatusVariableNamesAsync([E30DemoEquipmentProfile.EquipmentStateVariableId], cancellationToken).ConfigureAwait(false);
        RequireCompleted(names, "S1F11/F12", checks);
        if (AssertSingle(names.Value, "S1F12").Name != "EquipmentState") throw new InvalidOperationException("Unexpected SVID metadata.");

        var constants = await host.ReadEquipmentConstantsAsync([E30DemoEquipmentProfile.BatchSizeConstantId], cancellationToken).ConfigureAwait(false);
        RequireCompleted(constants, "S2F13/F14", checks);
        if (AssertSingle(constants.Value, "S2F14") is not SecsUInt16Item initial || initial.Values.Span[0] != 10)
            throw new InvalidOperationException("Unexpected initial EC value.");
        RequireAck(await host.SetEquipmentConstantsAsync(
            [KeyValuePair.Create<ulong, SecsItem>(E30DemoEquipmentProfile.BatchSizeConstantId, new SecsUInt16Item(20))],
            cancellationToken).ConfigureAwait(false), 0, "S2F15/F16", checks);
        RequireCompleted(await host.ReadTimeAsync(cancellationToken).ConfigureAwait(false), "S2F17/F18", checks);
        var constantNames = await host.ReadEquipmentConstantNamesAsync([E30DemoEquipmentProfile.BatchSizeConstantId], cancellationToken).ConfigureAwait(false);
        RequireCompleted(constantNames, "S2F29/F30", checks);
        if (AssertSingle(constantNames.Value, "S2F30").Name != "DemoBatchSize") throw new InvalidOperationException("Unexpected EC metadata.");
        RequireAck(await host.SetTimeAsync(DateTimeOffset.UtcNow, cancellationToken: cancellationToken).ConfigureAwait(false), 0, "S2F31/F32", checks);

        RequireAck(await host.SetAlarmEnablementAsync(false, E30DemoEquipmentProfile.AlarmId, cancellationToken).ConfigureAwait(false), 0, "S5F3/F4-disable", checks);
        RequireAck(await host.SetAlarmEnablementAsync(true, E30DemoEquipmentProfile.AlarmId, cancellationToken).ConfigureAwait(false), 0, "S5F3/F4-enable", checks);
        var alarms = await host.ReadAlarmsAsync([E30DemoEquipmentProfile.AlarmId], cancellationToken).ConfigureAwait(false);
        RequireCompleted(alarms, "S5F5/F6", checks);
        _ = AssertSingle(alarms.Value, "S5F6");

        RequireAck(await host.DefineReportsAsync(1,
            [new E30ReportDefinition(DynamicReportId,
                [E30DemoEquipmentProfile.EquipmentStateVariableId, E30DemoEquipmentProfile.CompletedCountVariableId])],
            cancellationToken).ConfigureAwait(false), 0, "S2F33/F34", checks);
        RequireAck(await host.LinkEventReportsAsync(1,
            [new E30EventLink(E30DemoEquipmentProfile.CommandCompletedEventId, [DynamicReportId])],
            cancellationToken).ConfigureAwait(false), 0, "S2F35/F36", checks);
        RequireAck(await host.SetEventEnablementAsync(true,
            [E30DemoEquipmentProfile.CommandCompletedEventId], cancellationToken).ConfigureAwait(false), 0, "S2F37/F38", checks);

        _ = await alarmReceived.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        checks.Add("S5F1/F2:typed_alarm_ack0");
        Console.WriteLine("PASS S5F1/F2 typed alarm and ACKC5=0.");

        var command = await host.SendRemoteCommandAsync(
            E30DemoEquipmentProfile.StartCommand,
            [new E30CommandParameter("LOT", new SecsAsciiItem("DEMO"))],
            cancellationToken).ConfigureAwait(false);
        RequireAck(command, 4, "S2F41/F42-accepted", checks);
        _ = await eventReceived.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        checks.Add("S6F11/F12:typed_event_ack0");
        Console.WriteLine("PASS S6F11/F12 typed command-completion event and ACKC6=0.");

        var eventReport = await host.ReadEventReportAsync(E30DemoEquipmentProfile.StatusEventId, cancellationToken).ConfigureAwait(false);
        RequireCompleted(eventReport, "S6F15/F16", checks);
        if (eventReport.Value?.CollectionEventId != E30DemoEquipmentProfile.StatusEventId)
            throw new InvalidOperationException("Unexpected CEID in S6F16.");

        RequireAck(await host.RequestOfflineAsync(cancellationToken).ConfigureAwait(false), 0, "S1F15/F16", checks);
        RequireAck(await host.RequestOnlineAsync(cancellationToken).ConfigureAwait(false), 0, "S1F17/F18", checks);

        var summary = await wire.WaitForCompleteAsync(
            options.SessionId, SecsRole.Host, cancellationToken).ConfigureAwait(false);
        checks.Add("wire:nonzero_sid_wbit_systembytes_correlation");
        var evidence = new DemoProcessEvidence(
            "PASS", "Host", E30DerivedSubsetManifest.ProfileName, options.SessionId, checks, summary);
        await DemoEvidenceWriter.WriteAsync(options.EvidencePath, evidence, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"PASS Host: 20/20 dialogues, correlated={summary.CorrelatedTransactionCount}, expectedTimeouts={summary.ExpectedTimeoutCount}.");
        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        await session.DisconnectAsync(cancellationToken).ConfigureAwait(false);
        return 0;
    }

    private static async Task<int> RunEquipmentAsync(DemoSampleOptions options, CancellationToken cancellationToken)
    {
        await using var session = CreateSession(options, SecsConnectionMode.Passive, SecsRole.Equipment);
        await using var wire = new DemoWireEvidence(session);
        var profile = E30DemoEquipmentProfile.Create();
        var context = profile.CreateContext(new HsmsGemTransport(session));
        await using var router = new E30EquipmentRouter(session, context, new E30EquipmentRouterOptions
        {
            CommandCompletionEvents = new Dictionary<string, ulong>(StringComparer.Ordinal)
            {
                [E30DemoEquipmentProfile.StartCommand] = E30DemoEquipmentProfile.CommandCompletedEventId
            },
            CommandTimeout = TimeSpan.FromSeconds(3)
        });

        await session.ConnectAsync(cancellationToken).ConfigureAwait(false);
        await WaitForSelectedAsync(session, cancellationToken).ConfigureAwait(false);
        // Leave a deterministic T3 window so the Host can prove its typed timeout before S1F13.
        await Task.Delay(TimeSpan.FromMilliseconds(1500), cancellationToken).ConfigureAwait(false);
        await WaitUntilAsync(
            () => context.Runtime.Communication.State == GemCommunicationState.EnabledCommunicating,
            "Host S1F13 communication establishment",
            cancellationToken).ConfigureAwait(false);

        var checks = new List<string>();
        RequireCompleted(await router.AreYouThereAsync(cancellationToken).ConfigureAwait(false), "equipment S1F1/F2", checks);
        RequireCompleted(await router.ReadHostTimeAsync(cancellationToken).ConfigureAwait(false), "equipment S2F17/F18", checks);
        context.Runtime.Control.SelectRemote();
        checks.Add("control:OnlineRemote");

        await WaitUntilAsync(
            () => router.GetEventLinks().Any(link =>
                link.CollectionEventId == E30DemoEquipmentProfile.CommandCompletedEventId &&
                link.ReportIds.SequenceEqual([DynamicReportId])),
            "Host S2F35 event link",
            cancellationToken).ConfigureAwait(false);
        RequireAck(await router.PublishAlarmChangeAsync(
            E30DemoEquipmentProfile.AlarmId, true, cancellationToken: cancellationToken).ConfigureAwait(false),
            0,
            "equipment S5F1/F2",
            checks);

        var summary = await wire.WaitForCompleteAsync(
            options.SessionId, SecsRole.Equipment, cancellationToken).ConfigureAwait(false);
        checks.Add("wire:nonzero_sid_wbit_systembytes_correlation");
        var evidence = new DemoProcessEvidence(
            "PASS", "Equipment", E30DerivedSubsetManifest.ProfileName, options.SessionId, checks, summary);
        await DemoEvidenceWriter.WriteAsync(options.EvidencePath, evidence, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"PASS Equipment: 20/20 dialogues, correlated={summary.CorrelatedTransactionCount}, expectedTimeouts={summary.ExpectedTimeoutCount}.");
        await WaitForDisconnectedAsync(session, cancellationToken).ConfigureAwait(false);
        return 0;
    }

    private static HsmsSession CreateSession(DemoSampleOptions options, SecsConnectionMode mode, SecsRole role) =>
        new(new HsmsSessionOptions
        {
            Host = options.Host,
            Port = options.Port,
            Mode = mode,
            Role = role,
            SessionId = new SecsSessionId(options.SessionId),
            Timers = new HsmsTimerOptions { T3 = TimeSpan.FromSeconds(1) },
            WireObservation = new HsmsWireObservationOptions
            {
                QueueCapacity = 256,
                MaximumCapturedBytes = 14,
                DefaultCaptureMode = HsmsWireCaptureMode.HeaderOnly
            }
        });

    private static void RequireCompleted<T>(E30CallResult<T> result, string label, ICollection<string> checks) =>
        RequireOutcome(result, E30CallOutcome.Completed, label, checks);

    private static void RequireAck<T>(E30CallResult<T> result, byte expected, string label, ICollection<string> checks)
    {
        RequireCompleted(result, label, checks);
        if (result.Acknowledgement != expected)
            throw new InvalidOperationException($"{label} expected raw ACK {expected}, received {result.Acknowledgement?.ToString() ?? "none"}.");
        checks.Add($"{label}:raw_ack={expected}");
        Console.WriteLine($"PASS {label} raw ACK={expected}.");
    }

    private static void RequireOutcome<T>(E30CallResult<T> result, E30CallOutcome expected, string label, ICollection<string> checks)
    {
        if (result.Outcome != expected)
            throw new InvalidOperationException($"{label} expected {expected}, received {result.Outcome}.");
        checks.Add($"{label}:outcome={expected}");
        Console.WriteLine($"PASS {label} typed outcome={expected}.");
    }

    private static T AssertSingle<T>(IReadOnlyList<T>? values, string label)
    {
        if (values is null || values.Count != 1) throw new InvalidOperationException($"{label} expected one typed value.");
        return values[0];
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string label, CancellationToken cancellationToken)
    {
        while (!condition())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);
        }
        Console.WriteLine($"PASS {label} observed.");
    }

    private static async Task WaitForSelectedAsync(HsmsSession session, CancellationToken cancellationToken)
    {
        while (session.HsmsState != HsmsConnectionState.Selected)
        {
            if (session.State == ConnectionState.Disconnected)
                throw new InvalidOperationException("Peer disconnected before HSMS Selected.");
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task WaitForDisconnectedAsync(HsmsSession session, CancellationToken cancellationToken)
    {
        while (session.State != ConnectionState.Disconnected)
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);
    }
}
