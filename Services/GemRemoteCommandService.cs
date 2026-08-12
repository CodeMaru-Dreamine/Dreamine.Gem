using System.Collections.Concurrent;
using System.Collections.ObjectModel;
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
        Add(new(definition, null, handler));
    }

    /// <summary>\if KO 필수/선택 및 형식화된 매개변수를 가진 프로필 명령을 등록합니다. \endif \if EN Registers a profile command with required/optional typed parameters. \endif</summary>
    public void RegisterProfileCommand(GemRemoteCommandProfileDefinition definition, Func<IReadOnlyDictionary<string, SecsItem>, CancellationToken, ValueTask<GemCommandResult>> handler)
    {
        ArgumentNullException.ThrowIfNull(definition); ArgumentNullException.ThrowIfNull(handler);
        Add(new(null, definition, handler));
    }

    /// <inheritdoc />
    public async ValueTask<GemCommandResult> ExecuteAsync(string name, IReadOnlyDictionary<string, SecsItem> parameters, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name); ArgumentNullException.ThrowIfNull(parameters);
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        cancellationToken.ThrowIfCancellationRequested();
        if (_controlState() is not GemControlState.OnlineRemote) return new(GemCommandStatus.NotAllowed, "Remote commands require online-remote control.");
        if (!_entries.TryGetValue(name, out var entry)) return new(GemCommandStatus.NotAllowed, "Unknown command.");
        if (!ValidateParameters(entry, parameters)) return new(GemCommandStatus.InvalidParameter, "One or more parameters are missing, unknown, incorrectly formatted, or invalid.");

        var parameterSnapshot = new ReadOnlyDictionary<string, SecsItem>(new Dictionary<string, SecsItem>(parameters, StringComparer.Ordinal));
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task<GemCommandResult> operation;
        try
        {
            // Application code intentionally runs without holding registry or state locks.
            operation = entry.Handler(parameterSnapshot, linkedCancellation.Token).AsTask();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new(GemCommandStatus.Failed, $"The command failed: {exception.GetType().Name}.");
        }

        try
        {
            var result = await operation.WaitAsync(timeout, _timeProvider, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return result ?? new(GemCommandStatus.Failed, "The command handler returned no result.");
        }
        catch (TimeoutException)
        {
            linkedCancellation.Cancel();
            ObserveLateFault(operation);
            return new(GemCommandStatus.Failed, "The command timed out.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // WaitAsync and the linked source both observe the caller token. Explicitly
            // cancel here so a fast WaitAsync continuation cannot dispose the linked
            // source before its propagation callback reaches the application handler.
            linkedCancellation.Cancel();
            ObserveLateFault(operation);
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new(GemCommandStatus.Failed, $"The command failed: {exception.GetType().Name}.");
        }
    }

    private void Add(Entry entry)
    {
        if (!_entries.TryAdd(entry.Name, entry)) throw new InvalidOperationException($"Command '{entry.Name}' is already registered.");
    }

    private static bool ValidateParameters(Entry entry, IReadOnlyDictionary<string, SecsItem> parameters)
    {
        if (parameters.Any(static pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null)) return false;
        if (entry.ProfileDefinition is null)
        {
            var names = entry.LegacyDefinition!.Parameters;
            return names.All(parameters.ContainsKey) && parameters.Keys.All(parameter => names.Contains(parameter, StringComparer.Ordinal));
        }

        var definitions = entry.ProfileDefinition.Parameters.ToDictionary(static value => value.Name, StringComparer.Ordinal);
        if (entry.ProfileDefinition.Parameters.Any(parameter => parameter.Required && !parameters.ContainsKey(parameter.Name))) return false;
        if (parameters.Keys.Any(parameter => !definitions.ContainsKey(parameter))) return false;
        foreach (var pair in parameters)
        {
            var definition = definitions[pair.Key];
            if (pair.Value.Format != definition.Format) return false;
            if (definition.Validator is not null)
            {
                try { if (!definition.Validator(pair.Value)) return false; }
                catch { return false; }
            }
        }
        return true;
    }

    private static void ObserveLateFault(Task operation)
    {
        _ = operation.ContinueWith(
            static completed => _ = completed.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private sealed record Entry(
        GemRemoteCommandDefinition? LegacyDefinition,
        GemRemoteCommandProfileDefinition? ProfileDefinition,
        Func<IReadOnlyDictionary<string, SecsItem>, CancellationToken, ValueTask<GemCommandResult>> Handler)
    {
        public string Name => ProfileDefinition?.Name ?? LegacyDefinition!.Name;
    }
}
