using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Abstractions.States;
using Dreamine.Gem.Host;
using Dreamine.Gem.Profiles;
using Dreamine.Gem.Protocol.E30;
using Dreamine.Gem.Transport;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Abstractions.Validation;
using Dreamine.Secs.Com.Hsms;
using Xunit;

namespace Dreamine.Gem.Tests;

public sealed class E30RouterLoopbackTests
{
    [Fact]
    public async Task HostAndEquipmentExerciseLocallySupportedDialoguesAcrossActualTcp()
    {
        await using var pair = await LoopbackPair.CreateAsync();
        var commandCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var profile = CreateProfile(commandCompleted);
        var context = profile.CreateContext(new HsmsGemTransport(pair.Equipment));
        await using var router = new E30EquipmentRouter(pair.Equipment, context, new E30EquipmentRouterOptions
        {
            CommandCompletionEvents = new Dictionary<string, ulong>(StringComparer.Ordinal) { ["START"] = 101 },
            CommandTimeout = TimeSpan.FromSeconds(2)
        });
        using var host = new E30HostClient(pair.Host, new E30IdentifierPolicy(profile.IdentifierFormats));

        var notCommunicating = await host.ReadStatusAsync([1], pair.Token);
        Assert.Equal(E30CallOutcome.TimedOut, notCommunicating.Outcome);

        var establish = await host.EstablishCommunicationsAsync(pair.Token);
        Assert.True(establish.IsAcknowledged);
        Assert.Equal("MODEL", establish.Value!.ModelNumber);

        var offlineStatus = await host.ReadStatusAsync([1], pair.Token);
        Assert.Equal(E30CallOutcome.FunctionZero, offlineStatus.Outcome);
        Assert.Equal((byte)1, (await host.RequestOnlineAsync(pair.Token)).Acknowledgement);
        context.Runtime.Control.AttemptOnline();
        Assert.Equal(E30CallOutcome.Completed, (await router.AreYouThereAsync(pair.Token)).Outcome);
        Assert.Equal(GemControlState.OnlineLocal, context.Runtime.Control.State);

        using (var canceled = new CancellationTokenSource())
        {
            canceled.Cancel();
            Assert.Equal(E30CallOutcome.Canceled, (await host.AreYouThereAsync(canceled.Token)).Outcome);
        }

        Assert.Equal(E30CallOutcome.Completed, (await router.AreYouThereAsync(pair.Token)).Outcome);
        var hostTime = await router.ReadHostTimeAsync(pair.Token);
        Assert.Equal(E30CallOutcome.Completed, hostTime.Outcome);
        Assert.InRange((hostTime.Value - TimeProvider.System.GetUtcNow()).Duration(), TimeSpan.Zero, TimeSpan.FromSeconds(2));

        var identity = await host.AreYouThereAsync(pair.Token);
        Assert.Equal("REV", identity.Value!.SoftwareRevision);
        var status = await host.ReadStatusAsync([1], pair.Token);
        Assert.Equal(42u, Assert.IsType<SecsUInt32Item>(Assert.Single(status.Value!)).Values.Span[0]);
        var names = await host.ReadStatusVariableNamesAsync([1], pair.Token);
        Assert.Equal("Temperature", Assert.Single(names.Value!).Name);

        var constants = await host.ReadEquipmentConstantsAsync([5], pair.Token);
        Assert.Equal((ushort)10, Assert.IsType<SecsUInt16Item>(Assert.Single(constants.Value!)).Values.Span[0]);
        var rejectedBatch = await host.SetEquipmentConstantsAsync(
            [KeyValuePair.Create<ulong, SecsItem>(5, new SecsUInt16Item(50)), KeyValuePair.Create<ulong, SecsItem>(6, new SecsUInt16Item(1))], pair.Token);
        Assert.Equal((byte)1, rejectedBatch.Acknowledgement);
        Assert.Equal((ushort)10, Assert.IsType<SecsUInt16Item>(Assert.Single((await host.ReadEquipmentConstantsAsync([5], pair.Token)).Value!)).Values.Span[0]);
        Assert.True((await host.SetEquipmentConstantsAsync([KeyValuePair.Create<ulong, SecsItem>(5, new SecsUInt16Item(25))], pair.Token)).IsAcknowledged);
        Assert.Equal("SetPoint", Assert.Single((await host.ReadEquipmentConstantNamesAsync([5], pair.Token)).Value!).Name);

        var before = await host.ReadTimeAsync(pair.Token);
        Assert.Equal(E30CallOutcome.Completed, before.Outcome);
        var targetTime = new DateTimeOffset(2028, 2, 29, 12, 34, 56, TimeSpan.Zero).AddMilliseconds(780);
        Assert.True((await host.SetTimeAsync(targetTime, cancellationToken: pair.Token)).IsAcknowledged);
        var after = await host.ReadTimeAsync(pair.Token);
        Assert.InRange((after.Value - targetTime).Duration(), TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        var invalidTimeReply = await pair.Host.RequestAsync(E30Dialogues.S2F31, new SecsAsciiItem("2023022901020300"), pair.Token);
        Assert.Equal((byte)1, E30WireCodec.ReadAcknowledgement(invalidTimeReply.Item));
        var afterInvalid = await host.ReadTimeAsync(pair.Token);
        Assert.InRange((afterInvalid.Value - targetTime).Duration(), TimeSpan.Zero, TimeSpan.FromMilliseconds(150));

        Assert.True((await host.DefineReportsAsync(1, [new E30ReportDefinition(11, [1]), new E30ReportDefinition(12, [1])], pair.Token)).IsAcknowledged);
        var rejectedReports = await host.DefineReportsAsync(2, [new E30ReportDefinition(13, [1]), new E30ReportDefinition(14, [999])], pair.Token);
        Assert.Equal((byte)4, rejectedReports.Acknowledgement);
        Assert.DoesNotContain(router.GetReportDefinitions(), static value => value.ReportId is 13 or 14);
        Assert.True((await host.LinkEventReportsAsync(1, [new E30EventLink(101, [12, 11])], pair.Token)).IsAcknowledged);
        Assert.True((await host.SetEventEnablementAsync(true, [101], pair.Token)).IsAcknowledged);
        var rejectedRelink = await host.LinkEventReportsAsync(2, [new E30EventLink(101, [11])], pair.Token);
        Assert.Equal((byte)3, rejectedRelink.Acknowledgement);
        Assert.Equal(E30CallOutcome.FunctionZero,
            (await host.LinkEventReportsAsync(3, [new E30EventLink(101, [])], pair.Token)).Outcome);
        Assert.Equal(E30CallOutcome.FunctionZero,
            (await host.LinkEventReportsAsync(4, [], pair.Token)).Outcome);
        Assert.Equal([12ul, 11ul], Assert.Single(router.GetEventLinks(), static value => value.CollectionEventId == 101).ReportIds);
        var requestedEvent = await host.ReadEventReportAsync(101, pair.Token);
        Assert.Equal(101ul, requestedEvent.Value!.CollectionEventId);
        Assert.Equal([12ul, 11ul], requestedEvent.Value.Reports.Select(static value => value.ReportId));
        Assert.All(requestedEvent.Value.Reports,
            static report => Assert.Equal(42u, Assert.IsType<SecsUInt32Item>(Assert.Single(report.Values)).Values.Span[0]));

        var localCommand = await host.SendRemoteCommandAsync("START", [], pair.Token);
        Assert.Equal((byte)2, localCommand.Acknowledgement);
        context.Runtime.Control.SelectRemote();
        var completionEvent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.EventReportReceived += (_, report) => { if (report.CollectionEventId == 101) completionEvent.TrySetResult(); };
        var acceptedCommand = await host.SendRemoteCommandAsync("START", [], pair.Token);
        Assert.Equal((byte)4, acceptedCommand.Acknowledgement);
        await commandCompleted.Task.WaitAsync(pair.Token);
        await completionEvent.Task.WaitAsync(pair.Token);

        Assert.True((await host.SetAlarmEnablementAsync(false, 20, pair.Token)).IsAcknowledged);
        Assert.Single((await host.ReadAlarmsAsync([20], pair.Token)).Value!);
        Assert.True((await host.SetAlarmEnablementAsync(true, 20, pair.Token)).IsAcknowledged);
        var ordering = new ConcurrentQueue<string>();
        var alarmReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var alarmEventReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.AlarmReceived += (_, alarm) => { if (alarm.AlarmId == 20) { ordering.Enqueue("S5F1"); alarmReceived.TrySetResult(); } };
        host.EventReportReceived += (_, report) => { if (report.CollectionEventId == 101) { ordering.Enqueue("S6F11"); alarmEventReceived.TrySetResult(); } };
        Assert.True((await router.PublishAlarmChangeAsync(20, true, 101, pair.Token)).IsAcknowledged);
        await alarmReceived.Task.WaitAsync(pair.Token);
        await alarmEventReceived.Task.WaitAsync(pair.Token);
        Assert.Equal(["S5F1", "S6F11"], ordering.ToArray());

        Assert.True((await host.RequestOfflineAsync(pair.Token)).IsAcknowledged);
        Assert.Equal(GemControlState.HostOffline, context.Runtime.Control.State);
        Assert.Equal(E30CallOutcome.FunctionZero, (await host.ReadStatusAsync([1], pair.Token)).Outcome);
        Assert.True((await host.RequestOnlineAsync(pair.Token)).IsAcknowledged);
        Assert.Equal(GemControlState.OnlineLocal, context.Runtime.Control.State);
    }

    [Fact]
    public async Task MandatoryW1MutationIsRejectedBeforeStateChangeWhileOptionalS5F3W0Applies()
    {
        await using var pair = await LoopbackPair.CreateAsync();
        var profile = CreateProfile(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        var context = profile.CreateContext(new HsmsGemTransport(pair.Equipment));
        await using var router = new E30EquipmentRouter(pair.Equipment, context);
        using var host = new E30HostClient(pair.Host, new E30IdentifierPolicy(profile.IdentifierFormats));
        Assert.True((await host.EstablishCommunicationsAsync(pair.Token)).IsAcknowledged);
        context.Runtime.Control.AttemptOnline();
        Assert.Equal(E30CallOutcome.Completed, (await router.AreYouThereAsync(pair.Token)).Outcome);

        var streamNine = new TaskCompletionSource<SecsMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        pair.Host.MessageReceived += (_, message) =>
        {
            if (message.Stream.Value == 9 && message.Function.Value == 7) streamNine.TrySetResult(message);
        };
        await pair.Host.SendAsync(new SecsStream(2), new SecsFunction(15),
            E30WireCodec.EquipmentConstantUpdates(
                [KeyValuePair.Create<ulong, SecsItem>(5, new SecsUInt16Item(77))],
                new E30IdentifierPolicy(profile.IdentifierFormats)), pair.Token);
        var illegal = await streamNine.Task.WaitAsync(pair.Token);

        Assert.Equal(10, Assert.IsType<SecsBinaryItem>(illegal.Item).Count);
        Assert.Equal((ushort)10, Assert.IsType<SecsUInt16Item>(
            context.Runtime.Constants.GetSnapshots().Single(static value => value.Definition.Id == 5).Value).Values.Span[0]);

        await pair.Host.SendAsync(new SecsStream(5), new SecsFunction(3),
            E30WireCodec.AlarmEnablement(false, 20, new E30IdentifierPolicy(profile.IdentifierFormats)), pair.Token);
        await WaitUntilAsync(() => !context.Runtime.Alarms.GetSnapshots().Single(static value => value.Definition.Id == 20).Enabled, pair.Token);
    }

    [Fact]
    public async Task CommandWithoutCompletionEventMappingIsRejectedBeforeQueueing()
    {
        await using var pair = await LoopbackPair.CreateAsync();
        var commandCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var profile = CreateProfile(commandCompleted);
        var context = profile.CreateContext(new HsmsGemTransport(pair.Equipment));
        await using var router = new E30EquipmentRouter(pair.Equipment, context);
        using var host = new E30HostClient(pair.Host, new E30IdentifierPolicy(profile.IdentifierFormats));
        Assert.True((await host.EstablishCommunicationsAsync(pair.Token)).IsAcknowledged);
        context.Runtime.Control.AttemptOnline();
        Assert.Equal(E30CallOutcome.Completed, (await router.AreYouThereAsync(pair.Token)).Outcome);
        context.Runtime.Control.SelectRemote();

        var result = await host.SendRemoteCommandAsync("START", [], pair.Token);

        Assert.Equal((byte)2, result.Acknowledgement);
        Assert.False(commandCompleted.Task.IsCompleted);
    }

    [Fact]
    public async Task ValidZeroLengthS6F16IsRepresentedWithoutMalformedOutcome()
    {
        await using var pair = await LoopbackPair.CreateAsync();
        var profile = CreateProfile(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        var context = profile.CreateContext(new HsmsGemTransport(pair.Equipment));
        await using var router = new E30EquipmentRouter(pair.Equipment, context);
        using var host = new E30HostClient(pair.Host, new E30IdentifierPolicy(profile.IdentifierFormats));
        Assert.True((await host.EstablishCommunicationsAsync(pair.Token)).IsAcknowledged);
        context.Runtime.Control.AttemptOnline();
        Assert.Equal(E30CallOutcome.Completed, (await router.AreYouThereAsync(pair.Token)).Outcome);

        var result = await host.ReadEventReportAsync(999, pair.Token);

        Assert.Equal(E30CallOutcome.Completed, result.Outcome);
        Assert.Equal(999ul, result.Value!.CollectionEventId);
        Assert.Empty(result.Value.Reports);
    }

    [Fact]
    public async Task SuppressedAlarmPublicationDoesNotFabricatePeerAcknowledgement()
    {
        await using var pair = await LoopbackPair.CreateAsync();
        var profile = CreateProfile(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        var context = profile.CreateContext(new HsmsGemTransport(pair.Equipment));
        await using var router = new E30EquipmentRouter(pair.Equipment, context);
        Assert.Equal(GemAlarmChangeStatus.Changed, context.Runtime.Alarms.ChangeEnabled(20, false));

        var result = await router.PublishAlarmChangeAsync(20, true, cancellationToken: pair.Token);

        Assert.Equal(E30CallOutcome.NotSent, result.Outcome);
        Assert.Null(result.Acknowledgement);
        Assert.True(context.Runtime.Alarms.GetSnapshots().Single(static value => value.Definition.Id == 20).IsSet);
    }

    [Fact]
    public async Task OutboundEventIsStateGatedBeforeAnyWireRequest()
    {
        await using var pair = await LoopbackPair.CreateAsync();
        var profile = CreateProfile(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        var context = profile.CreateContext(new HsmsGemTransport(pair.Equipment));
        await using var router = new E30EquipmentRouter(pair.Equipment, context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => router.PublishEventAsync(100, pair.Token));
    }

    [Fact]
    public async Task ObservableFundamentalErrorsEmitCorrelatedS9AcrossActualTcp()
    {
        await using var pair = await LoopbackPair.CreateAsync();
        var profile = CreateProfile(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        var context = profile.CreateContext(new HsmsGemTransport(pair.Equipment));
        await using var router = new E30EquipmentRouter(pair.Equipment, context, new E30EquipmentRouterOptions { MaximumSingleBlockBodyBytes = 4 });
        using var host = new E30HostClient(pair.Host, new E30IdentifierPolicy(profile.IdentifierFormats));
        Assert.Equal(E30CallOutcome.TimedOut, (await host.ReadStatusAsync([1], pair.Token)).Outcome);
        Assert.True((await host.EstablishCommunicationsAsync(pair.Token)).IsAcknowledged);
        Assert.Equal(E30CallOutcome.FunctionZero, (await host.ReadStatusAsync([1], pair.Token)).Outcome);
        var offlineUnknown = await pair.Host.RequestAsync(
            new SecsDialogueDefinition(new SecsStream(1), new SecsFunction(5), new SecsFunction(6)), null, pair.Token);
        Assert.Equal((byte)0, offlineUnknown.Function.Value);
        Assert.Equal((byte)1, (await host.RequestOnlineAsync(pair.Token)).Acknowledgement);
        context.Runtime.Control.AttemptOnline();
        Assert.Equal(E30CallOutcome.Completed, (await router.AreYouThereAsync(pair.Token)).Outcome);
        var received = new ConcurrentQueue<SecsMessage>();
        pair.Host.MessageReceived += (_, message) => { if (message.Stream.Value == 9) received.Enqueue(message); };

        var unknownStream = new SecsMessage(new SecsSessionId(7), new SecsStream(4), new SecsFunction(1), false,
            pair.Host.AllocateSystemBytes());
        await pair.Host.SendAsync(unknownStream, pair.Token);
        await WaitUntilAsync(() => received.Any(static value => value.Function.Value == 3), pair.Token);
        AssertCorrelatedStreamNine(received.Single(static value => value.Function.Value == 3), unknownStream, 3);

        var unknownFunction = new SecsMessage(new SecsSessionId(7), new SecsStream(1), new SecsFunction(5), false,
            pair.Host.AllocateSystemBytes());
        await pair.Host.SendAsync(unknownFunction, pair.Token);
        await WaitUntilAsync(() => received.Any(static value => value.Function.Value == 5), pair.Token);
        AssertCorrelatedStreamNine(received.Single(static value => value.Function.Value == 5), unknownFunction, 5);

        var malformed = new SecsMessage(new SecsSessionId(7), new SecsStream(1), new SecsFunction(3), true,
            pair.Host.AllocateSystemBytes(), new SecsAsciiItem("X"));
        var malformedRequest = pair.Host.SendPrimaryAsync(malformed, pair.Token);
        await WaitUntilAsync(() => received.Any(static value => value.Function.Value == 7), pair.Token);
        AssertCorrelatedStreamNine(received.Single(static value => value.Function.Value == 7), malformed, 7);
        await Assert.ThrowsAsync<SecsTransactionTimeoutException>(() => malformedRequest);

        var oversized = new SecsMessage(
            new SecsSessionId(7), new SecsStream(1), new SecsFunction(3), true,
            pair.Host.AllocateSystemBytes(), new SecsListItem(new SecsUInt32Item(1)));
        var oversizedRequest = pair.Host.SendPrimaryAsync(oversized, pair.Token);
        await WaitUntilAsync(() => received.Any(static value => value.Function.Value == 11), pair.Token);
        AssertCorrelatedStreamNine(received.Single(static value => value.Function.Value == 11), oversized, 11);
        await Assert.ThrowsAsync<SecsTransactionTimeoutException>(() => oversizedRequest);
    }

    [Fact]
    public async Task EquipmentInitiatedCommunicationUsesSameExactDialogueAcrossActualTcp()
    {
        await using var pair = await LoopbackPair.CreateAsync();
        var profile = CreateProfile(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        var context = profile.CreateContext(new HsmsGemTransport(pair.Equipment));
        await using var router = new E30EquipmentRouter(pair.Equipment, context);
        using var host = new E30HostClient(pair.Host, new E30IdentifierPolicy(profile.IdentifierFormats));

        var result = await router.EstablishCommunicationsAsync(pair.Token);

        Assert.True(result.IsAcknowledged);
        Assert.Equal(GemCommunicationState.EnabledCommunicating, context.Runtime.Communication.State);
    }

    [Fact]
    public async Task SimultaneousCommunicationEstablishmentConvergesAcrossActualTcp()
    {
        await using var pair = await LoopbackPair.CreateAsync();
        var profile = CreateProfile(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        var context = profile.CreateContext(new HsmsGemTransport(pair.Equipment));
        await using var router = new E30EquipmentRouter(pair.Equipment, context);
        using var host = new E30HostClient(pair.Host, new E30IdentifierPolicy(profile.IdentifierFormats));

        var equipmentRequest = router.EstablishCommunicationsAsync(pair.Token);
        var hostRequest = host.EstablishCommunicationsAsync(pair.Token);
        await Task.WhenAll(equipmentRequest, hostRequest);

        Assert.True(equipmentRequest.Result.IsAcknowledged);
        Assert.True(hostRequest.Result.IsAcknowledged);
        Assert.Equal(GemCommunicationState.EnabledCommunicating, context.Runtime.Communication.State);
    }

    [Fact]
    public void SharedFrozenProfileCreatesIsolatedWireAndDomainContexts()
    {
        var profile = CreateProfile(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        var first = profile.CreateContext(new FakeTransport());
        var second = profile.CreateContext(new FakeTransport());

        Assert.Equal(GemConstantBatchStatus.Updated, first.Runtime.Constants.SetValues([new GemEquipmentConstantUpdate(5, new SecsUInt16Item(33))], GemControlState.OnlineRemote).Status);

        Assert.Equal((ushort)33, Assert.IsType<SecsUInt16Item>(first.Runtime.Constants.GetSnapshots().Single().Value).Values.Span[0]);
        Assert.Equal((ushort)10, Assert.IsType<SecsUInt16Item>(second.Runtime.Constants.GetSnapshots().Single().Value).Values.Span[0]);
        Assert.Same(profile, first.Profile);
        Assert.Same(profile, second.Profile);
    }

    private static GemEquipmentProfile CreateProfile(TaskCompletionSource commandCompleted) => new GemEquipmentProfileBuilder(
            E30DerivedSubsetManifest.ProfileName, new GemEquipmentIdentity("MODEL", "REV"))
        .AddVariable(new GemVariableDefinition(1, "Temperature", GemVariableKind.Status, units: "C"), SecsItemFormat.UInt32,
            static _ => ValueTask.FromResult<SecsItem>(new SecsUInt32Item(42)))
        .AddEquipmentConstant(new GemEquipmentConstantDefinition(5, "SetPoint", new SecsUInt16Item(10), units: "C",
                minimumValue: new SecsUInt16Item(0), maximumValue: new SecsUInt16Item(100)), SecsItemFormat.UInt16,
            static value => value is SecsUInt16Item item && item.Count == 1 && item.Values.Span[0] <= 100,
            [GemControlState.OnlineLocal, GemControlState.OnlineRemote])
        .AddReport(new GemReportDefinition(10, [1]))
        .AddCollectionEvent(new GemCollectionEventDefinition(100, "Initial", [10], enabled: true))
        .AddCollectionEvent(new GemCollectionEventDefinition(101, "CommandCompleted", enabled: false))
        .AddAlarm(new GemAlarmDefinition(20, 4, "TEST ALARM"))
        .AddRemoteCommand(new GemRemoteCommandProfileDefinition("START"), (_, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            commandCompleted.TrySetResult();
            return ValueTask.FromResult(new GemCommandResult(GemCommandStatus.Completed));
        })
        .Build();

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        while (!condition()) { cancellationToken.ThrowIfCancellationRequested(); await Task.Yield(); }
    }

    private static void AssertCorrelatedStreamNine(SecsMessage error, SecsMessage offending, byte function)
    {
        Assert.Equal(offending.SessionId, error.SessionId);
        Assert.Equal(offending.SystemBytes, error.SystemBytes);
        Assert.Equal((byte)9, error.Stream.Value);
        Assert.Equal(function, error.Function.Value);
        Assert.Equal(E30WireCodec.MessageHeader(offending).Values.ToArray(),
            Assert.IsType<SecsBinaryItem>(error.Item).Values.ToArray());
    }

    private sealed class FakeTransport : Dreamine.Gem.Abstractions.Interfaces.IGemMessageTransport
    {
        public Dreamine.Secs.Abstractions.Interfaces.ISecsConnection Connection { get; } = new FakeConnection();
        public SecsSessionId SessionId { get; } = new(7);
        public event EventHandler<SecsMessage>? MessageReceived;
        public Task SendAsync(SecsMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<SecsMessage> RequestAsync(SecsMessage message, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public SecsSystemBytes AllocateSystemBytes() => new(1);
        public void Raise(SecsMessage message) => MessageReceived?.Invoke(this, message);
    }

    private sealed class FakeConnection : Dreamine.Secs.Abstractions.Interfaces.ISecsConnection
    {
        public string ProviderKey => "fake";
        public ConnectionState State => ConnectionState.Disconnected;
        public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class LoopbackPair : IAsyncDisposable
    {
        private readonly CancellationTokenSource _timeout = new(TimeSpan.FromSeconds(30));
        private LoopbackPair(HsmsSession equipment, HsmsSession host) { Equipment = equipment; Host = host; }
        public HsmsSession Equipment { get; }
        public HsmsSession Host { get; }
        public CancellationToken Token => _timeout.Token;

        public static async Task<LoopbackPair> CreateAsync()
        {
            var port = ReservePort();
            var timers = new HsmsTimerOptions { T3 = TimeSpan.FromSeconds(1) };
            var equipment = new HsmsSession(new HsmsSessionOptions { Host = "127.0.0.1", Port = port, Mode = SecsConnectionMode.Passive, Role = SecsRole.Equipment, SessionId = new SecsSessionId(7), Timers = timers });
            var host = new HsmsSession(new HsmsSessionOptions { Host = "127.0.0.1", Port = port, Mode = SecsConnectionMode.Active, Role = SecsRole.Host, SessionId = new SecsSessionId(7), Timers = timers });
            var pair = new LoopbackPair(equipment, host);
            try
            {
                var passive = equipment.ConnectAsync(pair.Token);
                while (equipment.State != ConnectionState.Listening) { pair.Token.ThrowIfCancellationRequested(); await Task.Yield(); }
                await host.ConnectAsync(pair.Token);
                await passive;
                await host.SelectAsync(pair.Token);
                return pair;
            }
            catch { await pair.DisposeAsync(); throw; }
        }

        public async ValueTask DisposeAsync()
        {
            _timeout.Cancel();
            try { await Host.DisposeAsync(); }
            finally { await Equipment.DisposeAsync(); _timeout.Dispose(); }
        }

        private static int ReservePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
