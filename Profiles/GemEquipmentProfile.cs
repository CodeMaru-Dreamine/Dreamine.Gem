using System.Collections.ObjectModel;
using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Abstractions.States;
using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.Gem.Profiles;

/// <summary>\if KO E30-0611에서 파생한 명시적 subset v1의 불변 장비 프로필입니다. 전체 GEM 적합성 선언이 아닙니다. \endif \if EN Represents an immutable equipment profile for the explicit E30-0611-derived subset v1; it is not a full GEM conformance declaration. \endif</summary>
public sealed class GemEquipmentProfile
{
    private static readonly ReadOnlyCollection<GemProfileCapability> ClosedV1Capabilities = Array.AsReadOnly(
    [
        GemProfileCapability.CommunicationAndControl,
        GemProfileCapability.StatusAndDataVariables,
        GemProfileCapability.EquipmentConstants,
        GemProfileCapability.Alarms,
        GemProfileCapability.CollectionEventsAndReports,
        GemProfileCapability.RemoteCommands,
        GemProfileCapability.Clock
    ]);

    private readonly ReadOnlyCollection<GemVariableProfileDefinition> _variables;
    private readonly ReadOnlyCollection<GemEquipmentConstantProfileDefinition> _equipmentConstants;
    private readonly ReadOnlyCollection<GemReportDefinition> _reports;
    private readonly ReadOnlyCollection<GemCollectionEventDefinition> _collectionEvents;
    private readonly ReadOnlyCollection<GemAlarmDefinition> _alarms;
    private readonly ReadOnlyCollection<GemRemoteCommandProfileEntry> _remoteCommands;

    internal GemEquipmentProfile(
        string name,
        GemEquipmentIdentity identity,
        string targetRevision,
        GemIdentifierFormatPolicy identifierFormats,
        GemVariableProfileDefinition[] variables,
        GemEquipmentConstantProfileDefinition[] equipmentConstants,
        GemReportDefinition[] reports,
        GemCollectionEventDefinition[] collectionEvents,
        GemAlarmDefinition[] alarms,
        GemRemoteCommandProfileEntry[] remoteCommands)
    {
        Name = name;
        Identity = identity;
        TargetRevision = targetRevision;
        IdentifierFormats = identifierFormats;
        _variables = Array.AsReadOnly(variables);
        _equipmentConstants = Array.AsReadOnly(equipmentConstants);
        _reports = Array.AsReadOnly(reports);
        _collectionEvents = Array.AsReadOnly(collectionEvents);
        _alarms = Array.AsReadOnly(alarms);
        _remoteCommands = Array.AsReadOnly(remoteCommands);
    }

    /// <summary>\if KO subset v1의 고정 기능 스냅샷입니다. Process Program, Spool, Trace, Limits 및 Terminal은 포함하지 않습니다. \endif \if EN Gets the fixed subset-v1 capability snapshot; Process Program, Spool, Trace, Limits, and Terminal are not included. \endif</summary>
    public static IReadOnlyList<GemProfileCapability> V1Capabilities => ClosedV1Capabilities;

    /// <summary>\if KO 프로필 이름입니다. \endif \if EN Gets the profile name. \endif</summary>
    public string Name { get; }

    /// <summary>\if KO 장비 식별 정보입니다. \endif \if EN Gets the equipment identity. \endif</summary>
    public GemEquipmentIdentity Identity { get; }

    /// <summary>\if KO 명시적 대상 Revision/범위 라벨입니다. \endif \if EN Gets the explicit target revision/scope label. \endif</summary>
    public string TargetRevision { get; }

    /// <summary>\if KO 식별자 계열별 형식 정책입니다. \endif \if EN Gets the identifier-family format policy. \endif</summary>
    public GemIdentifierFormatPolicy IdentifierFormats { get; }

    /// <summary>\if KO 고정 기능 스냅샷입니다. \endif \if EN Gets the frozen capability snapshot. \endif</summary>
    public IReadOnlyList<GemProfileCapability> Capabilities => ClosedV1Capabilities;

    /// <summary>\if KO ID 순서의 변수 정의 스냅샷입니다. \endif \if EN Gets variable definitions ordered by ID. \endif</summary>
    public IReadOnlyList<GemVariableProfileDefinition> Variables => _variables;

