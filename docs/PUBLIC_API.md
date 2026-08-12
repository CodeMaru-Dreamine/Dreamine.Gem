# Public API Inventory

Assembly: `Dreamine.Gem`

This inventory is generated from the compiled Release assembly. It is an audit artifact, not an additional compatibility promise.

Exported types: **43**

## Types

### `public static class Dreamine.Gem.Demo.E30DemoEquipmentProfile`

- `Dreamine.Gem.Profiles.GemEquipmentProfile Create()`
- `const System.String ProfileName = "Dreamine generic E30 demo equipment"`
- `const System.String StartCommand = "START"`
- `const System.UInt64 AlarmId = 2000`
- `const System.UInt64 BatchSizeConstantId = 100`
- `const System.UInt64 CommandCompletedEventId = 1001`
- `const System.UInt64 CompletedCountVariableId = 2`
- `const System.UInt64 EquipmentStateVariableId = 1`
- `const System.UInt64 StatusEventId = 1000`
- `const System.UInt64 StatusReportId = 10`

### `public sealed class Dreamine.Gem.GemAssemblyMarker`

- No declared public members.

### `public sealed class Dreamine.Gem.GemRuntime`

- `Dreamine.Gem.Abstractions.Interfaces.IGemMessageTransport Transport { get; }`
- `Dreamine.Gem.Profiles.GemEquipmentProfile Profile { get; }`
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

### `public sealed class Dreamine.Gem.Host.E30HostClient`

- `E30HostClient(Dreamine.Secs.Abstractions.Interfaces.ISecsMessageSession session, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy identifiers, Dreamine.Gem.Abstractions.Interfaces.IGemClockService clock)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<Dreamine.Gem.Protocol.E30.E30EventReport>> ReadEventReportAsync(System.UInt64 collectionEventId, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<Dreamine.Gem.Protocol.E30.E30HostCommandAcknowledgement>> SendRemoteCommandAsync(System.String name, System.Collections.Generic.IEnumerable<Dreamine.Gem.Protocol.E30.E30CommandParameter> parameters, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<Dreamine.Gem.Protocol.E30.E30PeerIdentity>> AreYouThereAsync(System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<Dreamine.Gem.Protocol.E30.E30PeerIdentity>> EstablishCommunicationsAsync(System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<System.Byte>> DefineReportsAsync(System.UInt64 dataId, System.Collections.Generic.IEnumerable<Dreamine.Gem.Protocol.E30.E30ReportDefinition> reports, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<System.Byte>> LinkEventReportsAsync(System.UInt64 dataId, System.Collections.Generic.IEnumerable<Dreamine.Gem.Protocol.E30.E30EventLink> links, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<System.Byte>> RequestOfflineAsync(System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<System.Byte>> RequestOnlineAsync(System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<System.Byte>> SetAlarmEnablementAsync(System.Boolean enabled, System.Nullable<System.UInt64> alarmId, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<System.Byte>> SetEquipmentConstantsAsync(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.UInt64, Dreamine.Secs.Abstractions.Model.SecsItem>> updates, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<System.Byte>> SetEventEnablementAsync(System.Boolean enabled, System.Collections.Generic.IEnumerable<System.UInt64> collectionEventIds, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<System.Byte>> SetTimeAsync(System.DateTimeOffset value, System.Boolean fourDigitYear, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Protocol.E30.E30AlarmData>>> ReadAlarmsAsync(System.Collections.Generic.IEnumerable<System.UInt64> alarmIds, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Protocol.E30.E30EquipmentConstantName>>> ReadEquipmentConstantNamesAsync(System.Collections.Generic.IEnumerable<System.UInt64> equipmentConstantIds, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Protocol.E30.E30StatusVariableName>>> ReadStatusVariableNamesAsync(System.Collections.Generic.IEnumerable<System.UInt64> statusVariableIds, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<System.Collections.Generic.IReadOnlyList<Dreamine.Secs.Abstractions.Model.SecsItem>>> ReadEquipmentConstantsAsync(System.Collections.Generic.IEnumerable<System.UInt64> equipmentConstantIds, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<System.Collections.Generic.IReadOnlyList<Dreamine.Secs.Abstractions.Model.SecsItem>>> ReadStatusAsync(System.Collections.Generic.IEnumerable<System.UInt64> statusVariableIds, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<System.DateTimeOffset>> ReadTimeAsync(System.Threading.CancellationToken cancellationToken)`
- `System.Void Dispose()`
- `event System.EventHandler<Dreamine.Gem.Protocol.E30.E30AlarmData> AlarmReceived`
- `event System.EventHandler<Dreamine.Gem.Protocol.E30.E30EventReport> EventReportReceived`

### `public sealed class Dreamine.Gem.Profiles.GemEquipmentContext`

- `Dreamine.Gem.GemRuntime Runtime { get; }`
- `Dreamine.Gem.Profiles.GemEquipmentProfile Profile { get; }`

### `public sealed class Dreamine.Gem.Profiles.GemEquipmentProfile`

