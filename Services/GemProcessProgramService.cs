using System.Collections.Concurrent;
using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Abstractions.Model;

namespace Dreamine.Gem.Services;

/// <summary>\if KO 불투명 PPBODY를 방어적으로 복사하는 메모리 공정 프로그램 저장소입니다. \endif \if EN Provides an in-memory process-program store with defensive PPBODY copies. \endif</summary>
public sealed class GemProcessProgramService : IGemProcessProgramService
{
    private readonly ConcurrentDictionary<string, GemProcessProgram> _programs = new(StringComparer.Ordinal);
    /// <inheritdoc />
    public void Put(GemProcessProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);
        _programs[program.Id] = new(program.Id, program.Body.Span);
    }
    /// <inheritdoc />
    public bool TryGet(string id, out GemProcessProgram? program)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (_programs.TryGetValue(id, out var stored)) { program = new(stored.Id, stored.Body.Span); return true; }
        program = null; return false;
    }
    /// <inheritdoc />
    public bool Delete(string id) { ArgumentException.ThrowIfNullOrWhiteSpace(id); return _programs.TryRemove(id, out _); }
    /// <inheritdoc />
    public IReadOnlyList<string> GetIds() => _programs.Keys.Order(StringComparer.Ordinal).ToArray();
}
