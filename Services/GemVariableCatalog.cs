using System.Collections.Concurrent;
using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Abstractions.States;
using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.Gem.Services;

/// <summary>\if KO 스레드 안전 GEM 변수 카탈로그입니다. \endif \if EN Provides a thread-safe GEM variable catalog. \endif</summary>
public sealed class GemVariableCatalog : IGemVariableCatalog
{
    private readonly ConcurrentDictionary<ulong, Entry> _entries = new();
    /// <inheritdoc />
    public void Register(GemVariableDefinition definition, Func<CancellationToken, ValueTask<SecsItem>> reader)
    {
        ArgumentNullException.ThrowIfNull(definition); ArgumentNullException.ThrowIfNull(reader);
        if (!_entries.TryAdd(definition.Id, new(definition, reader))) throw new InvalidOperationException($"Variable {definition.Id} is already registered.");
    }
    /// <inheritdoc />
    public bool TryGetDefinition(ulong id, out GemVariableDefinition? definition)
    {
        if (_entries.TryGetValue(id, out var entry)) { definition = entry.Definition; return true; }
        definition = null; return false;
    }
    /// <inheritdoc />
    public async ValueTask<SecsItem> ReadAsync(ulong id, CancellationToken cancellationToken = default)
    {
        if (!_entries.TryGetValue(id, out var entry)) throw new KeyNotFoundException($"Variable {id} is not registered.");
        cancellationToken.ThrowIfCancellationRequested();
        return await entry.Reader(cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("A variable reader returned null.");
    }
    /// <inheritdoc />
    public IReadOnlyList<GemVariableDefinition> GetDefinitions(GemVariableKind? kind = null) =>
        _entries.Values.Select(static entry => entry.Definition).Where(value => kind is null || value.Kind == kind).OrderBy(static value => value.Id).ToArray();
    private sealed record Entry(GemVariableDefinition Definition, Func<CancellationToken, ValueTask<SecsItem>> Reader);
}
