using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.Gem.Services;

/// <summary>\if KO 동적 보고서 연결과 이벤트 데이터 수집을 관리합니다. \endif \if EN Manages dynamic report links and event data collection. \endif</summary>
public sealed class GemEventReportService : IGemEventReportService
{
    private readonly IGemVariableCatalog _variables;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<ulong, GemReportDefinition> _reports = [];
    private readonly Dictionary<ulong, EventEntry> _events = [];
    private readonly object _definitionGate = new();

    /// <summary>\if KO 변수 카탈로그와 시간 공급자로 서비스를 만듭니다. \endif \if EN Creates the service with a variable catalog and time provider. \endif</summary>
    public GemEventReportService(IGemVariableCatalog variables, TimeProvider? timeProvider = null)
    {
        _variables = variables ?? throw new ArgumentNullException(nameof(variables));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public void DefineReport(GemReportDefinition report)
    {
        ArgumentNullException.ThrowIfNull(report);
        lock (_definitionGate)
            if (!_reports.TryAdd(report.Id, report)) throw new InvalidOperationException($"Report {report.Id} is already defined.");
    }

    /// <inheritdoc />
    public void DefineEvent(GemCollectionEventDefinition collectionEvent)
    {
        ArgumentNullException.ThrowIfNull(collectionEvent);
        lock (_definitionGate)
        {
            if (collectionEvent.ReportIds.Any(id => !_reports.ContainsKey(id))) throw new ArgumentException("The event links an undefined report.", nameof(collectionEvent));
            if (!_events.TryAdd(collectionEvent.Id, new(collectionEvent))) throw new InvalidOperationException($"Event {collectionEvent.Id} is already defined.");
        }
    }

    /// <inheritdoc />
    public bool LinkReport(ulong eventId, ulong reportId)
    {
        lock (_definitionGate)
        {
            if (!_events.TryGetValue(eventId, out var entry) || !_reports.ContainsKey(reportId) || entry.ReportIds.Contains(reportId)) return false;
            entry.ReportIds.Add(reportId);
            return true;
        }
    }

    /// <inheritdoc />
    public bool UnlinkReport(ulong eventId, ulong reportId)
    {
        lock (_definitionGate)
            return _events.TryGetValue(eventId, out var entry) && entry.ReportIds.Remove(reportId);
    }

    /// <inheritdoc />
    public bool DeleteReport(ulong reportId)
    {
        lock (_definitionGate)
        {
            if (_events.Values.Any(entry => entry.ReportIds.Contains(reportId))) return false;
            return _reports.Remove(reportId);
        }
    }

    /// <inheritdoc />
    public bool SetEnabled(ulong eventId, bool enabled)
    {
        lock (_definitionGate)
        {
            if (!_events.TryGetValue(eventId, out var entry)) return false;
            entry.Enabled = enabled;
            return true;
        }
    }

    /// <inheritdoc />
    public async ValueTask<GemEventSnapshot?> CollectAsync(ulong eventId, CancellationToken cancellationToken = default)
    {
        GemReportDefinition[] reports;
        lock (_definitionGate)
        {
            if (!_events.TryGetValue(eventId, out var entry)) throw new KeyNotFoundException($"Event {eventId} is not defined.");
            if (!entry.Enabled) return null;
            reports = entry.ReportIds.Select(id => _reports[id]).ToArray();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var values = new Dictionary<ulong, SecsItem>();
        var reportValues = new GemReportValueSnapshot[reports.Length];
        for (var reportIndex = 0; reportIndex < reports.Length; reportIndex++)
        {
            var report = reports[reportIndex];
            var ordered = new GemVariableValueSnapshot[report.VariableIds.Count];
            for (var valueIndex = 0; valueIndex < report.VariableIds.Count; valueIndex++)
            {
                var variableId = report.VariableIds[valueIndex];
                if (!values.TryGetValue(variableId, out var value))
                {
                    value = await _variables.ReadAsync(variableId, cancellationToken).ConfigureAwait(false);
                    values.Add(variableId, value);
                }
                ordered[valueIndex] = new(variableId, value);
            }
            reportValues[reportIndex] = new(report.Id, ordered);
        }
        return new(eventId, _timeProvider.GetUtcNow(), reportValues);
    }

    /// <summary>\if KO 보고서 추가와 삭제를 하나의 candidate 구성에서 검증하고 원자적으로 적용합니다. 삭제된 보고서의 이벤트 연결도 같은 commit에서 제거합니다. \endif \if EN Validates report additions and deletions in one candidate configuration and applies them atomically; event links to removed reports are pruned in the same commit. \endif</summary>
    public GemEventConfigurationResult ApplyReportChanges(
        IEnumerable<GemReportDefinition> definitions,
        IEnumerable<ulong> deleteIds,
        bool deleteAll = false)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(deleteIds);
        var additions = definitions.ToArray();
        var deletions = deleteIds.ToArray();
        if (additions.Any(static report => report is null)) throw new ArgumentException("Reports cannot contain null.", nameof(definitions));
        if (deletions.Any(static id => id == 0)) throw new ArgumentException("Report IDs must be positive.", nameof(deleteIds));
        if (deleteAll && deletions.Length != 0) throw new ArgumentException("deleteIds must be empty when deleteAll is true.", nameof(deleteIds));

        var addedIds = new HashSet<ulong>();
        foreach (var report in additions)
        {
            if (!addedIds.Add(report.Id)) return new(GemEventConfigurationStatus.Duplicate, report.Id);
            foreach (var variableId in report.VariableIds)
                if (!_variables.TryGetDefinition(variableId, out _))
                    return new(GemEventConfigurationStatus.UnknownVariable, variableId);
        }
        var deletedIds = new HashSet<ulong>();
        foreach (var reportId in deletions)
            if (!deletedIds.Add(reportId)) return new(GemEventConfigurationStatus.Duplicate, reportId);

        lock (_definitionGate)
        {
            var candidate = deleteAll
                ? new Dictionary<ulong, GemReportDefinition>()
                : new Dictionary<ulong, GemReportDefinition>(_reports);
            foreach (var reportId in deletions)
                if (!candidate.Remove(reportId)) return new(GemEventConfigurationStatus.UnknownReport, reportId);
            foreach (var report in additions)
            {
                if (candidate.ContainsKey(report.Id)) return new(GemEventConfigurationStatus.Duplicate, report.Id);
                candidate.Add(report.Id, report);
            }

            _reports.Clear();
            foreach (var pair in candidate) _reports.Add(pair.Key, pair.Value);
            foreach (var entry in _events.Values)
                entry.ReportIds.RemoveAll(reportId => !candidate.ContainsKey(reportId));
        }
        return new(GemEventConfigurationStatus.Applied);
    }

    /// <summary>\if KO 보고서 정의 묶음을 전체 검증한 뒤 원자적으로 추가합니다. Wire ACK를 생성하지 않습니다. \endif \if EN Validates a report-definition batch completely before adding it atomically; this does not produce a wire ACK. \endif</summary>
    public GemEventConfigurationResult DefineReports(IEnumerable<GemReportDefinition> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);
        var staged = reports.ToArray();
        if (staged.Any(static report => report is null)) throw new ArgumentException("Reports cannot contain null.", nameof(reports));
        var seen = new HashSet<ulong>();
        foreach (var report in staged)
        {
            if (!seen.Add(report.Id)) return new(GemEventConfigurationStatus.Duplicate, report.Id);
            foreach (var variableId in report.VariableIds)
                if (!_variables.TryGetDefinition(variableId, out _))
                    return new(GemEventConfigurationStatus.UnknownVariable, variableId);
        }
        lock (_definitionGate)
        {
            foreach (var report in staged)
                if (_reports.ContainsKey(report.Id)) return new(GemEventConfigurationStatus.Duplicate, report.Id);
            foreach (var report in staged) _reports.Add(report.Id, report);
        }
        return new(GemEventConfigurationStatus.Applied);
    }

