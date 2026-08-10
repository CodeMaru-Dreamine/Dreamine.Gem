using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Abstractions.States;
using Dreamine.Gem.Services;
using Dreamine.Secs.Abstractions.Model;
using Xunit;

namespace Dreamine.Gem.Tests;

public sealed class ServiceTests
{
    [Fact]
    public async Task VariableCatalogReadsTypedValueAndFiltersDefinitions()
    {
        var catalog = new GemVariableCatalog();
        catalog.Register(new(1, "State", GemVariableKind.Status), _ => ValueTask.FromResult<SecsItem>(new SecsUInt8Item(7)));
        catalog.Register(new(2, "Data", GemVariableKind.Data), _ => ValueTask.FromResult<SecsItem>(new SecsAsciiItem("x")));
        var value = Assert.IsType<SecsUInt8Item>(await catalog.ReadAsync(1));
        Assert.Equal((byte)7, value.Values.Span[0]);
        Assert.Single(catalog.GetDefinitions(GemVariableKind.Status));
    }

    [Fact]
    public async Task VariableCatalogRejectsDuplicateAndUnknownRead()
    {
        var catalog = new GemVariableCatalog();
        catalog.Register(new(1, "State", GemVariableKind.Status), _ => ValueTask.FromResult<SecsItem>(new SecsUInt8Item()));
        Assert.Throws<InvalidOperationException>(() => catalog.Register(new(1, "Again", GemVariableKind.Status), _ => ValueTask.FromResult<SecsItem>(new SecsUInt8Item())));
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await catalog.ReadAsync(9));
    }

    [Fact]
    public void EquipmentConstantValidatesUpdates()
    {
        var service = new GemEquipmentConstantService();
        service.Register(new(1, "Limit", new SecsUInt8Item(2)), item => item is SecsUInt8Item value && value.Values.Span[0] < 5);
        Assert.False(service.TrySetValue(1, new SecsUInt8Item(9)));
        Assert.True(service.TrySetValue(1, new SecsUInt8Item(4)));
        Assert.True(service.TryGetValue(1, out var current));
        Assert.Equal((byte)4, Assert.IsType<SecsUInt8Item>(current).Values.Span[0]);
    }

    [Fact]
    public void EquipmentConstantReportsMetadataValidationAndStatePolicy()
    {
        var minimum = new SecsUInt8Item(1); var maximum = new SecsUInt8Item(5);
        var definition = new GemEquipmentConstantDefinition(1, "Limit", new SecsUInt8Item(2), minimumValue: minimum, maximumValue: maximum);
        var service = new GemEquipmentConstantService();
        service.Register(definition, item => item is SecsUInt8Item value && value.Values.Span[0] is >= 1 and <= 5, state => state is GemControlState.OnlineRemote);
        Assert.Same(minimum, definition.MinimumValue); Assert.Same(maximum, definition.MaximumValue);
        Assert.Equal(GemConstantSetStatus.ValidationFailed, service.SetValue(1, new SecsUInt8Item(9), GemControlState.OnlineRemote));
        Assert.Equal(GemConstantSetStatus.PolicyDenied, service.SetValue(1, new SecsUInt8Item(3), GemControlState.OnlineLocal));
        Assert.Equal(GemConstantSetStatus.Updated, service.SetValue(1, new SecsUInt8Item(3), GemControlState.OnlineRemote));
        Assert.Equal(GemConstantSetStatus.Unknown, service.SetValue(9, new SecsUInt8Item(3), GemControlState.OnlineRemote));
    }

    [Fact]
    public async Task EventReportCollectsDistinctLinkedVariablesAtInjectedTime()
    {
        var time = new ManualTimeProvider();
        var variables = new GemVariableCatalog();
        variables.Register(new(1, "State", GemVariableKind.Status), _ => ValueTask.FromResult<SecsItem>(new SecsUInt8Item(3)));
        var service = new GemEventReportService(variables, time);
        service.DefineReport(new(10, new ulong[] { 1 })); service.DefineReport(new(11, new ulong[] { 1 }));
        service.DefineEvent(new(20, "Changed", new ulong[] { 10, 11 }));
        time.Advance(TimeSpan.FromMinutes(1));
        var snapshot = await service.CollectAsync(20);
        Assert.NotNull(snapshot); Assert.Single(snapshot.Values); Assert.Equal(DateTimeOffset.UnixEpoch.AddMinutes(1), snapshot.OccurredAt);
    }

    [Fact]
    public async Task DisabledEventDoesNotReadVariables()
    {
        var reads = 0; var variables = new GemVariableCatalog();
        variables.Register(new(1, "State", GemVariableKind.Status), _ => { reads++; return ValueTask.FromResult<SecsItem>(new SecsUInt8Item()); });
        var service = new GemEventReportService(variables); service.DefineReport(new(1, new ulong[] { 1 })); service.DefineEvent(new(2, "Event", new ulong[] { 1 }, false));
        Assert.Null(await service.CollectAsync(2)); Assert.Equal(0, reads);
    }

    [Fact]
    public async Task EventReportsCanBeLinkedUnlinkedAndDeletedSafely()
    {
        var variables = new GemVariableCatalog();
        variables.Register(new(1, "State", GemVariableKind.Status), _ => ValueTask.FromResult<SecsItem>(new SecsUInt8Item(1)));
        var service = new GemEventReportService(variables);
        service.DefineReport(new(1, new ulong[] { 1 })); service.DefineEvent(new(2, "Changed"));
        Assert.True(service.LinkReport(2, 1)); Assert.False(service.DeleteReport(1));
        Assert.Single((await service.CollectAsync(2))!.Values);
        Assert.True(service.UnlinkReport(2, 1)); Assert.True(service.DeleteReport(1));
        Assert.Empty((await service.CollectAsync(2))!.Values);
    }

    [Fact]
    public async Task EventCollectionHonorsCancellation()
    {
        var variables = new GemVariableCatalog();
        variables.Register(new(1, "State", GemVariableKind.Status), async token =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new SecsUInt8Item(1);
        });
        var service = new GemEventReportService(variables); service.DefineReport(new(1, new ulong[] { 1 })); service.DefineEvent(new(2, "Changed", new ulong[] { 1 }));
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CollectAsync(2, cancellation.Token).AsTask());
    }

    [Fact]
    public void AlarmSetStateIsIndependentOfEnablement()
    {
        var service = new GemAlarmService(); service.Register(new(1, 4, "Pressure", false));
        Assert.True(service.SetAlarm(1, true)); Assert.True(service.TryGetState(1, out var isSet)); Assert.True(isSet);
        Assert.True(service.SetEnabled(1, true)); Assert.True(service.TryGetState(1, out isSet)); Assert.True(isSet);
    }

    [Fact]
    public void AlarmChangeDistinguishesDuplicateAndListsSetAlarms()
    {
        var service = new GemAlarmService(); service.Register(new(2, 4, "Second")); service.Register(new(1, 4, "First"));
        Assert.Equal(GemAlarmChangeStatus.Changed, service.ChangeAlarm(2, true));
        Assert.Equal(GemAlarmChangeStatus.NoChange, service.ChangeAlarm(2, true));
        Assert.Equal(GemAlarmChangeStatus.Unknown, service.ChangeAlarm(9, true));
        Assert.Equal(new ulong[] { 2 }, service.GetSetAlarmIds());
    }

    [Fact]
    public async Task RemoteCommandValidatesParametersAndCompletes()
    {
        var service = new GemRemoteCommandService();
        service.Register(new("START", new[] { "PORT" }), (values, _) => ValueTask.FromResult(new GemCommandResult(GemCommandStatus.Completed, ((SecsAsciiItem)values["PORT"]).Value)));
        var invalid = await service.ExecuteAsync("START", new Dictionary<string, SecsItem>(), TimeSpan.FromSeconds(1));
        var valid = await service.ExecuteAsync("START", new Dictionary<string, SecsItem> { ["PORT"] = new SecsAsciiItem("A") }, TimeSpan.FromSeconds(1));
        Assert.Equal(GemCommandStatus.InvalidParameter, invalid.Status); Assert.Equal(GemCommandStatus.Completed, valid.Status); Assert.Equal("A", valid.Detail);
    }

    [Fact]
    public async Task RemoteCommandUsesInjectedTimeout()
    {
        var time = new ManualTimeProvider(); var completion = new TaskCompletionSource<GemCommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new GemRemoteCommandService(time); service.Register(new("WAIT"), (_, _) => new(completion.Task));
        var operation = service.ExecuteAsync("WAIT", new Dictionary<string, SecsItem>(), TimeSpan.FromSeconds(5)).AsTask();
        time.Advance(TimeSpan.FromSeconds(5));
        Assert.Equal(GemCommandStatus.Failed, (await operation).Status);
    }

    [Fact]
    public async Task RemoteCommandRequiresOnlineRemoteAndGeneralizesHandlerFailure()
    {
        var state = GemControlState.OnlineLocal;
        var service = new GemRemoteCommandService(controlState: () => state);
        service.Register(new("START"), (_, _) => throw new InvalidOperationException("private detail"));
        Assert.Equal(GemCommandStatus.NotAllowed, (await service.ExecuteAsync("START", new Dictionary<string, SecsItem>(), TimeSpan.FromSeconds(1))).Status);
        state = GemControlState.OnlineRemote;
        var failed = await service.ExecuteAsync("START", new Dictionary<string, SecsItem>(), TimeSpan.FromSeconds(1));
        Assert.Equal(GemCommandStatus.Failed, failed.Status); Assert.DoesNotContain("private detail", failed.Detail);
    }

    [Fact]
    public async Task RemoteCommandReceivesStableReadOnlyParameterSnapshot()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IReadOnlyDictionary<string, SecsItem>? observed = null;
        var service = new GemRemoteCommandService();
        service.Register(new GemRemoteCommandDefinition("START", ["LOT"]), async (parameters, cancellationToken) =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(cancellationToken);
            observed = parameters;
            return new GemCommandResult(GemCommandStatus.Completed);
        });
        var callerOwned = new Dictionary<string, SecsItem>(StringComparer.Ordinal) { ["LOT"] = new SecsAsciiItem("A") };

        var execution = service.ExecuteAsync("START", callerOwned, TimeSpan.FromSeconds(5)).AsTask();
        await entered.Task;
        callerOwned["LOT"] = new SecsAsciiItem("B");
        release.TrySetResult();

        Assert.Equal(GemCommandStatus.Completed, (await execution).Status);
        Assert.Equal("A", Assert.IsType<SecsAsciiItem>(observed!["LOT"]).Value);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, SecsItem>)observed).Add("EXTRA", new SecsAsciiItem("X")));
    }

    [Fact]
    public void ProcessProgramStoreCopiesOnPutAndGet()
    {
        var service = new GemProcessProgramService(); byte[] body = [1, 2]; service.Put(new("P1", body)); body[0] = 8;
        Assert.True(service.TryGet("P1", out var program)); Assert.Equal((byte)1, program!.Body.Span[0]); Assert.Equal(new[] { "P1" }, service.GetIds()); Assert.True(service.Delete("P1"));
    }

    [Fact]
    public void ClockFormatsInjectedTimeAndLogicalSet()
    {
        var time = new ManualTimeProvider(); var clock = new GemClockService(time);
        clock.SetUtcNow(new DateTimeOffset(2026, 8, 10, 12, 34, 56, TimeSpan.Zero).AddMilliseconds(780));
        Assert.Equal("2026081012345678", clock.Format()); Assert.Equal("260810123456", clock.Format(false));
    }

    [Fact]
    public async Task SpoolOverwritesOldestAndDrainsInOrder()
    {
        var spool = new GemSpoolService(2); spool.Start();
        Assert.False(spool.Enqueue(Message(1))); Assert.False(spool.Enqueue(Message(2))); Assert.True(spool.Enqueue(Message(3)));
        var sent = new List<uint>(); await spool.DrainAsync((message, _) => { sent.Add(message.SystemBytes.Value); return Task.CompletedTask; });
        Assert.Equal(new uint[] { 2, 3 }, sent); Assert.Equal(GemSpoolState.Disabled, spool.State);
    }

    [Fact]
    public async Task SpoolKeepsFailedHeadForRetry()
    {
        var spool = new GemSpoolService(2); spool.Start(); spool.Enqueue(Message(1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => spool.DrainAsync((_, _) => throw new InvalidOperationException("send")));
        Assert.Equal(1, spool.Count); Assert.Equal(GemSpoolState.Spooling, spool.State);
    }

    [Fact]
    public async Task SpoolCallbackMayPurgeReentrantlyWithoutCorruptingQueue()
    {
        var service = new GemSpoolService(2);
        service.Start();
        service.Enqueue(Message(1));

        await service.DrainAsync((_, _) =>
        {
            service.Purge();
            return Task.CompletedTask;
        });

        Assert.Equal(0, service.Count);
        Assert.Equal(GemSpoolState.Disabled, service.State);
    }

    [Fact]
    public async Task RequeuedSameMessageInstanceIsNotMistakenForDeliveredEntry()
    {
        var service = new GemSpoolService(2);
        var message = Message(1);
        service.Start();
        service.Enqueue(message);

        await service.DrainAsync((_, _) =>
        {
            service.Purge();
            service.Start();
            service.Enqueue(message);
            return Task.CompletedTask;
        });

        Assert.Equal(1, service.Count);
        Assert.Equal(GemSpoolState.Spooling, service.State);
    }

    private static SecsMessage Message(uint systemBytes) => new(new(1), new(6), new(11), false, new(systemBytes));
}
