using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Abstractions.States;
using Dreamine.Gem.Services;
using Dreamine.Secs.Abstractions.Model;

var variables = new GemVariableCatalog();
variables.Register(new GemVariableDefinition(1, "CommunicationState", GemVariableKind.Status),
    _ => ValueTask.FromResult<SecsItem>(new SecsAsciiItem("COMMUNICATING")));

var constants = new GemEquipmentConstantService();
constants.Register(new GemEquipmentConstantDefinition(100, "BatchSize", new SecsUInt16Item([10])),
    value => value is SecsUInt16Item item && item.Values.Span[0] is >= 1 and <= 100);
Console.WriteLine($"EC update: {constants.SetValue(100, new SecsUInt16Item([20]), GemControlState.OnlineRemote)}");

var events = new GemEventReportService(variables);
events.DefineReport(new GemReportDefinition(10, [1]));
events.DefineEvent(new GemCollectionEventDefinition(1000, "StateChanged", [10]));
var snapshot = await events.CollectAsync(1000);
Console.WriteLine($"Event {snapshot?.EventId} collected {snapshot?.Values.Count} value(s).");

var alarms = new GemAlarmService();
alarms.Register(new GemAlarmDefinition(2000, 1, "Sample alarm"));
alarms.SetAlarm(2000, true);
alarms.SetAlarm(2000, false);

var commands = new GemRemoteCommandService();
commands.Register(new GemRemoteCommandDefinition("START", ["LOT"]),
    (parameters, _) => ValueTask.FromResult(new GemCommandResult(
        parameters["LOT"] is SecsAsciiItem ? GemCommandStatus.Completed : GemCommandStatus.InvalidParameter)));
var command = await commands.ExecuteAsync("START",
    new Dictionary<string, SecsItem> { ["LOT"] = new SecsAsciiItem("DEMO") }, TimeSpan.FromSeconds(5));
Console.WriteLine($"Remote command: {command.Status}");
