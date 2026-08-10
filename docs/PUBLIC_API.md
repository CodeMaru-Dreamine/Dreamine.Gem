# Public API Inventory

Assembly: `Dreamine.Gem`

This inventory is generated from the compiled Release assembly. It is an audit artifact, not an additional compatibility promise.

Exported types: **15**

## Types

### `public sealed class Dreamine.Gem.GemAssemblyMarker`

- No declared public members.

### `public sealed class Dreamine.Gem.GemRuntime`

- `Dreamine.Gem.Abstractions.Interfaces.IGemMessageTransport Transport { get; }`
- `Dreamine.Gem.Protocol.GemProtocolEngine Protocol { get; }`
- `Dreamine.Gem.Services.GemAlarmService Alarms { get; }`
- `Dreamine.Gem.Services.GemClockService Clock { get; }`
- `Dreamine.Gem.Services.GemEquipmentConstantService Constants { get; }`
- `Dreamine.Gem.Services.GemEventReportService Events { get; }`
- `Dreamine.Gem.Services.GemProcessProgramService ProcessPrograms { get; }`
- `Dreamine.Gem.Services.GemRemoteCommandService Commands { get; }`
- `Dreamine.Gem.Services.GemSpoolService Spool { get; }`
- `Dreamine.Gem.Services.GemVariableCatalog Variables { get; }`
- `Dreamine.Gem.StateMachines.GemCommunicationStateMachine Communication { get; }`
- `Dreamine.Gem.StateMachines.GemControlStateMachine Control { get; }`
- `Dreamine.Gem.StateMachines.GemProcessingStateMachine Processing { get; }`
- `Dreamine.Secs.Abstractions.Interfaces.ISecsConnection SecsConnection { get; }`
- `GemRuntime(Dreamine.Gem.Abstractions.Interfaces.IGemMessageTransport transport, Dreamine.Gem.Abstractions.Model.GemEquipmentIdentity identity, System.TimeProvider timeProvider, System.Int32 spoolCapacity)`

### `public sealed class Dreamine.Gem.Protocol.GemProtocolEngine`

- `GemProtocolEngine(Dreamine.Gem.Abstractions.Interfaces.IGemMessageTransport transport, Dreamine.Gem.StateMachines.GemCommunicationStateMachine communication, Dreamine.Gem.Abstractions.Model.GemEquipmentIdentity identity, System.TimeProvider timeProvider, System.Nullable<System.TimeSpan> retryDelay)`
- `System.Threading.Tasks.Task<System.Boolean> EstablishCommunicationsAsync(System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<System.Boolean> HandleAsync(Dreamine.Secs.Abstractions.Model.SecsMessage primary, System.Threading.CancellationToken cancellationToken)`

### `public sealed class Dreamine.Gem.Services.GemAlarmService`

- `Dreamine.Gem.Abstractions.States.GemAlarmChangeStatus ChangeAlarm(System.UInt64 id, System.Boolean isSet)`
- `GemAlarmService()`
- `System.Boolean SetAlarm(System.UInt64 id, System.Boolean isSet)`
- `System.Boolean SetEnabled(System.UInt64 id, System.Boolean enabled)`
- `System.Boolean TryGetState(System.UInt64 id, System.Boolean& isSet)`
- `System.Collections.Generic.IReadOnlyList<System.UInt64> GetSetAlarmIds()`
- `System.Void Register(Dreamine.Gem.Abstractions.Model.GemAlarmDefinition definition)`

### `public sealed class Dreamine.Gem.Services.GemClockService`

- `GemClockService(System.TimeProvider timeProvider)`
- `System.DateTimeOffset GetUtcNow()`
- `System.String Format(System.Boolean fourDigitYear)`
- `System.Void SetUtcNow(System.DateTimeOffset value)`

### `public sealed class Dreamine.Gem.Services.GemEquipmentConstantService`

- `Dreamine.Gem.Abstractions.States.GemConstantSetStatus SetValue(System.UInt64 id, Dreamine.Secs.Abstractions.Model.SecsItem value, Dreamine.Gem.Abstractions.States.GemControlState controlState)`
- `GemEquipmentConstantService()`
- `System.Boolean TryGetValue(System.UInt64 id, Dreamine.Secs.Abstractions.Model.SecsItem& value)`
- `System.Boolean TrySetValue(System.UInt64 id, Dreamine.Secs.Abstractions.Model.SecsItem value)`
- `System.Void Register(Dreamine.Gem.Abstractions.Model.GemEquipmentConstantDefinition definition, System.Func<Dreamine.Secs.Abstractions.Model.SecsItem, System.Boolean> validator, System.Func<Dreamine.Gem.Abstractions.States.GemControlState, System.Boolean> statePolicy)`

### `public sealed class Dreamine.Gem.Services.GemEventReportService`

- `GemEventReportService(Dreamine.Gem.Abstractions.Interfaces.IGemVariableCatalog variables, System.TimeProvider timeProvider)`
- `System.Boolean DeleteReport(System.UInt64 reportId)`
- `System.Boolean LinkReport(System.UInt64 eventId, System.UInt64 reportId)`
- `System.Boolean SetEnabled(System.UInt64 eventId, System.Boolean enabled)`
- `System.Boolean UnlinkReport(System.UInt64 eventId, System.UInt64 reportId)`
- `System.Threading.Tasks.ValueTask<Dreamine.Gem.Abstractions.Model.GemEventSnapshot> CollectAsync(System.UInt64 eventId, System.Threading.CancellationToken cancellationToken)`
- `System.Void DefineEvent(Dreamine.Gem.Abstractions.Model.GemCollectionEventDefinition collectionEvent)`
- `System.Void DefineReport(Dreamine.Gem.Abstractions.Model.GemReportDefinition report)`