    /// <summary>\if KO ECID 순서의 장비 상수 정의 스냅샷입니다. \endif \if EN Gets equipment-constant definitions ordered by ECID. \endif</summary>
    public IReadOnlyList<GemEquipmentConstantProfileDefinition> EquipmentConstants => _equipmentConstants;

    /// <summary>\if KO RPTID 순서의 기본 보고서 스냅샷입니다. \endif \if EN Gets default reports ordered by RPTID. \endif</summary>
    public IReadOnlyList<GemReportDefinition> Reports => _reports;

    /// <summary>\if KO CEID 순서의 기본 수집 이벤트 스냅샷입니다. \endif \if EN Gets default collection events ordered by CEID. \endif</summary>
    public IReadOnlyList<GemCollectionEventDefinition> CollectionEvents => _collectionEvents;

    /// <summary>\if KO ALID 순서의 알람 정의 스냅샷입니다. \endif \if EN Gets alarm definitions ordered by ALID. \endif</summary>
    public IReadOnlyList<GemAlarmDefinition> Alarms => _alarms;

    /// <summary>\if KO 이름 순서의 원격 명령 정의 스냅샷입니다. \endif \if EN Gets remote-command definitions ordered by name. \endif</summary>
    public IReadOnlyList<GemRemoteCommandProfileEntry> RemoteCommands => _remoteCommands;

    /// <summary>\if KO 이 frozen 프로필에서 완전히 격리된 mutable 장비 컨텍스트를 만듭니다. 전송 소유권은 호출자에게 있습니다. \endif \if EN Creates a fully isolated mutable equipment context from this frozen profile; the caller owns the transport. \endif</summary>
    public GemEquipmentContext CreateContext(IGemMessageTransport transport, TimeProvider? timeProvider = null, int spoolCapacity = 1024) =>
        CreateContextCore(transport, timeProvider, spoolCapacity);

    private GemEquipmentContext CreateContextCore(IGemMessageTransport transport, TimeProvider? timeProvider, int spoolCapacity)
    {
        var runtime = new GemRuntime(transport, Identity, timeProvider, spoolCapacity);
        runtime.ApplyProfile(this);
        return new(this, runtime);
    }

    internal void Configure(GemRuntime runtime)
    {
        foreach (var variable in _variables)
            runtime.Variables.Register(variable.Definition, variable.Format, variable.Reader);
        foreach (var constant in _equipmentConstants)
        {
            var allowed = constant.AllowedControlStates.ToHashSet();
            runtime.Constants.RegisterTyped(constant.Definition, constant.Format, constant.Validator, allowed.Contains);
        }
        foreach (var report in _reports) runtime.Events.DefineReport(report);
        foreach (var collectionEvent in _collectionEvents) runtime.Events.DefineEvent(collectionEvent);
        foreach (var alarm in _alarms) runtime.Alarms.Register(alarm);
        foreach (var command in _remoteCommands) runtime.Commands.RegisterProfileCommand(command.Definition, command.Handler);
    }
}

/// <summary>\if KO frozen 프로필과 context-local GEM 런타임을 묶습니다. \endif \if EN Associates a frozen profile with a context-local GEM runtime. \endif</summary>
public sealed class GemEquipmentContext
{
    internal GemEquipmentContext(GemEquipmentProfile profile, GemRuntime runtime)
    {
        Profile = profile;
        Runtime = runtime;
    }

    /// <summary>\if KO 공유 가능한 불변 프로필입니다. \endif \if EN Gets the shareable immutable profile. \endif</summary>
    public GemEquipmentProfile Profile { get; }

    /// <summary>\if KO 이 컨텍스트만의 mutable 상태와 서비스를 소유하는 런타임입니다. \endif \if EN Gets the runtime owning mutable state and services for this context only. \endif</summary>
    public GemRuntime Runtime { get; }
}

/// <summary>\if KO E30-0611 파생 subset v1 프로필을 코드 우선으로 구성하고 한 번 freeze합니다. \endif \if EN Builds and freezes an E30-0611-derived subset-v1 profile through a code-first API. \endif</summary>
public sealed class GemEquipmentProfileBuilder
{
    private readonly List<GemVariableProfileDefinition> _variables = [];
    private readonly List<GemEquipmentConstantProfileDefinition> _equipmentConstants = [];
    private readonly List<GemReportDefinition> _reports = [];
    private readonly List<GemCollectionEventDefinition> _collectionEvents = [];
    private readonly List<GemAlarmDefinition> _alarms = [];
    private readonly List<GemRemoteCommandProfileEntry> _remoteCommands = [];
    private GemEquipmentProfile? _built;

