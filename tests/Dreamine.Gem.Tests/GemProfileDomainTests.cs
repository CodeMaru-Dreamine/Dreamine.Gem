using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Abstractions.States;
using Dreamine.Gem.Profiles;
using Dreamine.Gem.Services;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Xunit;

namespace Dreamine.Gem.Tests;

public sealed class GemProfileDomainTests
{
    [Fact]
    public void ProfileFreezesClosedCapabilitiesAndOrderedDefinitions()
    {
        var builder = CreateBuilder()
            .AddVariable(new(2, "Second", GemVariableKind.Data), SecsItemFormat.Ascii,
                _ => ValueTask.FromResult<SecsItem>(new SecsAsciiItem("x")))
            .AddVariable(new(1, "First", GemVariableKind.Status), SecsItemFormat.UInt8,
                _ => ValueTask.FromResult<SecsItem>(new SecsUInt8Item(1)))
            .AddEquipmentConstant(new(7, "Limit", new SecsUInt8Item(2)), SecsItemFormat.UInt8)
            .AddAlarm(new(9, 1, "Pressure"));

        var profile = builder.Build();

        Assert.Equal(GemEquipmentProfile.V1Capabilities, profile.Capabilities);
        Assert.Equal(new ulong[] { 1, 2 }, profile.Variables.Select(static value => value.Definition.Id));
        Assert.Throws<InvalidOperationException>(() => builder.AddAlarm(new(10, 1, "Late")));
        Assert.Throws<NotSupportedException>(() =>
            ((IList<GemProfileCapability>)profile.Capabilities).Add(GemProfileCapability.Clock));
    }

    [Fact]
    public void ProfileRejectsDuplicatesUndefinedReferencesAndIdentifierOverflow()
    {
        Assert.Throws<InvalidOperationException>(() => CreateBuilder()
            .AddVariable(new(1, "Same", GemVariableKind.Status), SecsItemFormat.UInt8,
                _ => ValueTask.FromResult<SecsItem>(new SecsUInt8Item()))
            .AddVariable(new(1, "Other", GemVariableKind.Data), SecsItemFormat.UInt8,
                _ => ValueTask.FromResult<SecsItem>(new SecsUInt8Item()))
            .Build());

        Assert.Throws<InvalidOperationException>(() => CreateBuilder()
            .AddVariable(new(1, "Same", GemVariableKind.Status), SecsItemFormat.UInt8,
                _ => ValueTask.FromResult<SecsItem>(new SecsUInt8Item()))
            .AddVariable(new(2, "Same", GemVariableKind.Data), SecsItemFormat.UInt8,
                _ => ValueTask.FromResult<SecsItem>(new SecsUInt8Item()))
            .Build());

        Assert.Throws<InvalidOperationException>(() => CreateBuilder()
            .AddReport(new(10, [999]))
            .Build());

        Assert.Throws<InvalidOperationException>(() => CreateBuilder()
            .AddCollectionEvent(new(20, "Changed", [999]))
            .Build());

        var narrowIds = new GemIdentifierFormatPolicy(
            [new(GemIdentifierFamily.StatusVariable, SecsItemFormat.UInt8)]);
        Assert.Throws<InvalidOperationException>(() => CreateBuilder(narrowIds)
            .AddVariable(new(256, "Overflow", GemVariableKind.Status), SecsItemFormat.UInt8,
                _ => ValueTask.FromResult<SecsItem>(new SecsUInt8Item()))
            .Build());
    }

