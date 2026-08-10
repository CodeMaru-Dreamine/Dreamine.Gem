using System.Collections.Concurrent;
using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Abstractions.States;

namespace Dreamine.Gem.Services;

/// <summary>\if KO 알람 설정과 보고 활성화를 독립적으로 관리합니다. \endif \if EN Manages alarm set state independently from reporting enablement. \endif</summary>
public sealed class GemAlarmService : IGemAlarmService
{
    private readonly ConcurrentDictionary<ulong, Entry> _entries = new();
    /// <inheritdoc />
    public void Register(GemAlarmDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!_entries.TryAdd(definition.Id, new(definition))) throw new InvalidOperationException($"Alarm {definition.Id} is already registered.");
    }
    /// <inheritdoc />
    public bool SetEnabled(ulong id, bool enabled)
    {
        if (!_entries.TryGetValue(id, out var entry)) return false;
        lock (entry.Gate) entry.Enabled = enabled; return true;
    }
    /// <inheritdoc />
    public bool SetAlarm(ulong id, bool isSet)
        => ChangeAlarm(id, isSet) is not GemAlarmChangeStatus.Unknown;
    /// <inheritdoc />
    public GemAlarmChangeStatus ChangeAlarm(ulong id, bool isSet)
    {
        if (!_entries.TryGetValue(id, out var entry)) return GemAlarmChangeStatus.Unknown;
        lock (entry.Gate)
        {
            if (entry.IsSet == isSet) return GemAlarmChangeStatus.NoChange;
            entry.IsSet = isSet;
            return GemAlarmChangeStatus.Changed;
        }
    }
    /// <inheritdoc />
    public bool TryGetState(ulong id, out bool isSet)
    {
        if (_entries.TryGetValue(id, out var entry)) { lock (entry.Gate) isSet = entry.IsSet; return true; }
        isSet = false; return false;
    }
    /// <inheritdoc />
    public IReadOnlyList<ulong> GetSetAlarmIds() => _entries
        .Where(static pair => pair.Value.GetIsSet())
        .Select(static pair => pair.Key)
        .OrderBy(static id => id)
        .ToArray();
    private sealed class Entry
    {
        public Entry(GemAlarmDefinition definition) { Definition = definition; Enabled = definition.Enabled; }
        public object Gate { get; } = new(); public GemAlarmDefinition Definition { get; } public bool Enabled { get; set; } public bool IsSet { get; set; }
        public bool GetIsSet() { lock (Gate) return IsSet; }
    }
}
