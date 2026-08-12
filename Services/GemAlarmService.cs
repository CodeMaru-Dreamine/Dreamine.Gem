using System.Collections.Concurrent;
using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Abstractions.States;

namespace Dreamine.Gem.Services;

/// <summary>\if KO 알람 설정과 보고 활성화를 독립적으로 관리합니다. \endif \if EN Manages alarm set state independently from reporting enablement. \endif</summary>
public sealed class GemAlarmService : IGemAlarmService
{
    private readonly ConcurrentDictionary<ulong, Entry> _entries = new();
    private readonly object _stateGate = new();

    /// <inheritdoc />
    public void Register(GemAlarmDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        lock (_stateGate)
            if (!_entries.TryAdd(definition.Id, new(definition)))
                throw new InvalidOperationException($"Alarm {definition.Id} is already registered.");
    }

    /// <inheritdoc />
    public bool SetEnabled(ulong id, bool enabled) => ChangeEnabled(id, enabled) is not GemAlarmChangeStatus.Unknown;

    /// <summary>\if KO 보고 활성화를 변경하고 unknown/no-change/changed를 결정적으로 구분합니다. \endif \if EN Changes reporting enablement and deterministically distinguishes unknown, no-change, and changed results. \endif</summary>
    public GemAlarmChangeStatus ChangeEnabled(ulong id, bool enabled)
    {
        lock (_stateGate)
        {
            if (!_entries.TryGetValue(id, out var entry)) return GemAlarmChangeStatus.Unknown;
            if (entry.Enabled == enabled) return GemAlarmChangeStatus.NoChange;
            entry.Enabled = enabled;
            return GemAlarmChangeStatus.Changed;
        }
    }

    /// <inheritdoc />
    public bool SetAlarm(ulong id, bool isSet) => ChangeAlarm(id, isSet) is not GemAlarmChangeStatus.Unknown;

    /// <inheritdoc />
    public GemAlarmChangeStatus ChangeAlarm(ulong id, bool isSet)
    {
        lock (_stateGate)
        {
            if (!_entries.TryGetValue(id, out var entry)) return GemAlarmChangeStatus.Unknown;
            if (entry.IsSet == isSet) return GemAlarmChangeStatus.NoChange;
            entry.IsSet = isSet;
            return GemAlarmChangeStatus.Changed;
        }
    }

    /// <inheritdoc />
    public bool TryGetState(ulong id, out bool isSet)
    {
        lock (_stateGate)
        {
            if (_entries.TryGetValue(id, out var entry)) { isSet = entry.IsSet; return true; }
            isSet = false; return false;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ulong> GetSetAlarmIds()
    {
        lock (_stateGate)
            return _entries
                .Where(static pair => pair.Value.IsSet)
                .Select(static pair => pair.Key)
                .OrderBy(static id => id)
                .ToArray();
    }

    /// <summary>\if KO ALID 순서의 정의·enable·set 불변 스냅샷을 반환합니다. \endif \if EN Returns immutable definition/enable/set snapshots ordered by ALID. \endif</summary>
    public IReadOnlyList<GemAlarmSnapshot> GetSnapshots()
    {
        lock (_stateGate)
            return Array.AsReadOnly(_entries.Values
                .OrderBy(static entry => entry.Definition.Id)
                .Select(static entry => new GemAlarmSnapshot(entry.Definition, entry.Enabled, entry.IsSet))
                .ToArray());
    }

    private sealed class Entry
    {
        public Entry(GemAlarmDefinition definition) { Definition = definition; Enabled = definition.Enabled; }
        public GemAlarmDefinition Definition { get; }
        public bool Enabled { get; set; }
        public bool IsSet { get; set; }
    }
}