    [Fact]
    public async Task ContextsFromOneProfileHaveCompletelyIsolatedMutableState()
    {
        var time = new ManualTimeProvider();
        var profile = CreateBuilder()
            .AddVariable(new(1, "State", GemVariableKind.Status), SecsItemFormat.UInt8,
                _ => ValueTask.FromResult<SecsItem>(new SecsUInt8Item(3)))
            .AddEquipmentConstant(new(2, "Limit", new SecsUInt8Item(2)), SecsItemFormat.UInt8)
            .AddReport(new(10, [1]))
            .AddReport(new(11, [1]))
            .AddCollectionEvent(new(20, "Changed", [10]))
            .AddAlarm(new(30, 1, "Pressure"))
            .Build();
        var left = profile.CreateContext(new FakeTransport(), time);
        var right = profile.CreateContext(new FakeTransport(), time);

        Assert.Equal(GemConstantSetStatus.Updated,
            left.Runtime.Constants.SetValue(2, new SecsUInt8Item(7), GemControlState.EquipmentOffline));
        Assert.Equal(GemAlarmChangeStatus.Changed, left.Runtime.Alarms.ChangeAlarm(30, true));
        Assert.True(left.Runtime.Alarms.SetEnabled(30, false));
        Assert.True(left.Runtime.Events.LinkReport(20, 11));
        left.Runtime.Control.AttemptOnline();
        left.Runtime.Clock.SetUtcNow(DateTimeOffset.UnixEpoch.AddDays(1));

        Assert.Equal((byte)7, Assert.IsType<SecsUInt8Item>(GetConstant(left.Runtime.Constants, 2)).Values.Span[0]);
        Assert.Equal((byte)2, Assert.IsType<SecsUInt8Item>(GetConstant(right.Runtime.Constants, 2)).Values.Span[0]);
        Assert.True(left.Runtime.Alarms.GetSnapshots().Single().IsSet);
        Assert.False(left.Runtime.Alarms.GetSnapshots().Single().Enabled);
        Assert.False(right.Runtime.Alarms.GetSnapshots().Single().IsSet);
        Assert.True(right.Runtime.Alarms.GetSnapshots().Single().Enabled);
        Assert.Equal(2, (await left.Runtime.Events.CollectAsync(20))!.Reports.Count);
        Assert.Single((await right.Runtime.Events.CollectAsync(20))!.Reports);
        Assert.Equal(GemControlState.AttemptOnline, left.Runtime.Control.State);
        Assert.Equal(GemControlState.EquipmentOffline, right.Runtime.Control.State);
        Assert.NotEqual(left.Runtime.Clock.GetUtcNow(), right.Runtime.Clock.GetUtcNow());
    }