    /// <summary>\if KO RPTID 묶음을 전체 검증한 뒤 원자적으로 삭제합니다. 연결 중인 보고서가 있으면 변경하지 않습니다. \endif \if EN Validates an RPTID batch before deleting it atomically; no change occurs when a selected report is linked. \endif</summary>
    public GemEventConfigurationResult DeleteReports(IEnumerable<ulong> reportIds)
    {
        ArgumentNullException.ThrowIfNull(reportIds);
        var staged = reportIds.ToArray();
        if (staged.Any(static id => id == 0)) throw new ArgumentException("Report IDs must be positive.", nameof(reportIds));
        var seen = new HashSet<ulong>();
        foreach (var reportId in staged)
            if (!seen.Add(reportId)) return new(GemEventConfigurationStatus.Duplicate, reportId);
        lock (_definitionGate)
        {
            foreach (var reportId in staged)
                if (!_reports.ContainsKey(reportId)) return new(GemEventConfigurationStatus.UnknownReport, reportId);
            foreach (var reportId in staged)
                if (_events.Values.Any(entry => entry.ReportIds.Contains(reportId)))
                    return new(GemEventConfigurationStatus.ReportInUse, reportId);
            foreach (var reportId in staged) _reports.Remove(reportId);
        }
        return new(GemEventConfigurationStatus.Applied);
    }

    /// <summary>\if KO 모든 보고서 정의와 이벤트 연결을 하나의 원자적 변경으로 제거합니다. \endif \if EN Removes every report definition and event link as one atomic change. \endif</summary>
    public GemEventConfigurationResult DeleteAllReports()
    {
        lock (_definitionGate)
        {
            _reports.Clear();
            foreach (var entry in _events.Values) entry.ReportIds.Clear();
        }
        return new(GemEventConfigurationStatus.Applied);
    }

    /// <summary>\if KO 모든 수집 이벤트의 보고서 연결을 하나의 원자적 변경으로 제거합니다. Wire 의미는 상위 프로필이 결정합니다. \endif \if EN Removes report links from every collection event as one atomic change; the upper profile decides wire semantics. \endif</summary>
    public GemEventConfigurationResult DeleteAllEventLinks()
    {
        lock (_definitionGate)
            foreach (var entry in _events.Values) entry.ReportIds.Clear();
        return new(GemEventConfigurationStatus.Applied);
    }

