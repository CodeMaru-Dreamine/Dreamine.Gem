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
    private readonly object _stateGate = new();

    /// <inheritdoc />
    public void Register(GemEquipmentConstantDefinition definition, Func<SecsItem, bool>? validator = null, Func<GemControlState, bool>? statePolicy = null)
        => RegisterCore(definition, null, validator, statePolicy);

    /// <summary>\if KO 정확한 SECS Item 형식을 포함해 상수를 등록합니다. \endif \if EN Registers a constant with an exact SECS item format. \endif</summary>
    public void RegisterTyped(
        GemEquipmentConstantDefinition definition,
        SecsItemFormat format,
        Func<SecsItem, bool>? validator = null,
        Func<GemControlState, bool>? statePolicy = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!Enum.IsDefined(format)) throw new ArgumentOutOfRangeException(nameof(format));
        if (definition.DefaultValue.Format != format)
            throw new ArgumentException("The default value does not match the declared format.", nameof(format));
        RegisterCore(definition, format, validator, statePolicy);
    }

    /// <inheritdoc />
    public bool TryGetValue(ulong id, out SecsItem? value)
    {
        lock (_stateGate)
        {
            if (_entries.TryGetValue(id, out var entry)) { value = entry.Value; return true; }
            value = null; return false;
        }
    }

    /// <inheritdoc />
    public bool TrySetValue(ulong id, SecsItem value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!_entries.TryGetValue(id, out var entry) || !ValidateValue(entry, value)) return false;
        lock (_stateGate) entry.Value = value;
        return true;
    }

    /// <inheritdoc />
    public GemConstantSetStatus SetValue(ulong id, SecsItem value, GemControlState controlState)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!_entries.TryGetValue(id, out var entry)) return GemConstantSetStatus.Unknown;
        if (!ValidateValue(entry, value)) return GemConstantSetStatus.ValidationFailed;
        if (entry.StatePolicy is not null && !InvokeStatePolicy(entry.StatePolicy, controlState, entry.GeneralizeCallbackFailure)) return GemConstantSetStatus.PolicyDenied;
        lock (_stateGate) entry.Value = value;
        return GemConstantSetStatus.Updated;
    }

    /// <summary>\if KO 모든 항목을 먼저 stage·검증한 후 한 번에 적용합니다. 실패 시 어떤 값도 변경하지 않습니다. \endif \if EN Stages and validates every entry before applying them together; no value changes on failure. \endif</summary>
    public GemConstantBatchResult SetValues(IEnumerable<GemEquipmentConstantUpdate> updates, GemControlState controlState)
    {
        ArgumentNullException.ThrowIfNull(updates);
        var staged = updates.ToArray();
        if (staged.Length == 0) throw new ArgumentException("At least one update is required.", nameof(updates));
        if (staged.Any(static update => update is null)) throw new ArgumentException("Updates cannot contain null.", nameof(updates));

        var seen = new HashSet<ulong>();
        foreach (var update in staged)
            if (!seen.Add(update.Id)) return new(GemConstantBatchStatus.Duplicate, update.Id);

        var entries = new Entry[staged.Length];
        lock (_stateGate)
        {
            for (var index = 0; index < staged.Length; index++)
            {
                if (!_entries.TryGetValue(staged[index].Id, out var entry))
                    return new(GemConstantBatchStatus.Unknown, staged[index].Id);
                entries[index] = entry;
            }
        }

        // User validators and policies intentionally run outside the state lock.
        for (var index = 0; index < staged.Length; index++)
        {
            if (!ValidateValue(entries[index], staged[index].Value))
                return new(GemConstantBatchStatus.ValidationFailed, staged[index].Id);
            if (entries[index].StatePolicy is not null && !InvokeStatePolicy(entries[index].StatePolicy!, controlState, entries[index].GeneralizeCallbackFailure))
                return new(GemConstantBatchStatus.PolicyDenied, staged[index].Id);
        }

        lock (_stateGate)
            for (var index = 0; index < staged.Length; index++)
                entries[index].Value = staged[index].Value;
        return new(GemConstantBatchStatus.Updated);
    }

    /// <summary>\if KO ECID 순서의 정의·현재값 불변 스냅샷을 반환합니다. \endif \if EN Returns an immutable definition/current-value snapshot ordered by ECID. \endif</summary>
    public IReadOnlyList<GemEquipmentConstantSnapshot> GetSnapshots()
    {
        lock (_stateGate)
            return Array.AsReadOnly(_entries.Values
                .OrderBy(static entry => entry.Definition.Id)
                .Select(static entry => new GemEquipmentConstantSnapshot(entry.Definition, entry.Value))
                .ToArray());
    }

    private void RegisterCore(
        GemEquipmentConstantDefinition definition,
        SecsItemFormat? format,
        Func<SecsItem, bool>? validator,
        Func<GemControlState, bool>? statePolicy)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var generalizeCallbackFailure = format is not null;
        if (validator is not null && !InvokeValidator(validator, definition.DefaultValue, generalizeCallbackFailure))
            throw new ArgumentException("The default value does not satisfy the validator.", nameof(definition));
        lock (_stateGate)
            if (!_entries.TryAdd(definition.Id, new(definition, format, validator, statePolicy, generalizeCallbackFailure)))
                throw new InvalidOperationException($"Equipment constant {definition.Id} is already registered.");
    }

    private static bool ValidateValue(Entry entry, SecsItem value) =>
        (entry.Format is null || value.Format == entry.Format) &&
        (entry.Validator is null || InvokeValidator(entry.Validator, value, entry.GeneralizeCallbackFailure));

    private static bool InvokeValidator(Func<SecsItem, bool> validator, SecsItem value, bool generalizeFailure)
    {
        if (!generalizeFailure) return validator(value);
        try { return validator(value); } catch { return false; }
    }

    private static bool InvokeStatePolicy(Func<GemControlState, bool> policy, GemControlState state, bool generalizeFailure = true)
    {
        if (!generalizeFailure) return policy(state);
        try { return policy(state); } catch { return false; }
    }

    private sealed class Entry
    {
        public Entry(
            GemEquipmentConstantDefinition definition,
            SecsItemFormat? format,
            Func<SecsItem, bool>? validator,
            Func<GemControlState, bool>? statePolicy,
            bool generalizeCallbackFailure)
        {
            Definition = definition;
            Value = definition.DefaultValue;
            Format = format;
            Validator = validator;
            StatePolicy = statePolicy;
            GeneralizeCallbackFailure = generalizeCallbackFailure;
        }

        public GemEquipmentConstantDefinition Definition { get; }
        public SecsItemFormat? Format { get; }
        public Func<SecsItem, bool>? Validator { get; }
        public Func<GemControlState, bool>? StatePolicy { get; }
        public bool GeneralizeCallbackFailure { get; }
        public SecsItem Value { get; set; }
    }
}
