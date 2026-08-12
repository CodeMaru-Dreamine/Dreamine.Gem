using System.Collections.Concurrent;
using System.Text.Json;
using Dreamine.Gem.Protocol.E30;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;

namespace Dreamine.Gem.QuickStart;

internal sealed class DemoWireEvidence : IAsyncDisposable
{
    private readonly IHsmsWireObservationSource _source;
    private readonly ConcurrentQueue<ObservedHeader> _headers = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Task _reader;

    public DemoWireEvidence(IHsmsWireObservationSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        if (!_source.IsWireObservationEnabled)
            throw new InvalidOperationException("Header-only wire observation must be enabled before creating the session.");
        _reader = ReadAsync();
    }

    public async Task<DemoWireSummary> WaitForCompleteAsync(
        ushort sessionId,
        SecsRole localRole,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(8);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsPotentiallyComplete(sessionId, localRole)) return BuildSummary(sessionId, localRole);
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);
        }
        return BuildSummary(sessionId, localRole);
    }

    public async ValueTask DisposeAsync()
    {
        _lifetime.Cancel();
        try { await _reader.ConfigureAwait(false); }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        finally { _lifetime.Dispose(); }
    }

    private async Task ReadAsync()
    {
        await foreach (var observation in _source.ReadWireObservationsAsync(_lifetime.Token).ConfigureAwait(false))
        {
            if (observation.Header is { IsData: true } header)
                _headers.Enqueue(new ObservedHeader(observation.SequenceNumber, observation.Direction, header));
        }
    }

    private bool IsPotentiallyComplete(ushort sessionId, SecsRole localRole)
    {
        try
        {
            _ = BuildSummary(sessionId, localRole);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private DemoWireSummary BuildSummary(ushort sessionId, SecsRole localRole)
    {
        if (_source.DroppedWireObservationCount != 0)
            throw new InvalidOperationException($"Wire observations were dropped: {_source.DroppedWireObservationCount}.");
        var frames = _headers.OrderBy(static value => value.Sequence).ToArray();
        if (frames.Any(value => value.Header.SessionId != sessionId))
            throw new InvalidOperationException("A data frame used an unexpected Session ID.");

        var expected = E30DerivedSubsetManifest.IncludedDialogues
            .Select(static dialogue => (Stream: dialogue.Stream.Value, Function: dialogue.PrimaryFunction.Value))
            .ToHashSet();
        var directionalRequirements = BuildDirectionalRequirements(expected).ToArray();
        var primaries = frames.Where(static value => value.Header.ReplyExpected).ToArray();
        var unexpected = primaries
            .Select(static value => (value.Header.Stream, value.Header.Function))
            .Where(value => !expected.Contains(value))
            .Distinct()
            .ToArray();
        if (unexpected.Length != 0)
            throw new InvalidOperationException($"Unexpected W1 primary S{unexpected[0].Stream}F{unexpected[0].Function}.");
        var missingDirections = directionalRequirements.Where(requirement => !primaries.Any(observed =>
                observed.Header.Stream == requirement.Stream &&
                observed.Header.Function == requirement.Function &&
                observed.Direction == GetLocalDirection(localRole, requirement.Initiator)))
            .ToArray();
        if (missingDirections.Length != 0)
        {
            var missing = missingDirections[0];
            throw new InvalidOperationException(
                $"Missing {missing.Initiator}-initiated primary S{missing.Stream}F{missing.Function} for local role {localRole}.");
        }

        var matchedSecondarySequences = new HashSet<long>();
        var unmatched = new List<ObservedHeader>();
        foreach (var primary in primaries)
        {
            var secondary = frames.FirstOrDefault(candidate =>
                candidate.Sequence > primary.Sequence &&
                candidate.Direction != primary.Direction &&
                !candidate.Header.ReplyExpected &&
                candidate.Header.SessionId == primary.Header.SessionId &&
                candidate.Header.SystemBytes == primary.Header.SystemBytes &&
                candidate.Header.Stream == primary.Header.Stream &&
                candidate.Header.Function == primary.Header.Function + 1);
            if (secondary is null) unmatched.Add(primary);
            else matchedSecondarySequences.Add(secondary.Sequence);
        }
        if (unmatched.Count != 1 || unmatched[0].Header.Stream != 1 || unmatched[0].Header.Function != 3)
            throw new InvalidOperationException($"Expected one intentional pre-communication S1F3 timeout; unmatched={unmatched.Count}.");

        var normalSecondaries = frames.Where(static value => !value.Header.ReplyExpected).ToArray();
        var orphans = normalSecondaries.Where(value => !matchedSecondarySequences.Contains(value.Sequence)).ToArray();
        if (orphans.Length != 0)
            throw new InvalidOperationException($"Observed {orphans.Length} uncorrelated normal Secondary frame(s).");

        var dialogues = expected.OrderBy(static value => value.Stream).ThenBy(static value => value.Function)
            .Select(static value => $"S{value.Stream}F{value.Function}").ToArray();
        var primaryDirections = directionalRequirements
            .OrderBy(static value => value.Stream)
            .ThenBy(static value => value.Function)
            .ThenBy(static value => value.Initiator)
            .Select(static value =>
                $"S{value.Stream}F{value.Function}:{value.Initiator}->{(value.Initiator == SecsRole.Host ? SecsRole.Equipment : SecsRole.Host)}")
            .ToArray();
        return new DemoWireSummary(
            sessionId,
            frames.Length,
            primaries.Length,
            matchedSecondarySequences.Count,
            1,
            _source.DroppedWireObservationCount,
            dialogues,
            primaryDirections);
    }

    private static IEnumerable<(byte Stream, byte Function, SecsRole Initiator)> BuildDirectionalRequirements(
        IEnumerable<(byte Stream, byte Function)> dialogues)
    {
        foreach (var dialogue in dialogues)
        {
            // EN: Alarm/event sends originate at Equipment; the other frozen Demo requests originate at Host.
            // KO: Alarm/event 송신은 Equipment가 시작하고 나머지 동결 Demo 요청은 Host가 시작한다.
            var initiator = dialogue is (5, 1) or (6, 11) ? SecsRole.Equipment : SecsRole.Host;
            yield return (dialogue.Stream, dialogue.Function, initiator);

            // EN: These symmetric operations are also exercised from Equipment to prove both public directions.
            // KO: 이 대칭 작업은 양쪽 공개 방향을 증명하기 위해 Equipment에서도 실행한다.
            if (dialogue is (1, 1) or (2, 17))
                yield return (dialogue.Stream, dialogue.Function, SecsRole.Equipment);
        }
    }

    private static HsmsWireDirection GetLocalDirection(SecsRole localRole, SecsRole initiator) =>
        localRole == initiator ? HsmsWireDirection.Outbound : HsmsWireDirection.Inbound;

    private sealed record ObservedHeader(long Sequence, HsmsWireDirection Direction, HsmsHeader Header);
}

internal sealed record DemoWireSummary(
    ushort SessionId,
    int DataFrameCount,
    int PrimaryTransactionCount,
    int CorrelatedTransactionCount,
    int ExpectedTimeoutCount,
    long DroppedObservationCount,
    IReadOnlyList<string> IncludedDialogues,
    IReadOnlyList<string> RequiredPrimaryDirections);

internal sealed record DemoProcessEvidence(
    string Status,
    string Role,
    string Profile,
    ushort SessionId,
    IReadOnlyList<string> TypedChecks,
    DemoWireSummary Wire);

internal static class DemoEvidenceWriter
{
    public static async Task WriteAsync(string? path, DemoProcessEvidence evidence, CancellationToken cancellationToken)
    {
        if (path is null) return;
        var directory = Path.GetDirectoryName(path) ?? throw new ArgumentException("Evidence path has no parent directory.", nameof(path));
        Directory.CreateDirectory(directory);
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 16 * 1024, true);
        await JsonSerializer.SerializeAsync(stream, evidence, new JsonSerializerOptions { WriteIndented = true }, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