    /// <summary>\if KO 여러 이벤트의 보고서 연결을 전체 검증한 뒤 원자적으로 교체합니다. \endif \if EN Validates and atomically replaces report links for multiple events. \endif</summary>
    public GemEventConfigurationResult ReplaceEventLinks(IEnumerable<GemEventReportLinkUpdate> updates)
        => ApplyEventLinks(updates, rejectExisting: false, disableUpdated: false);

    /// <summary>\if KO 여러 이벤트의 연결을 검증하고 선택적으로 기존 연결을 거부하며, 연결 교체와 disable을 하나의 원자적 commit으로 적용합니다. \endif \if EN Validates links for multiple events, optionally rejects existing links, and applies link replacement plus disablement in one atomic commit. \endif</summary>
    public GemEventConfigurationResult ApplyEventLinks(
        IEnumerable<GemEventReportLinkUpdate> updates,
        bool rejectExisting,
        bool disableUpdated)
    {
        ArgumentNullException.ThrowIfNull(updates);
        var staged = updates.ToArray();
        if (staged.Any(static update => update is null)) throw new ArgumentException("Updates cannot contain null.", nameof(updates));
        var seen = new HashSet<ulong>();
        foreach (var update in staged)
            if (!seen.Add(update.EventId)) return new(GemEventConfigurationStatus.Duplicate, update.EventId);
        lock (_definitionGate)
        {
            foreach (var update in staged)
            {
                if (!_events.ContainsKey(update.EventId)) return new(GemEventConfigurationStatus.UnknownEvent, update.EventId);
                if (rejectExisting && _events[update.EventId].ReportIds.Count != 0)
                    return new(GemEventConfigurationStatus.ExistingLinks, update.EventId);
                foreach (var reportId in update.ReportIds)
                    if (!_reports.ContainsKey(reportId)) return new(GemEventConfigurationStatus.UnknownReport, reportId);
            }
            foreach (var update in staged)
            {
                var target = _events[update.EventId].ReportIds;
                target.Clear();
                target.AddRange(update.ReportIds);
                if (disableUpdated) _events[update.EventId].Enabled = false;
            }
        }
        return new(GemEventConfigurationStatus.Applied);
    }

    /// <summary>\if KO 여러 이벤트의 enable 상태를 전체 검증한 뒤 원자적으로 적용합니다. \endif \if EN Validates and atomically applies enablement updates for multiple events. \endif</summary>
    public GemEventConfigurationResult SetEventsEnabled(IEnumerable<GemEventEnableUpdate> updates)
    {
        ArgumentNullException.ThrowIfNull(updates);
        var staged = updates.ToArray();
        if (staged.Any(static update => update is null)) throw new ArgumentException("Updates cannot contain null.", nameof(updates));
        var seen = new HashSet<ulong>();
        foreach (var update in staged)
            if (!seen.Add(update.EventId)) return new(GemEventConfigurationStatus.Duplicate, update.EventId);
        lock (_definitionGate)
        {
            foreach (var update in staged)
                if (!_events.ContainsKey(update.EventId)) return new(GemEventConfigurationStatus.UnknownEvent, update.EventId);
            foreach (var update in staged) _events[update.EventId].Enabled = update.Enabled;
        }
        return new(GemEventConfigurationStatus.Applied);
    }

    /// <summary>\if KO 모든 이벤트의 enable 상태를 원자적으로 설정합니다. \endif \if EN Atomically sets enablement for every event. \endif</summary>
    public GemEventConfigurationResult SetAllEventsEnabled(bool enabled)
    {
        lock (_definitionGate)
            foreach (var entry in _events.Values) entry.Enabled = enabled;
        return new(GemEventConfigurationStatus.Applied);
    }

    /// <summary>\if KO RPTID 순서의 불변 보고서 정의 스냅샷을 반환합니다. \endif \if EN Returns immutable report definitions ordered by RPTID. \endif</summary>
    public IReadOnlyList<GemReportDefinition> GetReportDefinitions()
    {
        lock (_definitionGate) return Array.AsReadOnly(_reports.Values.OrderBy(static report => report.Id).ToArray());
    }

    /// <summary>\if KO CEID 순서의 정의·연결·enable 스냅샷을 반환합니다. \endif \if EN Returns definition/link/enablement snapshots ordered by CEID. \endif</summary>
    public IReadOnlyList<GemCollectionEventSnapshot> GetEventSnapshots()
    {
        lock (_definitionGate)
            return Array.AsReadOnly(_events.Values
                .OrderBy(static entry => entry.Definition.Id)
                .Select(static entry => new GemCollectionEventSnapshot(entry.Definition, entry.ReportIds, entry.Enabled))
                .ToArray());
    }

    private sealed class EventEntry
    {
        public EventEntry(GemCollectionEventDefinition definition)
        {
            Definition = definition;
            Enabled = definition.Enabled;
            ReportIds = [.. definition.ReportIds];
        }

        public GemCollectionEventDefinition Definition { get; }
        public List<ulong> ReportIds { get; }
        public bool Enabled { get; set; }
    }
}