- `Dreamine.Gem.Abstractions.Model.GemEquipmentIdentity Identity { get; }`
- `Dreamine.Gem.Abstractions.Model.GemIdentifierFormatPolicy IdentifierFormats { get; }`
- `Dreamine.Gem.Profiles.GemEquipmentContext CreateContext(Dreamine.Gem.Abstractions.Interfaces.IGemMessageTransport transport, System.TimeProvider timeProvider, System.Int32 spoolCapacity)`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Abstractions.Model.GemAlarmDefinition> Alarms { get; }`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Abstractions.Model.GemCollectionEventDefinition> CollectionEvents { get; }`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Abstractions.Model.GemEquipmentConstantProfileDefinition> EquipmentConstants { get; }`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Abstractions.Model.GemProfileCapability> Capabilities { get; }`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Abstractions.Model.GemProfileCapability> V1Capabilities { get; }`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Abstractions.Model.GemRemoteCommandProfileEntry> RemoteCommands { get; }`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Abstractions.Model.GemReportDefinition> Reports { get; }`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Abstractions.Model.GemVariableProfileDefinition> Variables { get; }`
- `System.String Name { get; }`
- `System.String TargetRevision { get; }`

### `public sealed class Dreamine.Gem.Profiles.GemEquipmentProfileBuilder`

- `Dreamine.Gem.Abstractions.Model.GemEquipmentIdentity Identity { get; }`
- `Dreamine.Gem.Abstractions.Model.GemIdentifierFormatPolicy IdentifierFormats { get; }`
- `Dreamine.Gem.Profiles.GemEquipmentProfile Build()`
- `Dreamine.Gem.Profiles.GemEquipmentProfileBuilder AddAlarm(Dreamine.Gem.Abstractions.Model.GemAlarmDefinition alarm)`
- `Dreamine.Gem.Profiles.GemEquipmentProfileBuilder AddCollectionEvent(Dreamine.Gem.Abstractions.Model.GemCollectionEventDefinition collectionEvent)`
- `Dreamine.Gem.Profiles.GemEquipmentProfileBuilder AddEquipmentConstant(Dreamine.Gem.Abstractions.Model.GemEquipmentConstantDefinition definition, Dreamine.Secs.Abstractions.Model.SecsItemFormat format, System.Func<Dreamine.Secs.Abstractions.Model.SecsItem, System.Boolean> validator, System.Collections.Generic.IEnumerable<Dreamine.Gem.Abstractions.States.GemControlState> allowedControlStates)`
- `Dreamine.Gem.Profiles.GemEquipmentProfileBuilder AddRemoteCommand(Dreamine.Gem.Abstractions.Model.GemRemoteCommandProfileDefinition definition, System.Func<System.Collections.Generic.IReadOnlyDictionary<System.String, Dreamine.Secs.Abstractions.Model.SecsItem>, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<Dreamine.Gem.Abstractions.Model.GemCommandResult>> handler)`
- `Dreamine.Gem.Profiles.GemEquipmentProfileBuilder AddReport(Dreamine.Gem.Abstractions.Model.GemReportDefinition report)`
- `Dreamine.Gem.Profiles.GemEquipmentProfileBuilder AddVariable(Dreamine.Gem.Abstractions.Model.GemVariableDefinition definition, Dreamine.Secs.Abstractions.Model.SecsItemFormat format, System.Func<System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<Dreamine.Secs.Abstractions.Model.SecsItem>> reader)`
- `GemEquipmentProfileBuilder(System.String name, Dreamine.Gem.Abstractions.Model.GemEquipmentIdentity identity, System.String targetRevision, Dreamine.Gem.Abstractions.Model.GemIdentifierFormatPolicy identifierFormats)`
- `System.String Name { get; }`
- `System.String TargetRevision { get; }`

### `public sealed class Dreamine.Gem.Protocol.E30.E30AlarmData`

- `Dreamine.Gem.Protocol.E30.E30AlarmData <Clone>$()`
- `E30AlarmData(System.Byte Code, System.UInt64 AlarmId, System.String Text)`
- `System.Boolean Equals(Dreamine.Gem.Protocol.E30.E30AlarmData other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Byte Code { get; set; }`
- `System.Int32 GetHashCode()`
- `System.String Text { get; set; }`
- `System.String ToString()`
- `System.UInt64 AlarmId { get; set; }`
- `System.Void Deconstruct(out System.Byte Code, out System.UInt64 AlarmId, out System.String Text)`

### `public enum Dreamine.Gem.Protocol.E30.E30CallOutcome`

- `const Dreamine.Gem.Protocol.E30.E30CallOutcome Canceled = 3`
- `const Dreamine.Gem.Protocol.E30.E30CallOutcome Completed = 0`
- `const Dreamine.Gem.Protocol.E30.E30CallOutcome FunctionZero = 1`
- `const Dreamine.Gem.Protocol.E30.E30CallOutcome Malformed = 4`
- `const Dreamine.Gem.Protocol.E30.E30CallOutcome NotSent = 5`
- `const Dreamine.Gem.Protocol.E30.E30CallOutcome TimedOut = 2`

### `public sealed class Dreamine.Gem.Protocol.E30.E30CallResult<T>`

