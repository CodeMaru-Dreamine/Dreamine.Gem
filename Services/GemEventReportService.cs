using System.Collections.Concurrent;
using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Abstractions.Model;

namespace Dreamine.Gem.Services;

/// <summary>\if KO 동적 보고서 연결과 이벤트 데이터 수집을 관리합니다. \endif \if EN Manages dynamic report links and event data collection. \endif</summary>
public sealed class GemEventReportService : IGemEventReportService
{
    private readonly IGemVariableCatalog _variables;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<ulong, GemReportDefinition> _reports = new();
    private readonly ConcurrentDictionary<ulong, EventEntry> _events = new();
    private readonly object _definitionGate = new();
    /// <summary>\if KO 변수 카탈로그와 시간 공급자로 서비스를 만듭니다. \endif \if EN Creates the service with a variable catalog and time provider. \endif</summary>
    public GemEventReportService(IGemVariableCatalog variables, TimeProvider? timeProvider = null) { _variables = variables ?? throw new ArgumentNullException(nameof(variables)); _timeProvider = timeProvider ?? TimeProvider.System; }
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
            if (!_events.TryGetValue(eventId, out var entry) || !_reports.ContainsKey(reportId)) return false;
            return entry.ReportIds.Add(reportId);
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
            return _reports.TryRemove(reportId, out _);
        }
    }
    /// <inheritdoc />
    public bool SetEnabled(ulong eventId, bool enabled)
    {
        if (!_events.TryGetValue(eventId, out var entry)) return false;
        lock (entry.Gate) entry.Enabled = enabled;
        return true;
    }
    /// <inheritdoc />
    public async ValueTask<GemEventSnapshot?> CollectAsync(ulong eventId, CancellationToken cancellationToken = default)
    {
        if (!_events.TryGetValue(eventId, out var entry)) throw new KeyNotFoundException($"Event {eventId} is not defined.");
        lock (entry.Gate) if (!entry.Enabled) return null;
        ulong[] reportIds;
        lock (_definitionGate) reportIds = entry.ReportIds.ToArray();
        var ids = reportIds.Select(id => _reports[id]).SelectMany(static report => report.VariableIds).Distinct().ToArray();
        var values = new Dictionary<ulong, Dreamine.Secs.Abstractions.Model.SecsItem>(ids.Length);
        foreach (var id in ids) values.Add(id, await _variables.ReadAsync(id, cancellationToken).ConfigureAwait(false));
        return new(eventId, _timeProvider.GetUtcNow(), values);
    }
    private sealed class EventEntry
    {
        public EventEntry(GemCollectionEventDefinition definition) { Definition = definition; Enabled = definition.Enabled; ReportIds = new(definition.ReportIds); }
        public object Gate { get; } = new();
        public GemCollectionEventDefinition Definition { get; }
        public HashSet<ulong> ReportIds { get; }
        public bool Enabled { get; set; }
    }
}