    [Fact]
    public async Task ProfileEnforcesVariableAndEquipmentConstantFormats()
    {
        var profile = CreateBuilder()
            .AddVariable(new(1, "BadReader", GemVariableKind.Status), SecsItemFormat.UInt8,
                _ => ValueTask.FromResult<SecsItem>(new SecsAsciiItem("wrong")))
            .AddEquipmentConstant(new(2, "Limit", new SecsUInt8Item(2)), SecsItemFormat.UInt8)
            .Build();
        var context = profile.CreateContext(new FakeTransport());

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.Runtime.Variables.ReadAsync(1).AsTask());
        Assert.Equal(GemConstantSetStatus.ValidationFailed,
            context.Runtime.Constants.SetValue(2, new SecsAsciiItem("wrong"), GemControlState.EquipmentOffline));
    }

    [Fact]
    public void EquipmentConstantBatchStagesValidatesAndAppliesAtomically()
    {
        var service = new GemEquipmentConstantService();
        service.RegisterTyped(new(1, "A", new SecsUInt8Item(1)), SecsItemFormat.UInt8,
            item => ((SecsUInt8Item)item).Values.Span[0] < 10);
        service.RegisterTyped(new(2, "B", new SecsUInt8Item(2)), SecsItemFormat.UInt8,
            item => ((SecsUInt8Item)item).Values.Span[0] < 10);

        var failed = service.SetValues(
        [
            new(1, new SecsUInt8Item(3)),
            new(2, new SecsUInt8Item(99))
        ], GemControlState.OnlineRemote);

        Assert.Equal(GemConstantBatchStatus.ValidationFailed, failed.Status);
        Assert.Equal((ulong)2, failed.FailedId);
        Assert.Equal(new byte[] { 1, 2 }, service.GetSnapshots()
            .Select(static value => Assert.IsType<SecsUInt8Item>(value.Value).Values.Span[0]));

        var updated = service.SetValues(
        [
            new(1, new SecsUInt8Item(3)),
            new(2, new SecsUInt8Item(4))
        ], GemControlState.OnlineRemote);

        Assert.Equal(GemConstantBatchStatus.Updated, updated.Status);
        Assert.Equal(new byte[] { 3, 4 }, service.GetSnapshots()
            .Select(static value => Assert.IsType<SecsUInt8Item>(value.Value).Values.Span[0]));
    }

    [Fact]
    public async Task StructuredEventSnapshotPreservesReportOrderAndRepeatedVid()
    {
        var variables = new GemVariableCatalog();
        variables.Register(new(1, "State", GemVariableKind.Status), SecsItemFormat.UInt8,
            _ => ValueTask.FromResult<SecsItem>(new SecsUInt8Item(1)));
        variables.Register(new(2, "Data", GemVariableKind.Data), SecsItemFormat.UInt8,
            _ => ValueTask.FromResult<SecsItem>(new SecsUInt8Item(2)));
        var service = new GemEventReportService(variables);
        service.DefineReport(new(10, [2, 1]));
        service.DefineReport(new(11, [1]));
        service.DefineEvent(new(20, "Changed", [10, 11]));

        var snapshot = (await service.CollectAsync(20))!;

        Assert.Equal(new ulong[] { 10, 11 }, snapshot.Reports.Select(static report => report.ReportId));
        Assert.Equal(new ulong[] { 2, 1 }, snapshot.Reports[0].Values.Select(static value => value.VariableId));
        Assert.Equal(2, snapshot.Reports.Sum(static report => report.Values.Count(static value => value.VariableId == 1)));
        Assert.Equal(new ulong[] { 10, 11 }, service.GetReportDefinitions().Select(static report => report.Id));
    }

    [Fact]
    public void EventReportBatchConfigurationIsValidatedAndCommittedAtomically()
    {
        var variables = new GemVariableCatalog();
        variables.Register(new(1, "State", GemVariableKind.Status), SecsItemFormat.UInt8,
            _ => ValueTask.FromResult<SecsItem>(new SecsUInt8Item(1)));
        var service = new GemEventReportService(variables);
        service.DefineReport(new(10, [1]));
        service.DefineEvent(new(20, "First", [10]));
        service.DefineEvent(new(21, "Second"));
        service.DefineEvent(new(22, "Third"));

        var invalidReports = service.DefineReports([new(11, [1]), new(12, [999])]);
        Assert.Equal(GemEventConfigurationStatus.UnknownVariable, invalidReports.Status);
        Assert.Equal(new ulong[] { 10 }, service.GetReportDefinitions().Select(static value => value.Id));

        Assert.Equal(GemEventConfigurationStatus.Applied,
            service.DefineReports([new(11, [1]), new(12, [1])]).Status);
        Assert.Equal(GemEventConfigurationStatus.Applied,
            service.ApplyEventLinks([new(22, [11])], rejectExisting: true, disableUpdated: true).Status);
        var atomicallyDisabled = service.GetEventSnapshots().Single(static value => value.Definition.Id == 22);
        Assert.Equal(new ulong[] { 11 }, atomicallyDisabled.ReportIds);
        Assert.False(atomicallyDisabled.Enabled);
        Assert.Equal(GemEventConfigurationStatus.ExistingLinks,
            service.ApplyEventLinks([new(22, [12])], rejectExisting: true, disableUpdated: true).Status);
        Assert.Equal(GemEventConfigurationStatus.Applied, service.DeleteAllEventLinks().Status);
        Assert.All(service.GetEventSnapshots(), static value => Assert.Empty(value.ReportIds));
        Assert.Equal(GemEventConfigurationStatus.Applied, service.SetEventsEnabled([new(22, true)]).Status);
        var invalidLinks = service.ReplaceEventLinks(
        [
            new(20, [11]),
            new(21, [999])
        ]);
        Assert.Equal(GemEventConfigurationStatus.UnknownReport, invalidLinks.Status);
        Assert.Empty(service.GetEventSnapshots().Single(static value => value.Definition.Id == 20).ReportIds);
        Assert.Empty(service.GetEventSnapshots().Single(static value => value.Definition.Id == 21).ReportIds);

        Assert.Equal(GemEventConfigurationStatus.Applied,
            service.ReplaceEventLinks([new(20, [11, 12]), new(21, [10])]).Status);
        Assert.Equal(GemEventConfigurationStatus.UnknownEvent,
            service.SetEventsEnabled([new(20, false), new(999, false)]).Status);
        Assert.All(service.GetEventSnapshots(), static value => Assert.True(value.Enabled));

        var mixed = service.ApplyReportChanges([new(13, [1])], [10]);
        Assert.Equal(GemEventConfigurationStatus.Applied, mixed.Status);
        Assert.Equal(new ulong[] { 11, 12, 13 }, service.GetReportDefinitions().Select(static value => value.Id));
        Assert.Empty(service.GetEventSnapshots().Single(static value => value.Definition.Id == 21).ReportIds);

        var invalidMixed = service.ApplyReportChanges([new(14, [999])], [11]);
        Assert.Equal(GemEventConfigurationStatus.UnknownVariable, invalidMixed.Status);
        Assert.Equal(new ulong[] { 11, 12, 13 }, service.GetReportDefinitions().Select(static value => value.Id));

        Assert.Equal(GemEventConfigurationStatus.Applied, service.DeleteAllReports().Status);
        Assert.Empty(service.GetReportDefinitions());
        Assert.All(service.GetEventSnapshots(), static value => Assert.Empty(value.ReportIds));
    }

    [Fact]
    public async Task TypedRemoteCommandValidatesRequiredOptionalFormatRangeTimeoutAndCancellation()
    {
        var service = new GemRemoteCommandService();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var definition = new GemRemoteCommandProfileDefinition("START",
        [
            new("PORT", SecsItemFormat.Ascii, validator: item => ((SecsAsciiItem)item).Value.Length <= 4),
            new("COUNT", SecsItemFormat.UInt8, required: false,
                validator: item => ((SecsUInt8Item)item).Values.Span[0] <= 3)
        ]);
        service.RegisterProfileCommand(definition, async (_, token) =>
        {
            entered.TrySetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            finally { cancelled.TrySetResult(); }
            return new(GemCommandStatus.Completed);
        });

        Assert.Equal(GemCommandStatus.InvalidParameter,
            (await service.ExecuteAsync("START", new Dictionary<string, SecsItem>(), TimeSpan.FromSeconds(1))).Status);
        Assert.Equal(GemCommandStatus.InvalidParameter,
            (await service.ExecuteAsync("START", new Dictionary<string, SecsItem>
            {
                ["PORT"] = new SecsUInt8Item(1)
            }, TimeSpan.FromSeconds(1))).Status);
        Assert.Equal(GemCommandStatus.InvalidParameter,
            (await service.ExecuteAsync("START", new Dictionary<string, SecsItem>
            {
                ["PORT"] = new SecsAsciiItem("ABCDE")
            }, TimeSpan.FromSeconds(1))).Status);

        using var callerCancellation = new CancellationTokenSource();
        var execution = service.ExecuteAsync("START", new Dictionary<string, SecsItem>
        {
            ["PORT"] = new SecsAsciiItem("A")
        }, TimeSpan.FromSeconds(30), callerCancellation.Token).AsTask();
        await entered.Task;
        callerCancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task RemoteCommandTimeoutCancelsTheHandlerTokenAndObservesCompletion()
    {
        var time = new ManualTimeProvider();
        var cancellationObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new GemRemoteCommandService(time);
        service.RegisterProfileCommand(new("WAIT"), async (_, token) =>
        {
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            finally { cancellationObserved.TrySetResult(); }
            return new(GemCommandStatus.Completed);
        });

        var operation = service.ExecuteAsync("WAIT", new Dictionary<string, SecsItem>(), TimeSpan.FromSeconds(5)).AsTask();
        time.Advance(TimeSpan.FromSeconds(5));

        Assert.Equal(GemCommandStatus.Failed, (await operation).Status);
        await cancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AlarmSnapshotsAndChangeResultsAreDeterministicAndOrdered()
    {
        var service = new GemAlarmService();
        service.Register(new(2, 1, "Second", false));
        service.Register(new(1, 1, "First"));

        Assert.Equal(GemAlarmChangeStatus.Changed, service.ChangeEnabled(2, true));
        Assert.Equal(GemAlarmChangeStatus.NoChange, service.ChangeEnabled(2, true));
        Assert.Equal(GemAlarmChangeStatus.Unknown, service.ChangeEnabled(99, true));
        Assert.Equal(GemAlarmChangeStatus.Changed, service.ChangeAlarm(2, true));
        var snapshots = service.GetSnapshots();
        Assert.Equal(new ulong[] { 1, 2 }, snapshots.Select(static value => value.Definition.Id));
        Assert.True(snapshots[1].Enabled);
        Assert.True(snapshots[1].IsSet);
        Assert.Throws<NotSupportedException>(() => ((IList<GemAlarmSnapshot>)snapshots).Add(snapshots[0]));
    }

    [Fact]
    public void OrderedSnapshotsRemainStableDuringConcurrentStateChanges()
    {
        var variables = new GemVariableCatalog();
        variables.Register(new(1, "State", GemVariableKind.Status), SecsItemFormat.UInt8,
            _ => ValueTask.FromResult<SecsItem>(new SecsUInt8Item(1)));
        var constants = new GemEquipmentConstantService();
        constants.RegisterTyped(new(2, "Second", new SecsUInt8Item(2)), SecsItemFormat.UInt8);
        constants.RegisterTyped(new(1, "First", new SecsUInt8Item(1)), SecsItemFormat.UInt8);
        var alarms = new GemAlarmService();
        alarms.Register(new(2, 1, "Second"));
        alarms.Register(new(1, 1, "First"));
        var events = new GemEventReportService(variables);
        events.DefineReport(new(2, [1]));
        events.DefineReport(new(1, [1]));
        events.DefineEvent(new(2, "Second", [2]));
        events.DefineEvent(new(1, "First", [1]));

        Parallel.For(0, 256, iteration =>
        {
            constants.SetValue(1, new SecsUInt8Item((byte)iteration), GemControlState.OnlineRemote);
            alarms.ChangeAlarm(2, (iteration & 1) == 0);
            events.SetAllEventsEnabled((iteration & 1) == 0);
            Assert.Equal(new ulong[] { 1, 2 }, constants.GetSnapshots().Select(static value => value.Definition.Id));
            Assert.Equal(new ulong[] { 1, 2 }, alarms.GetSnapshots().Select(static value => value.Definition.Id));
            Assert.Equal(new ulong[] { 1, 2 }, events.GetReportDefinitions().Select(static value => value.Id));
            Assert.Equal(new ulong[] { 1, 2 }, events.GetEventSnapshots().Select(static value => value.Definition.Id));
        });
    }

    private static GemEquipmentProfileBuilder CreateBuilder(GemIdentifierFormatPolicy? identifiers = null) =>
        new("E30-0611 subset", new("MODEL", "1.0"), "E30-0611-derived subset v1", identifiers);

    private static SecsItem GetConstant(GemEquipmentConstantService service, ulong id)
    {
        Assert.True(service.TryGetValue(id, out var value));
        return value!;
    }

    private sealed class FakeTransport : IGemMessageTransport
    {
        private uint _systemBytes;
        public ISecsConnection Connection { get; } = new FakeConnection();
        public SecsSessionId SessionId { get; } = new(1);
        public event EventHandler<SecsMessage>? MessageReceived;
        public SecsSystemBytes AllocateSystemBytes() => new(++_systemBytes);
        public Task SendAsync(SecsMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<SecsMessage> RequestAsync(SecsMessage message, CancellationToken cancellationToken = default) =>
            Task.FromException<SecsMessage>(new NotSupportedException());
        public void Raise(SecsMessage message) => MessageReceived?.Invoke(this, message);
    }

    private sealed class FakeConnection : ISecsConnection
    {
        public string ProviderKey => "test";
        public ConnectionState State => ConnectionState.Connected;
        public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
