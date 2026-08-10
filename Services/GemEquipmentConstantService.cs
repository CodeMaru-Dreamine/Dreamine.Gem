using System.Collections.Concurrent;
using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Abstractions.States;
using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.Gem.Services;

/// <summary>\if KO 검증 가능한 스레드 안전 장비 상수 저장소입니다. \endif \if EN Provides a validated, thread-safe equipment-constant store. \endif</summary>
public sealed class GemEquipmentConstantService : IGemEquipmentConstantService
{
    private readonly ConcurrentDictionary<ulong, Entry> _entries = new();
    /// <inheritdoc />
    public void Register(GemEquipmentConstantDefinition definition, Func<SecsItem, bool>? validator = null, Func<GemControlState, bool>? statePolicy = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (validator is not null && !validator(definition.DefaultValue)) throw new ArgumentException("The default value does not satisfy the validator.", nameof(definition));
        if (!_entries.TryAdd(definition.Id, new(definition, validator, statePolicy))) throw new InvalidOperationException($"Equipment constant {definition.Id} is already registered.");
    }
    /// <inheritdoc />
    public bool TryGetValue(ulong id, out SecsItem? value)
    {
        if (_entries.TryGetValue(id, out var entry)) { lock (entry.Gate) value = entry.Value; return true; }
        value = null; return false;
    }
    /// <inheritdoc />
    public bool TrySetValue(ulong id, SecsItem value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!_entries.TryGetValue(id, out var entry) || (entry.Validator is not null && !entry.Validator(value))) return false;
        lock (entry.Gate) entry.Value = value;
        return true;
    }
    /// <inheritdoc />
    public GemConstantSetStatus SetValue(ulong id, SecsItem value, GemControlState controlState)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!_entries.TryGetValue(id, out var entry)) return GemConstantSetStatus.Unknown;
        if (entry.Validator is not null && !entry.Validator(value)) return GemConstantSetStatus.ValidationFailed;
        if (entry.StatePolicy is not null && !entry.StatePolicy(controlState)) return GemConstantSetStatus.PolicyDenied;
        lock (entry.Gate) entry.Value = value;
        return GemConstantSetStatus.Updated;
    }
    private sealed class Entry
    {
        public Entry(GemEquipmentConstantDefinition definition, Func<SecsItem, bool>? validator, Func<GemControlState, bool>? statePolicy) { Definition = definition; Value = definition.DefaultValue; Validator = validator; StatePolicy = statePolicy; }
        public object Gate { get; } = new();
        public GemEquipmentConstantDefinition Definition { get; }
        public Func<SecsItem, bool>? Validator { get; }
        public Func<GemControlState, bool>? StatePolicy { get; }
        public SecsItem Value { get; set; }
    }
}