    /// <summary>\if KO 프로필 builder를 만듭니다. 기본 식별자 형식은 모든 계열 U4입니다. \endif \if EN Creates a profile builder; every identifier family defaults to U4. \endif</summary>
    public GemEquipmentProfileBuilder(
        string name,
        GemEquipmentIdentity identity,
        string targetRevision = "E30-0611-derived subset v1",
        GemIdentifierFormatPolicy? identifierFormats = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetRevision);
        Name = name;
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        TargetRevision = targetRevision;
        IdentifierFormats = identifierFormats ?? new();
    }

    /// <summary>\if KO 프로필 이름입니다. \endif \if EN Gets the profile name. \endif</summary>
    public string Name { get; }

    /// <summary>\if KO 장비 식별 정보입니다. \endif \if EN Gets the equipment identity. \endif</summary>
    public GemEquipmentIdentity Identity { get; }

    /// <summary>\if KO 대상 Revision/범위 라벨입니다. \endif \if EN Gets the target revision/scope label. \endif</summary>
    public string TargetRevision { get; }

    /// <summary>\if KO 식별자 형식 정책입니다. \endif \if EN Gets the identifier format policy. \endif</summary>
    public GemIdentifierFormatPolicy IdentifierFormats { get; }

    /// <summary>\if KO 상태 또는 데이터 변수와 판독기를 추가합니다. \endif \if EN Adds a status or data variable and its reader. \endif</summary>
    public GemEquipmentProfileBuilder AddVariable(GemVariableDefinition definition, SecsItemFormat format, Func<CancellationToken, ValueTask<SecsItem>> reader)
    {
        EnsureMutable();
        _variables.Add(new(definition, format, reader));
        return this;
    }

    /// <summary>\if KO 장비 상수와 검증·제어 상태 정책을 추가합니다. \endif \if EN Adds an equipment constant with validation and control-state policy. \endif</summary>
    public GemEquipmentProfileBuilder AddEquipmentConstant(
        GemEquipmentConstantDefinition definition,
        SecsItemFormat format,
        Func<SecsItem, bool>? validator = null,
        IEnumerable<GemControlState>? allowedControlStates = null)
    {
        EnsureMutable();
        _equipmentConstants.Add(new(definition, format, validator, allowedControlStates));
        return this;
    }

    /// <summary>\if KO 기본 보고서를 추가합니다. \endif \if EN Adds a default report. \endif</summary>
    public GemEquipmentProfileBuilder AddReport(GemReportDefinition report)
    {
        EnsureMutable();
        _reports.Add(report ?? throw new ArgumentNullException(nameof(report)));
        return this;
    }

    /// <summary>\if KO 기본 수집 이벤트를 추가합니다. \endif \if EN Adds a default collection event. \endif</summary>
    public GemEquipmentProfileBuilder AddCollectionEvent(GemCollectionEventDefinition collectionEvent)
    {
        EnsureMutable();
        _collectionEvents.Add(collectionEvent ?? throw new ArgumentNullException(nameof(collectionEvent)));
        return this;
    }

    /// <summary>\if KO 알람을 추가합니다. \endif \if EN Adds an alarm. \endif</summary>
    public GemEquipmentProfileBuilder AddAlarm(GemAlarmDefinition alarm)
    {
        EnsureMutable();
        _alarms.Add(alarm ?? throw new ArgumentNullException(nameof(alarm)));
        return this;
    }

    /// <summary>\if KO 형식화된 원격 명령과 실행기를 추가합니다. \endif \if EN Adds a typed remote command and handler. \endif</summary>
    public GemEquipmentProfileBuilder AddRemoteCommand(
        GemRemoteCommandProfileDefinition definition,
        Func<IReadOnlyDictionary<string, SecsItem>, CancellationToken, ValueTask<GemCommandResult>> handler)
    {
        EnsureMutable();
        _remoteCommands.Add(new(definition, handler));
        return this;
    }

    /// <summary>\if KO 모든 참조·중복·범위를 검증하고 프로필을 영구 freeze합니다. \endif \if EN Validates every reference, duplicate, and range and permanently freezes the profile. \endif</summary>
    public GemEquipmentProfile Build()
    {
        if (_built is not null) return _built;
        Validate();
        _built = new(
            Name,
            Identity,
            TargetRevision,
            IdentifierFormats,
            _variables.OrderBy(static value => value.Definition.Id).ToArray(),
            _equipmentConstants.OrderBy(static value => value.Definition.Id).ToArray(),
            _reports.OrderBy(static value => value.Id).ToArray(),
            _collectionEvents.OrderBy(static value => value.Id).ToArray(),
            _alarms.OrderBy(static value => value.Id).ToArray(),
            _remoteCommands.OrderBy(static value => value.Definition.Name, StringComparer.Ordinal).ToArray());
        return _built;
    }

    private void Validate()
    {
        EnsureUnique(_variables.Select(static value => value.Definition.Id), "variable ID");
        EnsureUnique(_variables.Select(static value => value.Definition.Name), "variable name");
        EnsureUnique(_equipmentConstants.Select(static value => value.Definition.Id), "equipment-constant ID");
        EnsureUnique(_equipmentConstants.Select(static value => value.Definition.Name), "equipment-constant name");
        EnsureUnique(_reports.Select(static value => value.Id), "report ID");
        EnsureUnique(_collectionEvents.Select(static value => value.Id), "collection-event ID");
        EnsureUnique(_collectionEvents.Select(static value => value.Name), "collection-event name");
        EnsureUnique(_alarms.Select(static value => value.Id), "alarm ID");
        EnsureUnique(_remoteCommands.Select(static value => value.Definition.Name), "remote-command name");

        foreach (var variable in _variables)
        {
            EnsureIdentifier(GemIdentifierFamily.Variable, variable.Definition.Id);
            EnsureIdentifier(variable.Definition.Kind == GemVariableKind.Status
                ? GemIdentifierFamily.StatusVariable
                : GemIdentifierFamily.DataVariable, variable.Definition.Id);
        }
        foreach (var constant in _equipmentConstants) EnsureIdentifier(GemIdentifierFamily.EquipmentConstant, constant.Definition.Id);
        foreach (var report in _reports) EnsureIdentifier(GemIdentifierFamily.Report, report.Id);
        foreach (var collectionEvent in _collectionEvents) EnsureIdentifier(GemIdentifierFamily.CollectionEvent, collectionEvent.Id);
        foreach (var alarm in _alarms) EnsureIdentifier(GemIdentifierFamily.Alarm, alarm.Id);

        var variableIds = _variables.Select(static value => value.Definition.Id).ToHashSet();
        foreach (var report in _reports)
            foreach (var variableId in report.VariableIds)
                if (!variableIds.Contains(variableId)) throw new InvalidOperationException($"Report {report.Id} references undefined variable {variableId}.");
        var reportIds = _reports.Select(static value => value.Id).ToHashSet();
        foreach (var collectionEvent in _collectionEvents)
            foreach (var reportId in collectionEvent.ReportIds)
                if (!reportIds.Contains(reportId)) throw new InvalidOperationException($"Collection event {collectionEvent.Id} references undefined report {reportId}.");

        foreach (var constant in _equipmentConstants)
            if (constant.Validator is not null)
            {
                bool accepted;
                try { accepted = constant.Validator(constant.Definition.DefaultValue); }
                catch (Exception exception) { throw new InvalidOperationException($"Equipment constant {constant.Definition.Id} default validator threw an exception.", exception); }
                if (!accepted) throw new InvalidOperationException($"Equipment constant {constant.Definition.Id} default value is invalid.");
            }
    }

    private void EnsureIdentifier(GemIdentifierFamily family, ulong value)
    {
        if (!IdentifierFormats.IsValid(family, value))
            throw new InvalidOperationException($"Identifier {value} does not fit {family} format {IdentifierFormats.GetFormat(family)}.");
    }

    private void EnsureMutable()
    {
        if (_built is not null) throw new InvalidOperationException("The profile builder is frozen.");
    }

    private static void EnsureUnique<T>(IEnumerable<T> values, string label) where T : notnull
    {
        var seen = typeof(T) == typeof(string)
            ? new HashSet<T>((IEqualityComparer<T>)StringComparer.Ordinal)
            : [];
        foreach (var value in values)
            if (!seen.Add(value)) throw new InvalidOperationException($"Duplicate {label} '{value}'.");
    }
}