- `Dreamine.Gem.Protocol.E30.E30CallOutcome Outcome { get; }`
- `Dreamine.Gem.Protocol.E30.E30CallResult<T> <Clone>$()`
- `Dreamine.Gem.Protocol.E30.E30CallResult<T> Complete(T value, System.Nullable<System.Byte> acknowledgement)`
- `Dreamine.Gem.Protocol.E30.E30CallResult<T> CompleteWithAck(System.Byte acknowledgement, T value)`
- `Dreamine.Gem.Protocol.E30.E30CallResult<T> Ended(Dreamine.Gem.Protocol.E30.E30CallOutcome outcome, System.String error)`
- `System.Boolean Equals(Dreamine.Gem.Protocol.E30.E30CallResult<T> other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Boolean HasNormalSecondary { get; }`
- `System.Boolean IsAcknowledged { get; }`
- `System.Int32 GetHashCode()`
- `System.Nullable<System.Byte> Acknowledgement { get; }`
- `System.String Error { get; }`
- `System.String ToString()`
- `T Value { get; }`

### `public enum Dreamine.Gem.Protocol.E30.E30CapabilityDisposition`

- `const Dreamine.Gem.Protocol.E30.E30CapabilityDisposition BlockedBoundary = 3`
- `const Dreamine.Gem.Protocol.E30.E30CapabilityDisposition BlockedStandard = 2`
- `const Dreamine.Gem.Protocol.E30.E30CapabilityDisposition Implemented = 0`
- `const Dreamine.Gem.Protocol.E30.E30CapabilityDisposition ImplementedUnverified = 1`
- `const Dreamine.Gem.Protocol.E30.E30CapabilityDisposition IntentionallyExcluded = 4`

### `public sealed class Dreamine.Gem.Protocol.E30.E30CapabilityEntry`

- `Dreamine.Gem.Protocol.E30.E30CapabilityDisposition Disposition { get; }`
- `Dreamine.Gem.Protocol.E30.E30CapabilityEntry <Clone>$()`
- `E30CapabilityEntry(System.String name, Dreamine.Gem.Protocol.E30.E30CapabilityDisposition disposition, System.String rationale)`
- `System.Boolean Equals(Dreamine.Gem.Protocol.E30.E30CapabilityEntry other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Int32 GetHashCode()`
- `System.String Name { get; }`
- `System.String Rationale { get; }`
- `System.String ToString()`

### `public sealed class Dreamine.Gem.Protocol.E30.E30CommandParameter`

- `Dreamine.Gem.Protocol.E30.E30CommandParameter <Clone>$()`
- `Dreamine.Secs.Abstractions.Model.SecsItem Value { get; set; }`
- `E30CommandParameter(System.String Name, Dreamine.Secs.Abstractions.Model.SecsItem Value)`
- `System.Boolean Equals(Dreamine.Gem.Protocol.E30.E30CommandParameter other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Int32 GetHashCode()`
- `System.String Name { get; set; }`
- `System.String ToString()`
- `System.Void Deconstruct(out System.String Name, out Dreamine.Secs.Abstractions.Model.SecsItem Value)`

### `public static class Dreamine.Gem.Protocol.E30.E30DerivedSubsetManifest`

- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Protocol.E30.E30CapabilityEntry> Capabilities { get; }`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition> IncludedDialogues { get; }`
- `const System.String ProfileName = "E30-0611 derived subset profile v1"`

### `public static class Dreamine.Gem.Protocol.E30.E30Dialogues`

- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S1F1 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S1F11 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S1F13 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S1F15 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S1F17 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S1F3 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S2F13 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S2F15 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S2F17 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S2F29 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S2F31 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S2F33 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S2F35 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S2F37 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S2F41 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S5F1 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S5F3 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S5F5 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S6F11 { get; }`
- `Dreamine.Secs.Abstractions.Model.SecsDialogueDefinition S6F15 { get; }`

### `public sealed class Dreamine.Gem.Protocol.E30.E30EquipmentConstantName`

- `Dreamine.Gem.Protocol.E30.E30EquipmentConstantName <Clone>$()`
- `Dreamine.Secs.Abstractions.Model.SecsItem Default { get; set; }`
- `Dreamine.Secs.Abstractions.Model.SecsItem Maximum { get; set; }`
- `Dreamine.Secs.Abstractions.Model.SecsItem Minimum { get; set; }`
- `E30EquipmentConstantName(System.UInt64 Id, System.String Name, Dreamine.Secs.Abstractions.Model.SecsItem Minimum, Dreamine.Secs.Abstractions.Model.SecsItem Maximum, Dreamine.Secs.Abstractions.Model.SecsItem Default, System.String Units)`
- `System.Boolean Equals(Dreamine.Gem.Protocol.E30.E30EquipmentConstantName other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Int32 GetHashCode()`
- `System.String Name { get; set; }`
- `System.String ToString()`
- `System.String Units { get; set; }`
- `System.UInt64 Id { get; set; }`
- `System.Void Deconstruct(out System.UInt64 Id, out System.String Name, out Dreamine.Secs.Abstractions.Model.SecsItem Minimum, out Dreamine.Secs.Abstractions.Model.SecsItem Maximum, out Dreamine.Secs.Abstractions.Model.SecsItem Default, out System.String Units)`

### `public sealed class Dreamine.Gem.Protocol.E30.E30EquipmentRouter`

- `E30EquipmentRouter(Dreamine.Secs.Abstractions.Interfaces.ISecsMessageSession session, Dreamine.Gem.Profiles.GemEquipmentContext context, Dreamine.Gem.Protocol.E30.E30EquipmentRouterOptions options)`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Protocol.E30.E30EventLink> GetEventLinks()`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Protocol.E30.E30ReportDefinition> GetReportDefinitions()`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<Dreamine.Gem.Protocol.E30.E30PeerIdentity>> AreYouThereAsync(System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<Dreamine.Gem.Protocol.E30.E30PeerIdentity>> EstablishCommunicationsAsync(System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<System.Byte>> PublishAlarmChangeAsync(System.UInt64 alarmId, System.Boolean isSet, System.Nullable<System.UInt64> collectionEventId, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<System.Byte>> PublishEventAsync(System.UInt64 collectionEventId, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Gem.Protocol.E30.E30CallResult<System.DateTimeOffset>> ReadHostTimeAsync(System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.ValueTask DisposeAsync()`

### `public sealed class Dreamine.Gem.Protocol.E30.E30EquipmentRouterOptions`

- `E30EquipmentRouterOptions()`
- `System.Collections.Generic.IReadOnlyDictionary<System.String, System.UInt64> CommandCompletionEvents { get; set; }`
- `System.Int32 CommandQueueCapacity { get; set; }`
- `System.Int32 MaximumSingleBlockBodyBytes { get; set; }`
- `System.TimeSpan CommandTimeout { get; set; }`

### `public sealed class Dreamine.Gem.Protocol.E30.E30EventLink`

- `E30EventLink(System.UInt64 collectionEventId, System.Collections.Generic.IEnumerable<System.UInt64> reportIds)`
- `System.Collections.Generic.IReadOnlyList<System.UInt64> ReportIds { get; }`
- `System.UInt64 CollectionEventId { get; }`

### `public sealed class Dreamine.Gem.Protocol.E30.E30EventReport`

- `E30EventReport(System.UInt64 dataId, System.UInt64 collectionEventId, System.Collections.Generic.IEnumerable<Dreamine.Gem.Protocol.E30.E30ReportValues> reports)`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Protocol.E30.E30ReportValues> Reports { get; }`
- `System.UInt64 CollectionEventId { get; }`
- `System.UInt64 DataId { get; }`

### `public sealed class Dreamine.Gem.Protocol.E30.E30HostCommandAcknowledgement`

- `E30HostCommandAcknowledgement(System.Byte acknowledgement, System.Collections.Generic.IEnumerable<Dreamine.Gem.Protocol.E30.E30RejectedCommandParameter> rejectedParameters)`
- `System.Byte Acknowledgement { get; }`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Protocol.E30.E30RejectedCommandParameter> RejectedParameters { get; }`

### `public enum Dreamine.Gem.Protocol.E30.E30IdentifierFormat`

- `const Dreamine.Gem.Protocol.E30.E30IdentifierFormat UInt16 = 1`
- `const Dreamine.Gem.Protocol.E30.E30IdentifierFormat UInt32 = 2`
- `const Dreamine.Gem.Protocol.E30.E30IdentifierFormat UInt64 = 3`
- `const Dreamine.Gem.Protocol.E30.E30IdentifierFormat UInt8 = 0`

### `public sealed class Dreamine.Gem.Protocol.E30.E30IdentifierPolicy`

- `Dreamine.Gem.Protocol.E30.E30IdentifierFormat Alarm { get; }`
- `Dreamine.Gem.Protocol.E30.E30IdentifierFormat CollectionEvent { get; }`
- `Dreamine.Gem.Protocol.E30.E30IdentifierFormat Data { get; }`
- `Dreamine.Gem.Protocol.E30.E30IdentifierFormat EquipmentConstant { get; }`
- `Dreamine.Gem.Protocol.E30.E30IdentifierFormat Report { get; }`
- `Dreamine.Gem.Protocol.E30.E30IdentifierFormat StatusVariable { get; }`
- `Dreamine.Gem.Protocol.E30.E30IdentifierFormat Variable { get; }`
- `Dreamine.Gem.Protocol.E30.E30IdentifierPolicy <Clone>$()`
- `E30IdentifierPolicy(Dreamine.Gem.Abstractions.Model.GemIdentifierFormatPolicy policy)`
- `E30IdentifierPolicy(Dreamine.Gem.Protocol.E30.E30IdentifierFormat variable, Dreamine.Gem.Protocol.E30.E30IdentifierFormat statusVariable, Dreamine.Gem.Protocol.E30.E30IdentifierFormat equipmentConstant, Dreamine.Gem.Protocol.E30.E30IdentifierFormat data, Dreamine.Gem.Protocol.E30.E30IdentifierFormat report, Dreamine.Gem.Protocol.E30.E30IdentifierFormat collectionEvent, Dreamine.Gem.Protocol.E30.E30IdentifierFormat alarm)`
- `System.Boolean Equals(Dreamine.Gem.Protocol.E30.E30IdentifierPolicy other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Int32 GetHashCode()`
- `System.String ToString()`

### `public sealed class Dreamine.Gem.Protocol.E30.E30PeerIdentity`

- `Dreamine.Gem.Protocol.E30.E30PeerIdentity <Clone>$()`
- `E30PeerIdentity(System.String ModelNumber, System.String SoftwareRevision)`
- `System.Boolean Equals(Dreamine.Gem.Protocol.E30.E30PeerIdentity other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Int32 GetHashCode()`
- `System.String ModelNumber { get; set; }`
- `System.String SoftwareRevision { get; set; }`
- `System.String ToString()`
- `System.Void Deconstruct(out System.String ModelNumber, out System.String SoftwareRevision)`

### `public sealed class Dreamine.Gem.Protocol.E30.E30RejectedCommandParameter`

- `Dreamine.Gem.Protocol.E30.E30RejectedCommandParameter <Clone>$()`
- `E30RejectedCommandParameter(System.String Name, System.Byte Acknowledgement)`
- `System.Boolean Equals(Dreamine.Gem.Protocol.E30.E30RejectedCommandParameter other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Byte Acknowledgement { get; set; }`
- `System.Int32 GetHashCode()`
- `System.String Name { get; set; }`
- `System.String ToString()`
- `System.Void Deconstruct(out System.String Name, out System.Byte Acknowledgement)`

### `public sealed class Dreamine.Gem.Protocol.E30.E30ReportDefinition`

- `E30ReportDefinition(System.UInt64 reportId, System.Collections.Generic.IEnumerable<System.UInt64> variableIds)`
- `System.Collections.Generic.IReadOnlyList<System.UInt64> VariableIds { get; }`
- `System.UInt64 ReportId { get; }`

### `public sealed class Dreamine.Gem.Protocol.E30.E30ReportValues`

- `E30ReportValues(System.UInt64 reportId, System.Collections.Generic.IEnumerable<Dreamine.Secs.Abstractions.Model.SecsItem> values)`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Secs.Abstractions.Model.SecsItem> Values { get; }`
- `System.UInt64 ReportId { get; }`

### `public sealed class Dreamine.Gem.Protocol.E30.E30StatusVariableName`

- `Dreamine.Gem.Protocol.E30.E30StatusVariableName <Clone>$()`
- `E30StatusVariableName(System.UInt64 Id, System.String Name, System.String Units)`
- `System.Boolean Equals(Dreamine.Gem.Protocol.E30.E30StatusVariableName other)`
- `System.Boolean Equals(System.Object obj)`
- `System.Int32 GetHashCode()`
- `System.String Name { get; set; }`
- `System.String ToString()`
- `System.String Units { get; set; }`
- `System.UInt64 Id { get; set; }`
- `System.Void Deconstruct(out System.UInt64 Id, out System.String Name, out System.String Units)`

### `public static class Dreamine.Gem.Protocol.E30.E30WireCodec`

- `Dreamine.Gem.Protocol.E30.E30AlarmData ReadAlarm(Dreamine.Secs.Abstractions.Model.SecsItem item, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `Dreamine.Gem.Protocol.E30.E30EventReport ReadEventReport(Dreamine.Secs.Abstractions.Model.SecsItem item, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `Dreamine.Gem.Protocol.E30.E30HostCommandAcknowledgement ReadHostCommandAcknowledgement(Dreamine.Secs.Abstractions.Model.SecsItem item)`
- `Dreamine.Secs.Abstractions.Model.SecsAsciiItem Time(System.DateTimeOffset value, System.Boolean fourDigitYear)`
- `Dreamine.Secs.Abstractions.Model.SecsBinaryItem Acknowledgement(System.Byte value)`
- `Dreamine.Secs.Abstractions.Model.SecsBinaryItem MessageHeader(Dreamine.Secs.Abstractions.Model.SecsMessage message)`
- `Dreamine.Secs.Abstractions.Model.SecsItem DataIdentifier(System.UInt64 value, Dreamine.Gem.Protocol.E30.E30IdentifierFormat format)`
- `Dreamine.Secs.Abstractions.Model.SecsItem Identifier(System.UInt64 value, Dreamine.Gem.Protocol.E30.E30IdentifierFormat format)`
- `Dreamine.Secs.Abstractions.Model.SecsItem IdentifierVector(System.Collections.Generic.IEnumerable<System.UInt64> values, Dreamine.Gem.Protocol.E30.E30IdentifierFormat format)`
- `Dreamine.Secs.Abstractions.Model.SecsListItem Alarm(Dreamine.Gem.Protocol.E30.E30AlarmData alarm, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `Dreamine.Secs.Abstractions.Model.SecsListItem AlarmEnablement(System.Boolean enabled, System.Nullable<System.UInt64> alarmId, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `Dreamine.Secs.Abstractions.Model.SecsListItem Alarms(System.Collections.Generic.IEnumerable<Dreamine.Gem.Protocol.E30.E30AlarmData> alarms, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `Dreamine.Secs.Abstractions.Model.SecsListItem CommunicationAcknowledgement(System.Byte acknowledgement, System.String modelNumber, System.String softwareRevision, System.Boolean hostResponse)`
- `Dreamine.Secs.Abstractions.Model.SecsListItem EquipmentConstantNames(System.Collections.Generic.IEnumerable<Dreamine.Gem.Protocol.E30.E30EquipmentConstantName> values, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `Dreamine.Secs.Abstractions.Model.SecsListItem EquipmentConstantUpdates(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.UInt64, Dreamine.Secs.Abstractions.Model.SecsItem>> values, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `Dreamine.Secs.Abstractions.Model.SecsListItem EventEnablement(System.Boolean enabled, System.Collections.Generic.IEnumerable<System.UInt64> collectionEventIds, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `Dreamine.Secs.Abstractions.Model.SecsListItem EventLinks(System.UInt64 dataId, System.Collections.Generic.IEnumerable<Dreamine.Gem.Protocol.E30.E30EventLink> links, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `Dreamine.Secs.Abstractions.Model.SecsListItem EventReport(Dreamine.Gem.Protocol.E30.E30EventReport report, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `Dreamine.Secs.Abstractions.Model.SecsListItem HostCommand(System.String name, System.Collections.Generic.IEnumerable<Dreamine.Gem.Protocol.E30.E30CommandParameter> parameters)`
- `Dreamine.Secs.Abstractions.Model.SecsListItem HostCommandAcknowledgement(Dreamine.Gem.Protocol.E30.E30HostCommandAcknowledgement acknowledgement)`
- `Dreamine.Secs.Abstractions.Model.SecsListItem IdentifierList(System.Collections.Generic.IEnumerable<System.UInt64> values, Dreamine.Gem.Protocol.E30.E30IdentifierFormat format)`
- `Dreamine.Secs.Abstractions.Model.SecsListItem Identity(System.String modelNumber, System.String softwareRevision)`
- `Dreamine.Secs.Abstractions.Model.SecsListItem ReportDefinitions(System.UInt64 dataId, System.Collections.Generic.IEnumerable<Dreamine.Gem.Protocol.E30.E30ReportDefinition> reports, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `Dreamine.Secs.Abstractions.Model.SecsListItem StatusVariableNames(System.Collections.Generic.IEnumerable<Dreamine.Gem.Protocol.E30.E30StatusVariableName> values, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `Dreamine.Secs.Abstractions.Model.SecsListItem Values(System.Collections.Generic.IEnumerable<Dreamine.Secs.Abstractions.Model.SecsItem> values)`
- `Dreamine.Secs.Abstractions.Model.SecsMessage FunctionZero(Dreamine.Secs.Abstractions.Model.SecsMessage primary)`
- `Dreamine.Secs.Abstractions.Model.SecsMessage StreamNine(Dreamine.Secs.Abstractions.Model.SecsMessage offending, System.Byte function)`
- `System.Byte ReadAcknowledgement(Dreamine.Secs.Abstractions.Model.SecsItem item)`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Protocol.E30.E30AlarmData> ReadAlarms(Dreamine.Secs.Abstractions.Model.SecsItem item, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Protocol.E30.E30EquipmentConstantName> ReadEquipmentConstantNames(Dreamine.Secs.Abstractions.Model.SecsItem item, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Protocol.E30.E30StatusVariableName> ReadStatusVariableNames(Dreamine.Secs.Abstractions.Model.SecsItem item, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Secs.Abstractions.Model.SecsItem> ReadValues(Dreamine.Secs.Abstractions.Model.SecsItem item)`
- `System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<System.UInt64, Dreamine.Secs.Abstractions.Model.SecsItem>> ReadEquipmentConstantUpdates(Dreamine.Secs.Abstractions.Model.SecsItem item, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `System.Collections.Generic.IReadOnlyList<System.UInt64> ReadIdentifierList(Dreamine.Secs.Abstractions.Model.SecsItem item, Dreamine.Gem.Protocol.E30.E30IdentifierFormat format)`
- `System.Collections.Generic.IReadOnlyList<System.UInt64> ReadIdentifierVector(Dreamine.Secs.Abstractions.Model.SecsItem item, Dreamine.Gem.Protocol.E30.E30IdentifierFormat format)`
- `System.DateTimeOffset ReadTime(Dreamine.Secs.Abstractions.Model.SecsItem item)`
- `System.Nullable<System.ValueTuple<System.String, System.String>> ReadIdentity(Dreamine.Secs.Abstractions.Model.SecsItem item)`
- `System.UInt64 ReadDataIdentifier(Dreamine.Secs.Abstractions.Model.SecsItem item, Dreamine.Gem.Protocol.E30.E30IdentifierFormat format)`
- `System.UInt64 ReadIdentifier(Dreamine.Secs.Abstractions.Model.SecsItem item, Dreamine.Gem.Protocol.E30.E30IdentifierFormat format)`
- `System.ValueTuple<System.Boolean, System.Collections.Generic.IReadOnlyList<System.UInt64>> ReadEventEnablement(Dreamine.Secs.Abstractions.Model.SecsItem item, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `System.ValueTuple<System.Boolean, System.Nullable<System.UInt64>> ReadAlarmEnablement(Dreamine.Secs.Abstractions.Model.SecsItem item, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `System.ValueTuple<System.Byte, System.Nullable<System.ValueTuple<System.String, System.String>>> ReadCommunicationAcknowledgement(Dreamine.Secs.Abstractions.Model.SecsItem item)`
- `System.ValueTuple<System.String, System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Protocol.E30.E30CommandParameter>> ReadHostCommand(Dreamine.Secs.Abstractions.Model.SecsItem item)`
- `System.ValueTuple<System.UInt64, System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Protocol.E30.E30EventLink>> ReadEventLinks(Dreamine.Secs.Abstractions.Model.SecsItem item, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`
- `System.ValueTuple<System.UInt64, System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Protocol.E30.E30ReportDefinition>> ReadReportDefinitions(Dreamine.Secs.Abstractions.Model.SecsItem item, Dreamine.Gem.Protocol.E30.E30IdentifierPolicy policy)`

### `public sealed class Dreamine.Gem.Protocol.E30.E30WireFormatException`

- `E30WireFormatException(System.String message)`

### `public sealed class Dreamine.Gem.Protocol.GemProtocolEngine`

- `GemProtocolEngine(Dreamine.Gem.Abstractions.Interfaces.IGemMessageTransport transport, Dreamine.Gem.StateMachines.GemCommunicationStateMachine communication, Dreamine.Gem.Abstractions.Model.GemEquipmentIdentity identity, System.TimeProvider timeProvider, System.Nullable<System.TimeSpan> retryDelay)`
- `System.Threading.Tasks.Task<System.Boolean> EstablishCommunicationsAsync(System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<System.Boolean> HandleAsync(Dreamine.Secs.Abstractions.Model.SecsMessage primary, System.Threading.CancellationToken cancellationToken)`

### `public sealed class Dreamine.Gem.Services.GemAlarmService`

- `Dreamine.Gem.Abstractions.States.GemAlarmChangeStatus ChangeAlarm(System.UInt64 id, System.Boolean isSet)`
- `Dreamine.Gem.Abstractions.States.GemAlarmChangeStatus ChangeEnabled(System.UInt64 id, System.Boolean enabled)`
- `GemAlarmService()`
- `System.Boolean SetAlarm(System.UInt64 id, System.Boolean isSet)`
- `System.Boolean SetEnabled(System.UInt64 id, System.Boolean enabled)`
- `System.Boolean TryGetState(System.UInt64 id, out System.Boolean isSet)`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Abstractions.Model.GemAlarmSnapshot> GetSnapshots()`
- `System.Collections.Generic.IReadOnlyList<System.UInt64> GetSetAlarmIds()`
- `System.Void Register(Dreamine.Gem.Abstractions.Model.GemAlarmDefinition definition)`

### `public sealed class Dreamine.Gem.Services.GemClockService`

- `GemClockService(System.TimeProvider timeProvider)`
- `System.DateTimeOffset GetUtcNow()`
- `System.String Format(System.Boolean fourDigitYear)`
- `System.Void SetUtcNow(System.DateTimeOffset value)`

### `public sealed class Dreamine.Gem.Services.GemEquipmentConstantService`

- `Dreamine.Gem.Abstractions.Model.GemConstantBatchResult SetValues(System.Collections.Generic.IEnumerable<Dreamine.Gem.Abstractions.Model.GemEquipmentConstantUpdate> updates, Dreamine.Gem.Abstractions.States.GemControlState controlState)`
- `Dreamine.Gem.Abstractions.States.GemConstantSetStatus SetValue(System.UInt64 id, Dreamine.Secs.Abstractions.Model.SecsItem value, Dreamine.Gem.Abstractions.States.GemControlState controlState)`
- `GemEquipmentConstantService()`
- `System.Boolean TryGetValue(System.UInt64 id, out Dreamine.Secs.Abstractions.Model.SecsItem value)`
- `System.Boolean TrySetValue(System.UInt64 id, Dreamine.Secs.Abstractions.Model.SecsItem value)`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Abstractions.Model.GemEquipmentConstantSnapshot> GetSnapshots()`
- `System.Void Register(Dreamine.Gem.Abstractions.Model.GemEquipmentConstantDefinition definition, System.Func<Dreamine.Secs.Abstractions.Model.SecsItem, System.Boolean> validator, System.Func<Dreamine.Gem.Abstractions.States.GemControlState, System.Boolean> statePolicy)`
- `System.Void RegisterTyped(Dreamine.Gem.Abstractions.Model.GemEquipmentConstantDefinition definition, Dreamine.Secs.Abstractions.Model.SecsItemFormat format, System.Func<Dreamine.Secs.Abstractions.Model.SecsItem, System.Boolean> validator, System.Func<Dreamine.Gem.Abstractions.States.GemControlState, System.Boolean> statePolicy)`

### `public sealed class Dreamine.Gem.Services.GemEventReportService`

- `Dreamine.Gem.Abstractions.Model.GemEventConfigurationResult ApplyEventLinks(System.Collections.Generic.IEnumerable<Dreamine.Gem.Abstractions.Model.GemEventReportLinkUpdate> updates, System.Boolean rejectExisting, System.Boolean disableUpdated)`
- `Dreamine.Gem.Abstractions.Model.GemEventConfigurationResult ApplyReportChanges(System.Collections.Generic.IEnumerable<Dreamine.Gem.Abstractions.Model.GemReportDefinition> definitions, System.Collections.Generic.IEnumerable<System.UInt64> deleteIds, System.Boolean deleteAll)`
- `Dreamine.Gem.Abstractions.Model.GemEventConfigurationResult DefineReports(System.Collections.Generic.IEnumerable<Dreamine.Gem.Abstractions.Model.GemReportDefinition> reports)`
- `Dreamine.Gem.Abstractions.Model.GemEventConfigurationResult DeleteAllEventLinks()`
- `Dreamine.Gem.Abstractions.Model.GemEventConfigurationResult DeleteAllReports()`
- `Dreamine.Gem.Abstractions.Model.GemEventConfigurationResult DeleteReports(System.Collections.Generic.IEnumerable<System.UInt64> reportIds)`
- `Dreamine.Gem.Abstractions.Model.GemEventConfigurationResult ReplaceEventLinks(System.Collections.Generic.IEnumerable<Dreamine.Gem.Abstractions.Model.GemEventReportLinkUpdate> updates)`
- `Dreamine.Gem.Abstractions.Model.GemEventConfigurationResult SetAllEventsEnabled(System.Boolean enabled)`
- `Dreamine.Gem.Abstractions.Model.GemEventConfigurationResult SetEventsEnabled(System.Collections.Generic.IEnumerable<Dreamine.Gem.Abstractions.Model.GemEventEnableUpdate> updates)`
- `GemEventReportService(Dreamine.Gem.Abstractions.Interfaces.IGemVariableCatalog variables, System.TimeProvider timeProvider)`
- `System.Boolean DeleteReport(System.UInt64 reportId)`
- `System.Boolean LinkReport(System.UInt64 eventId, System.UInt64 reportId)`
- `System.Boolean SetEnabled(System.UInt64 eventId, System.Boolean enabled)`
- `System.Boolean UnlinkReport(System.UInt64 eventId, System.UInt64 reportId)`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Abstractions.Model.GemCollectionEventSnapshot> GetEventSnapshots()`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Abstractions.Model.GemReportDefinition> GetReportDefinitions()`
- `System.Threading.Tasks.ValueTask<Dreamine.Gem.Abstractions.Model.GemEventSnapshot> CollectAsync(System.UInt64 eventId, System.Threading.CancellationToken cancellationToken)`
- `System.Void DefineEvent(Dreamine.Gem.Abstractions.Model.GemCollectionEventDefinition collectionEvent)`
- `System.Void DefineReport(Dreamine.Gem.Abstractions.Model.GemReportDefinition report)`

### `public sealed class Dreamine.Gem.Services.GemProcessProgramService`

- `GemProcessProgramService()`
- `System.Boolean Delete(System.String id)`
- `System.Boolean TryGet(System.String id, out Dreamine.Gem.Abstractions.Model.GemProcessProgram program)`
- `System.Collections.Generic.IReadOnlyList<System.String> GetIds()`
- `System.Void Put(Dreamine.Gem.Abstractions.Model.GemProcessProgram program)`

### `public sealed class Dreamine.Gem.Services.GemRemoteCommandService`

- `GemRemoteCommandService(System.TimeProvider timeProvider, System.Func<Dreamine.Gem.Abstractions.States.GemControlState> controlState)`
- `System.Threading.Tasks.ValueTask<Dreamine.Gem.Abstractions.Model.GemCommandResult> ExecuteAsync(System.String name, System.Collections.Generic.IReadOnlyDictionary<System.String, Dreamine.Secs.Abstractions.Model.SecsItem> parameters, System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken)`
- `System.Void Register(Dreamine.Gem.Abstractions.Model.GemRemoteCommandDefinition definition, System.Func<System.Collections.Generic.IReadOnlyDictionary<System.String, Dreamine.Secs.Abstractions.Model.SecsItem>, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<Dreamine.Gem.Abstractions.Model.GemCommandResult>> handler)`
- `System.Void RegisterProfileCommand(Dreamine.Gem.Abstractions.Model.GemRemoteCommandProfileDefinition definition, System.Func<System.Collections.Generic.IReadOnlyDictionary<System.String, Dreamine.Secs.Abstractions.Model.SecsItem>, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<Dreamine.Gem.Abstractions.Model.GemCommandResult>> handler)`

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
- `System.Boolean TryGetDefinition(System.UInt64 id, out Dreamine.Gem.Abstractions.Model.GemVariableDefinition definition)`
- `System.Collections.Generic.IReadOnlyList<Dreamine.Gem.Abstractions.Model.GemVariableDefinition> GetDefinitions(System.Nullable<Dreamine.Gem.Abstractions.States.GemVariableKind> kind)`
- `System.Threading.Tasks.ValueTask<Dreamine.Secs.Abstractions.Model.SecsItem> ReadAsync(System.UInt64 id, System.Threading.CancellationToken cancellationToken)`
- `System.Void Register(Dreamine.Gem.Abstractions.Model.GemVariableDefinition definition, Dreamine.Secs.Abstractions.Model.SecsItemFormat format, System.Func<System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<Dreamine.Secs.Abstractions.Model.SecsItem>> reader)`
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
- `HsmsGemTransport(Dreamine.Secs.Abstractions.Interfaces.ISecsMessageSession session)`
- `HsmsGemTransport(Dreamine.Secs.Com.Hsms.HsmsSession session, Dreamine.Secs.Abstractions.Model.SecsSessionId sessionId)`
- `System.Threading.Tasks.Task SendAsync(Dreamine.Secs.Abstractions.Model.SecsMessage message, System.Threading.CancellationToken cancellationToken)`
- `System.Threading.Tasks.Task<Dreamine.Secs.Abstractions.Model.SecsMessage> RequestAsync(Dreamine.Secs.Abstractions.Model.SecsMessage message, System.Threading.CancellationToken cancellationToken)`
- `event System.EventHandler<Dreamine.Secs.Abstractions.Model.SecsMessage> MessageReceived`
