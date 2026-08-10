using System.Collections.Concurrent;
using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Abstractions.States;
using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.Gem.Services;

/// <summary>\if KO 취소와 주입 시간 기반 제한 시간을 지원하는 원격 명령 서비스입니다. \endif \if EN Provides remote commands with cancellation and injected-time timeouts. \endif</summary>
public sealed class GemRemoteCommandService : IGemRemoteCommandService
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;
    private readonly Func<GemControlState> _controlState;
    /// <summary>\if KO 시간 공급자로 서비스를 만듭니다. \endif \if EN Creates the service with a time provider. \endif</summary>
    public GemRemoteCommandService(TimeProvider? timeProvider = null, Func<GemControlState>? controlState = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _controlState = controlState ?? (static () => GemControlState.OnlineRemote);
    }
    /// <inheritdoc />
    public void Register(GemRemoteCommandDefinition definition, Func<IReadOnlyDictionary<string, SecsItem>, CancellationToken, ValueTask<GemCommandResult>> handler)
    {
        ArgumentNullException.ThrowIfNull(definition); ArgumentNullException.ThrowIfNull(handler);
        if (!_entries.TryAdd(definition.Name, new(definition, handler))) throw new InvalidOperationException($"Command '{definition.Name}' is already registered.");
    }
    /// <inheritdoc />
    public async ValueTask<GemCommandResult> ExecuteAsync(string name, IReadOnlyDictionary<string, SecsItem> parameters, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name); ArgumentNullException.ThrowIfNull(parameters);
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        if (_controlState() is not GemControlState.OnlineRemote) return new(GemCommandStatus.NotAllowed, "Remote commands require online-remote control.");
        if (!_entries.TryGetValue(name, out var entry)) return new(GemCommandStatus.NotAllowed, "Unknown command.");
        if (entry.Definition.Parameters.Any(parameter => !parameters.ContainsKey(parameter))) return new(GemCommandStatus.InvalidParameter, "A required parameter is missing.");
        if (parameters.Keys.Any(parameter => !entry.Definition.Parameters.Contains(parameter, StringComparer.Ordinal))) return new(GemCommandStatus.InvalidParameter, "An unknown parameter was supplied.");
        try
        {
            return await entry.Handler(parameters, cancellationToken).AsTask().WaitAsync(timeout, _timeProvider, cancellationToken).ConfigureAwait(false)
                ?? new(GemCommandStatus.Failed, "The command handler returned no result.");
        }
        catch (TimeoutException) { return new(GemCommandStatus.Failed, "The command timed out."); }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new(GemCommandStatus.Failed, $"The command failed: {exception.GetType().Name}.");
        }
    }
    private sealed record Entry(GemRemoteCommandDefinition Definition, Func<IReadOnlyDictionary<string, SecsItem>, CancellationToken, ValueTask<GemCommandResult>> Handler);
}