### `public sealed class Dreamine.Gem.Services.GemProcessProgramService`

- `GemProcessProgramService()`
- `System.Boolean Delete(System.String id)`
- `System.Boolean TryGet(System.String id, Dreamine.Gem.Abstractions.Model.GemProcessProgram& program)`
- `System.Collections.Generic.IReadOnlyList<System.String> GetIds()`
- `System.Void Put(Dreamine.Gem.Abstractions.Model.GemProcessProgram program)`

### `public sealed class Dreamine.Gem.Services.GemRemoteCommandService`

- `GemRemoteCommandService(System.TimeProvider timeProvider, System.Func<Dreamine.Gem.Abstractions.States.GemControlState> controlState)`
- `System.Threading.Tasks.ValueTask<Dreamine.Gem.Abstractions.Model.GemCommandResult> ExecuteAsync(System.String name, System.Collections.Generic.IReadOnlyDictionary<System.String, Dreamine.Secs.Abstractions.Model.SecsItem> parameters, System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken)`
- `System.Void Register(Dreamine.Gem.Abstractions.Model.GemRemoteCommandDefinition definition, System.Func<System.Collections.Generic.IReadOnlyDictionary<System.String, Dreamine.Secs.Abstractions.Model.SecsItem>, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<Dreamine.Gem.Abstractions.Model.GemCommandResult>> handler)`

### `public sealed class Dreamine.Gem.Services.GemSpoolService`

- `Dreamine.Gem.Abstractions.States.GemSpoolState State { get; }`
- `GemSpoolService(System.Int32 capacity)`
- `System.Boolean Enqueue(Dreamine.Secs.Abstractions.Model.SecsMessage message)`
- `System.Int32 Count { get; }`
- `System.Threading.Tasks.Task DrainAsync(System.Func<Dreamine.Secs.Abstractions.Model.SecsMessage, System.Threading.CancellationToken, System.Threading.Tasks.Task> sender, System.Threading.CancellationToken cancellationToken)`
- `System.Void Purge()`
- `System.Void Start()`

### `public sealed class Dreamine.Gem.Services.GemVariableCatalog`

- `GemVariableCatalog()`
- `System.Boolean TryGetDefinition(System.UInt64 id, Dreamine.Gem.Abstractions.Model.GemVariableDefinition& definition)`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Abstractions.Model.GemVariableDefinition> GetDefinitions(System.Nullable<Dreamine.Gem.Abstractions.States.GemVariableKind> kind)`
- `System.Threading.Tasks.ValueTask<Dreamine.Secs.Abstractions.Model.SecsItem> ReadAsync(System.UInt64 id, System.Threading.CancellationToken cancellationToken)`
- `System.Void Register(Dreamine.Gem.Abstractions.Model.GemVariableDefinition definition, System.Func<System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<Dreamine.Secs.Abstractions.Model.SecsItem>> reader)`

### `public sealed class Dreamine.Gem.StateMachines.GemCommunicationStateMachine`

- `Dreamine.Gem.Abstractions.States.GemCommunicationState State { get; }`
- `Dreamine.Gem.Abstractions.States.GemEstablishmentState EstablishmentState { get; }`
- `GemCommunicationStateMachine()`
- `System.Void Accept()`
- `System.Void CommunicationLost(System.Boolean equipmentInitiated)`
- `System.Void Disable()`
- `System.Void Enable(System.Boolean equipmentInitiated)`
- `System.Void RequestSent()`
- `System.Void Retry()`

### `public sealed class Dreamine.Gem.StateMachines.GemControlStateMachine`

- `Dreamine.Gem.Abstractions.States.GemControlState State { get; }`
- `GemControlStateMachine()`
- `System.Void AcceptOnline()`
- `System.Void AttemptOnline()`
- `System.Void EquipmentOffline()`
- `System.Void HostOffline()`
- `System.Void RejectOnline()`
- `System.Void SelectLocal()`
- `System.Void SelectRemote()`

### `public sealed class Dreamine.Gem.StateMachines.GemProcessingStateMachine`

- `Dreamine.Gem.Abstractions.States.GemProcessingState State { get; }`
- `GemProcessingStateMachine()`
- `System.Void Abort()`
- `System.Void BeginSetup()`
- `System.Void Complete()`
- `System.Void CompleteInitialization()`
- `System.Void Execute()`
- `System.Void Pause()`
- `System.Void Ready()`
- `System.Void Resume()`

### `public sealed class Dreamine.Gem.Transport.HsmsGemTransport`

- `Dreamine.Secs.Abstractions.Interfaces.ISecsConnection Connection { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsSessionId SessionId { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsSystemBytes AllocateSystemBytes()`
- `HsmsGemTransport(Dreamine.Secs.Com.Hsms.HsmsSession session, Dreamine.Secs.Abstractions.Model.SecsSessionId sessionId)`
- `System.Threading.Tasks.Task SendAsync(Dreamine.Secs.Abstractions.Model.SecsMessage message, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Secs.Abstractions.Model.SecsMessage> RequestAsync(Dreamine.Secs.Abstractions.Model.SecsMessage message, System.Threading.CancellationToken cancellationToken)`
- `event System.EventHandler<Dreamine.Secs.Abstractions.Model.SecsMessage> MessageReceived`
