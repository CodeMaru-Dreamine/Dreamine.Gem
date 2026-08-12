using System.Collections.ObjectModel;
using System.Threading.Channels;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Abstractions.States;
using Dreamine.Gem.Profiles;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.Gem.Protocol.E30;

/// <summary>\if KO provider-neutral session의 exact dispatcher에 E30-0611 파생 부분 프로필 v1 장비 대화를 연결합니다. 표준 적합성 주장이 아닙니다. \endif \if EN Connects E30-0611 derived subset profile v1 equipment dialogues to a provider-neutral session's exact dispatcher; this is not a standards-conformance claim. \endif</summary>
public sealed class E30EquipmentRouter : IAsyncDisposable
{
    private readonly ISecsMessageSession _session;
    private readonly GemEquipmentContext _context;
    private readonly E30IdentifierPolicy _identifiers;
    private readonly E30EquipmentRouterOptions _options;
    private readonly IDisposable[] _registrations;
    private readonly Channel<CommandWork> _commands;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Task _commandWorker;
    private int _disposed;

    /// <summary>\if KO isolated profile context를 nonzero Session ID의 장비 session에 연결합니다. session 소유권은 호출자에게 있습니다. \endif \if EN Connects an isolated profile context to an equipment session with a nonzero Session ID; session ownership remains with the caller. \endif</summary>
    public E30EquipmentRouter(ISecsMessageSession session, GemEquipmentContext context, E30EquipmentRouterOptions? options = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        if (_session.ConnectionIdentity.SessionId.Value == 0) throw new ArgumentException("The E30 derived subset requires a nonzero Session ID.", nameof(session));
        _options = options ?? new E30EquipmentRouterOptions();
        _options.Validate();
        _identifiers = new E30IdentifierPolicy(_context.Profile.IdentifierFormats);
        foreach (var pair in _options.CommandCompletionEvents)
        {
            if (!_context.Profile.RemoteCommands.Any(value => string.Equals(value.Definition.Name, pair.Key, StringComparison.Ordinal)))
                throw new ArgumentException($"Completion mapping references unknown command '{pair.Key}'.", nameof(options));
            if (!_context.Profile.CollectionEvents.Any(value => value.Id == pair.Value))
                throw new ArgumentException($"Completion mapping references unknown CEID {pair.Value}.", nameof(options));
        }

        _commands = Channel.CreateBounded<CommandWork>(new BoundedChannelOptions(_options.CommandQueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _registrations =
        [
            Register(E30Dialogues.S1F13, HandleS1F13Async),
            Register(E30Dialogues.S1F1, HandleS1F1Async),
            Register(E30Dialogues.S1F3, HandleS1F3Async),
            Register(E30Dialogues.S1F11, HandleS1F11Async),
            Register(E30Dialogues.S1F15, HandleS1F15Async),
            Register(E30Dialogues.S1F17, HandleS1F17Async),
            Register(E30Dialogues.S2F13, HandleS2F13Async),
            Register(E30Dialogues.S2F15, HandleS2F15Async),
            Register(E30Dialogues.S2F17, HandleS2F17Async),
            Register(E30Dialogues.S2F29, HandleS2F29Async),
            Register(E30Dialogues.S2F31, HandleS2F31Async),
            Register(E30Dialogues.S2F33, HandleS2F33Async),
            Register(E30Dialogues.S2F35, HandleS2F35Async),
            Register(E30Dialogues.S2F37, HandleS2F37Async),
            Register(E30Dialogues.S2F41, HandleS2F41Async),
            Register(E30Dialogues.S5F3, HandleS5F3Async),
            Register(E30Dialogues.S5F5, HandleS5F5Async),
            Register(E30Dialogues.S6F15, HandleS6F15Async),
            _session.PrimaryDispatcher.RegisterFallback(HandleFallbackAsync)
        ];
        _commandWorker = ProcessCommandsAsync();
    }

    /// <summary>\if KO 현재 context-local wire report 정의 스냅샷을 반환합니다. \endif \if EN Returns the current context-local wire report-definition snapshot. \endif</summary>
    public IReadOnlyList<E30ReportDefinition> GetReportDefinitions() => _context.Runtime.Events.GetReportDefinitions()
        .Select(static value => new E30ReportDefinition(value.Id, value.VariableIds)).ToArray();

    /// <summary>\if KO 현재 context-local event/report link 스냅샷을 반환합니다. \endif \if EN Returns the current context-local event/report-link snapshot. \endif</summary>
    public IReadOnlyList<E30EventLink> GetEventLinks() => _context.Runtime.Events.GetEventSnapshots()
        .Select(static value => new E30EventLink(value.Definition.Id, value.ReportIds)).ToArray();

    /// <summary>\if KO 장비 주도 S1F13/F14를 수행하고 communication state를 갱신합니다. \endif \if EN Performs equipment-initiated S1F13/F14 and updates communication state. \endif</summary>
    public async Task<E30CallResult<E30PeerIdentity?>> EstablishCommunicationsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_context.Runtime.Communication.State == GemCommunicationState.Disabled) _context.Runtime.Communication.Enable(equipmentInitiated: true);
        _context.Runtime.Communication.RequestSent();
        try
        {
            var response = await _session.RequestAsync(E30Dialogues.S1F13,
                E30WireCodec.Identity(_context.Profile.Identity.ModelNumber, _context.Profile.Identity.SoftwareRevision), cancellationToken).ConfigureAwait(false);
            if (response.Function.Value == 0)
            {
                RetryCommunicationIfPending();
                return E30CallResult<E30PeerIdentity?>.Ended(E30CallOutcome.FunctionZero);
            }
            var parsed = E30WireCodec.ReadCommunicationAcknowledgement(response.Item);
            if (parsed.Acknowledgement == 0)
                AcceptCommunicationIfPending();
            else RetryCommunicationIfPending();
            var identity = parsed.Identity is null ? null : new E30PeerIdentity(parsed.Identity.Value.ModelNumber, parsed.Identity.Value.SoftwareRevision);
            return E30CallResult<E30PeerIdentity?>.Complete(identity, parsed.Acknowledgement);
        }
        catch (OperationCanceledException)
        {
            RetryCommunicationIfPending();
            return E30CallResult<E30PeerIdentity?>.Ended(E30CallOutcome.Canceled);
        }
        catch (TimeoutException exception)
        {
            RetryCommunicationIfPending();
            return E30CallResult<E30PeerIdentity?>.Ended(E30CallOutcome.TimedOut, exception.GetType().Name);
        }
        catch (E30WireFormatException exception)
        {
            RetryCommunicationIfPending();
            return E30CallResult<E30PeerIdentity?>.Ended(E30CallOutcome.Malformed, exception.Message);
        }
    }

    /// <summary>\if KO 장비 주도 S1F1/F2로 host 존재를 확인하고 선택적 식별 정보를 읽습니다. \endif \if EN Performs equipment-initiated S1F1/F2 to verify host presence and read optional identity data. \endif</summary>
    public async Task<E30CallResult<E30PeerIdentity?>> AreYouThereAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureCommunicating();
        var state = _context.Runtime.Control.State;
        if (state == GemControlState.HostOffline)
            throw new InvalidOperationException("Equipment-initiated online identification cannot start from host-offline.");
        if (state == GemControlState.EquipmentOffline) _context.Runtime.Control.AttemptOnline();
        try
        {
            var result = await RequestAsync<E30PeerIdentity?>(
                E30Dialogues.S1F1, null,
                static item =>
                {
                    var value = E30WireCodec.ReadIdentity(item);
                    return (value is null ? null : new E30PeerIdentity(value.Value.ModelNumber, value.Value.SoftwareRevision), null);
                }, cancellationToken).ConfigureAwait(false);
            CompleteOnlineAttempt(result.Outcome == E30CallOutcome.Completed);
            return result;
        }
        catch
        {
            CompleteOnlineAttempt(succeeded: false);
            throw;
        }
    }

    /// <summary>\if KO 장비 주도 S2F17/F18로 host UTC 시각을 읽습니다. \endif \if EN Reads host UTC time with equipment-initiated S2F17/F18. \endif</summary>
    public Task<E30CallResult<DateTimeOffset>> ReadHostTimeAsync(CancellationToken cancellationToken = default)
    {
        EnsureOnlineWireOperation();
        return RequestAsync<DateTimeOffset>(E30Dialogues.S2F17, null,
            static item => (E30WireCodec.ReadTime(item), null), cancellationToken);
    }

    /// <summary>\if KO enabled alarm 상태 변경을 S5F1/F2로 먼저 보고한 뒤 선택적 S6F11/F12 event를 보냅니다. \endif \if EN Reports an enabled alarm change with S5F1/F2 before sending an optional S6F11/F12 event. \endif</summary>
    public async Task<E30CallResult<byte>> PublishAlarmChangeAsync(ulong alarmId, bool isSet, ulong? collectionEventId = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var before = _context.Runtime.Alarms.GetSnapshots().SingleOrDefault(value => value.Definition.Id == alarmId)
            ?? throw new KeyNotFoundException($"Alarm {alarmId} is not defined.");
        var changed = _context.Runtime.Alarms.ChangeAlarm(alarmId, isSet);
        if (changed == GemAlarmChangeStatus.NoChange)
            return E30CallResult<byte>.Ended(E30CallOutcome.NotSent, "The alarm state did not change.");
        if (!CanSendOnlineTraffic())
            return E30CallResult<byte>.Ended(E30CallOutcome.NotSent, "GEM communication is not online.");
        E30CallResult<byte>? lastWireResult = null;
        if (before.Enabled)
        {
            var code = isSet ? (byte)(before.Definition.Code | 0x80) : (byte)(before.Definition.Code & 0x7F);
            var alarmBody = E30WireCodec.Alarm(new E30AlarmData(code, alarmId, before.Definition.Text), _identifiers);
            var alarmResult = await RequestAcknowledgementAsync(E30Dialogues.S5F1, alarmBody, cancellationToken).ConfigureAwait(false);
            if (!alarmResult.IsAcknowledged) return alarmResult;
            lastWireResult = alarmResult;
        }
        if (collectionEventId.HasValue)
        {
            var eventResult = await PublishEventAsync(collectionEventId.Value, cancellationToken).ConfigureAwait(false);
            if (!eventResult.IsAcknowledged) return eventResult;
            lastWireResult = eventResult;
        }
        return lastWireResult ?? E30CallResult<byte>.Ended(E30CallOutcome.NotSent, "Alarm reporting is disabled.");
    }

    /// <summary>\if KO enabled CEID의 single-block S6F11/F12를 전송합니다. \endif \if EN Sends a single-block S6F11/F12 for an enabled CEID. \endif</summary>
    public async Task<E30CallResult<byte>> PublishEventAsync(ulong collectionEventId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureOnlineWireOperation();
        var report = await CollectEventAsync(collectionEventId, requireEnabled: true, cancellationToken).ConfigureAwait(false);
        if (report is null) throw new InvalidOperationException($"Collection event {collectionEventId} is unknown or disabled.");
        var body = E30WireCodec.EventReport(report, _identifiers);
        EnsureSingleBlock(body);
        return await RequestAcknowledgementAsync(E30Dialogues.S6F11, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>\if KO exact registrations와 bounded command worker를 종료합니다. session은 폐기하지 않습니다. \endif \if EN Stops exact registrations and the bounded command worker without disposing the session. \endif</summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        foreach (var registration in _registrations) registration.Dispose();
        _commands.Writer.TryComplete();
        _lifetime.Cancel();
        try { await _commandWorker.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        finally { _lifetime.Dispose(); }
    }

    private IDisposable Register(SecsDialogueDefinition dialogue, Func<ISecsPrimaryContext, CancellationToken, ValueTask> handler) =>
        _session.PrimaryDispatcher.Register(dialogue, (context, token) => RouteAsync(context, handler, token));

    private async ValueTask RouteAsync(ISecsPrimaryContext context, Func<ISecsPrimaryContext, CancellationToken, ValueTask> handler, CancellationToken cancellationToken)
    {
        var primary = context.Primary;
        var isEstablishCommunications = primary.Stream.Value == 1 && primary.Function.Value == 13;
        var isRequestOnline = primary.Stream.Value == 1 && primary.Function.Value == 17;
        var permitsOptionalReply = primary.Stream.Value == 5 && primary.Function.Value == 3;
        if (_context.Runtime.Communication.State != GemCommunicationState.EnabledCommunicating && !isEstablishCommunications) return;
        if (_context.Runtime.Control.State is GemControlState.EquipmentOffline or GemControlState.HostOffline or GemControlState.AttemptOnline)
        {
            if (!isEstablishCommunications && !isRequestOnline)
            {
                await _session.SendAsync(E30WireCodec.FunctionZero(primary), cancellationToken).ConfigureAwait(false);
                return;
            }
        }
        if (!permitsOptionalReply && !context.CanReply)
        {
            await _session.SendAsync(E30WireCodec.StreamNine(primary, 7), cancellationToken).ConfigureAwait(false);
            return;
        }
        if (primary.Item is { } item && item.BodyLength > _options.MaximumSingleBlockBodyBytes)
        {
            await _session.SendAsync(E30WireCodec.StreamNine(primary, 11), cancellationToken).ConfigureAwait(false);
            return;
        }
        await InvokeHandlerAsync(context, handler, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask InvokeHandlerAsync(ISecsPrimaryContext context, Func<ISecsPrimaryContext, CancellationToken, ValueTask> handler, CancellationToken cancellationToken)
    {
        try { await handler(context, cancellationToken).ConfigureAwait(false); }
        catch (E30WireFormatException) { await _session.SendAsync(E30WireCodec.StreamNine(context.Primary, 7), cancellationToken).ConfigureAwait(false); }
    }

    private async ValueTask HandleS1F13Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        _ = E30WireCodec.ReadIdentity(context.Primary.Item);
        if (_context.Runtime.Communication.State == GemCommunicationState.Disabled) _context.Runtime.Communication.Enable(equipmentInitiated: false);
        await context.ReplyAsync(E30WireCodec.CommunicationAcknowledgement(0,
            _context.Profile.Identity.ModelNumber, _context.Profile.Identity.SoftwareRevision), cancellationToken).ConfigureAwait(false);
        // E30 completes this transaction only after the accepting S1F14 has been transmitted.
        AcceptCommunicationIfPending();
    }

    private async ValueTask HandleS1F1Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        RequireHeaderOnly(context.Primary);
        await context.ReplyAsync(E30WireCodec.Identity(_context.Profile.Identity.ModelNumber, _context.Profile.Identity.SoftwareRevision), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleS1F3Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        var ids = E30WireCodec.ReadIdentifierList(context.Primary.Item, _identifiers.StatusVariable);
        if (ids.Count == 0) ids = _context.Profile.Variables.Where(value => value.Definition.Kind == GemVariableKind.Status).Select(value => value.Definition.Id).ToArray();
        var values = new List<SecsItem>(ids.Count);
        foreach (var id in ids)
        {
            if (!_context.Runtime.Variables.TryGetDefinition(id, out var definition) || definition!.Kind != GemVariableKind.Status) values.Add(new SecsListItem());
            else values.Add(await _context.Runtime.Variables.ReadAsync(id, cancellationToken).ConfigureAwait(false));
        }
        await context.ReplyAsync(E30WireCodec.Values(values), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleS1F11Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        var ids = E30WireCodec.ReadIdentifierList(context.Primary.Item, _identifiers.StatusVariable);
        if (ids.Count == 0) ids = _context.Profile.Variables.Where(value => value.Definition.Kind == GemVariableKind.Status).Select(value => value.Definition.Id).ToArray();
        var names = ids.Select(id => _context.Runtime.Variables.TryGetDefinition(id, out var value) && value!.Kind == GemVariableKind.Status
            ? new E30StatusVariableName(id, value.Name, value.Units)
            : new E30StatusVariableName(id, string.Empty, string.Empty));
        await context.ReplyAsync(E30WireCodec.StatusVariableNames(names, _identifiers), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleS1F15Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        RequireHeaderOnly(context.Primary);
        _context.Runtime.Control.HostOffline();
        await context.ReplyAsync(E30WireCodec.Acknowledgement(0), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleS1F17Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        RequireHeaderOnly(context.Primary);
        byte acknowledgement;
        switch (_context.Runtime.Control.State)
        {
            case GemControlState.HostOffline:
                _context.Runtime.Control.AcceptOnline();
                acknowledgement = 0;
                break;
            case GemControlState.EquipmentOffline:
            case GemControlState.AttemptOnline:
                acknowledgement = 1;
                break;
            default:
                acknowledgement = 2;
                break;
        }
        await context.ReplyAsync(E30WireCodec.Acknowledgement(acknowledgement), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleS2F13Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        var ids = E30WireCodec.ReadIdentifierList(context.Primary.Item, _identifiers.EquipmentConstant);
        if (ids.Count == 0) ids = _context.Runtime.Constants.GetSnapshots().Select(static value => value.Definition.Id).ToArray();
        var values = ids.Select(id => _context.Runtime.Constants.TryGetValue(id, out var value) ? value! : new SecsListItem());
        await context.ReplyAsync(E30WireCodec.Values(values), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleS2F15Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        var staged = E30WireCodec.ReadEquipmentConstantUpdates(context.Primary.Item, _identifiers)
            .Select(static value => new GemEquipmentConstantUpdate(value.Key, value.Value)).ToArray();
        if (staged.Length == 0) throw new E30WireFormatException("S2F15 requires at least one ECID/ECV pair");
        var result = _context.Runtime.Constants.SetValues(staged, _context.Runtime.Control.State);
        var acknowledgement = result.Status switch
        {
            GemConstantBatchStatus.Updated => (byte)0,
            GemConstantBatchStatus.Unknown => (byte)1,
            GemConstantBatchStatus.PolicyDenied => (byte)2,
            GemConstantBatchStatus.ValidationFailed => (byte)3,
            GemConstantBatchStatus.Duplicate => (byte)4,
            _ => (byte)4
        };
        await context.ReplyAsync(E30WireCodec.Acknowledgement(acknowledgement), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleS2F17Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        RequireHeaderOnly(context.Primary);
        await context.ReplyAsync(E30WireCodec.Time(_context.Runtime.Clock.GetUtcNow()), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleS2F29Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        var ids = E30WireCodec.ReadIdentifierList(context.Primary.Item, _identifiers.EquipmentConstant);
        var snapshots = _context.Runtime.Constants.GetSnapshots();
        if (ids.Count == 0) ids = snapshots.Select(static value => value.Definition.Id).ToArray();
        var byId = snapshots.ToDictionary(static value => value.Definition.Id);
        var names = ids.Select(id => byId.TryGetValue(id, out var value)
            ? new E30EquipmentConstantName(id, value.Definition.Name,
                value.Definition.MinimumValue ?? new SecsAsciiItem(string.Empty),
                value.Definition.MaximumValue ?? new SecsAsciiItem(string.Empty),
                value.Definition.DefaultValue, value.Definition.Units)
            : new E30EquipmentConstantName(id, string.Empty, new SecsAsciiItem(string.Empty), new SecsAsciiItem(string.Empty), new SecsAsciiItem(string.Empty), string.Empty));
        await context.ReplyAsync(E30WireCodec.EquipmentConstantNames(names, _identifiers), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleS2F31Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        try
        {
            var value = E30WireCodec.ReadTime(context.Primary.Item);
            _context.Runtime.Clock.SetUtcNow(value);
            await context.ReplyAsync(E30WireCodec.Acknowledgement(0), cancellationToken).ConfigureAwait(false);
        }
        catch (E30WireFormatException)
        {
            await context.ReplyAsync(E30WireCodec.Acknowledgement(1), cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask HandleS2F33Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        var staged = E30WireCodec.ReadReportDefinitions(context.Primary.Item, _identifiers);
        if (staged.Reports.Any(value => value.VariableIds.Distinct().Count() != value.VariableIds.Count))
        {
            await context.ReplyAsync(E30WireCodec.Acknowledgement(2), cancellationToken).ConfigureAwait(false);
            return;
        }
        var additions = staged.Reports.Where(static value => value.VariableIds.Count > 0)
            .Select(static value => new GemReportDefinition(value.ReportId, value.VariableIds)).ToArray();
        var deletions = staged.Reports.Where(static value => value.VariableIds.Count == 0).Select(static value => value.ReportId).ToArray();
        var result = _context.Runtime.Events.ApplyReportChanges(additions, deletions, deleteAll: staged.Reports.Count == 0);
        var acknowledgement = result.Status switch
        {
            GemEventConfigurationStatus.Applied => (byte)0,
            GemEventConfigurationStatus.Duplicate => (byte)3,
            GemEventConfigurationStatus.UnknownVariable => (byte)4,
            _ => (byte)5
        };
        await context.ReplyAsync(E30WireCodec.Acknowledgement(acknowledgement), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleS2F35Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        var staged = E30WireCodec.ReadEventLinks(context.Primary.Item, _identifiers);
        if (staged.Links.Count == 0 || staged.Links.Any(static value => value.ReportIds.Count == 0))
        {
            await _session.SendAsync(E30WireCodec.FunctionZero(context.Primary), cancellationToken).ConfigureAwait(false);
            return;
        }
        var updates = staged.Links.Select(static value => new GemEventReportLinkUpdate(value.CollectionEventId, value.ReportIds)).ToArray();
        var result = _context.Runtime.Events.ApplyEventLinks(updates, rejectExisting: true, disableUpdated: true);
        var acknowledgement = result.Status switch
        {
            GemEventConfigurationStatus.Applied => (byte)0,
            GemEventConfigurationStatus.Duplicate or GemEventConfigurationStatus.ExistingLinks => (byte)3,
            GemEventConfigurationStatus.UnknownEvent => (byte)4,
            GemEventConfigurationStatus.UnknownReport => (byte)5,
            _ => (byte)2
        };
        await context.ReplyAsync(E30WireCodec.Acknowledgement(acknowledgement), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleS2F37Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        var staged = E30WireCodec.ReadEventEnablement(context.Primary.Item, _identifiers);
        var result = staged.CollectionEventIds.Count == 0
            ? _context.Runtime.Events.SetAllEventsEnabled(staged.Enabled)
            : _context.Runtime.Events.SetEventsEnabled(staged.CollectionEventIds.Select(id => new GemEventEnableUpdate(id, staged.Enabled)));
        await context.ReplyAsync(E30WireCodec.Acknowledgement(result.Status == GemEventConfigurationStatus.Applied ? (byte)0 : (byte)1), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleS2F41Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        var parsed = E30WireCodec.ReadHostCommand(context.Primary.Item);
        if (_context.Runtime.Control.State != GemControlState.OnlineRemote)
        {
            await context.ReplyAsync(E30WireCodec.HostCommandAcknowledgement(new(2)), cancellationToken).ConfigureAwait(false);
            return;
        }
        var profile = _context.Profile.RemoteCommands.SingleOrDefault(value => string.Equals(value.Definition.Name, parsed.Name, StringComparison.Ordinal));
        if (profile is null)
        {
            await context.ReplyAsync(E30WireCodec.HostCommandAcknowledgement(new(1)), cancellationToken).ConfigureAwait(false);
            return;
        }
        if (!_options.CommandCompletionEvents.TryGetValue(parsed.Name, out var completionEventId) ||
            !_context.Runtime.Events.GetEventSnapshots().Any(value => value.Definition.Id == completionEventId && value.Enabled))
        {
            await context.ReplyAsync(E30WireCodec.HostCommandAcknowledgement(new(2)), cancellationToken).ConfigureAwait(false);
            return;
        }
        var rejected = ValidateCommandParameters(profile.Definition, parsed.Parameters);
        if (rejected.Count > 0)
        {
            await context.ReplyAsync(E30WireCodec.HostCommandAcknowledgement(new(3, rejected)), cancellationToken).ConfigureAwait(false);
            return;
        }
        var parameters = new ReadOnlyDictionary<string, SecsItem>(parsed.Parameters.ToDictionary(static value => value.Name, static value => value.Value, StringComparer.Ordinal));
        if (!_commands.Writer.TryWrite(new(parsed.Name, parameters, completionEventId)))
        {
            await context.ReplyAsync(E30WireCodec.HostCommandAcknowledgement(new(2)), cancellationToken).ConfigureAwait(false);
            return;
        }
        await context.ReplyAsync(E30WireCodec.HostCommandAcknowledgement(new(4)), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleS5F3Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        var parsed = E30WireCodec.ReadAlarmEnablement(context.Primary.Item, _identifiers);
        if (parsed.AlarmId.HasValue)
        {
            if (_context.Runtime.Alarms.ChangeEnabled(parsed.AlarmId.Value, parsed.Enabled) == GemAlarmChangeStatus.Unknown)
            {
                if (context.Primary.ReplyExpected)
                    await _session.SendAsync(E30WireCodec.FunctionZero(context.Primary), cancellationToken).ConfigureAwait(false);
                return;
            }
        }
        else
        {
            var snapshots = _context.Runtime.Alarms.GetSnapshots();
            foreach (var snapshot in snapshots) _context.Runtime.Alarms.ChangeEnabled(snapshot.Definition.Id, parsed.Enabled);
        }
        if (context.CanReply)
            await context.ReplyAsync(E30WireCodec.Acknowledgement(0), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleS5F5Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        var ids = E30WireCodec.ReadIdentifierVector(context.Primary.Item, _identifiers.Alarm);
        var snapshots = _context.Runtime.Alarms.GetSnapshots();
        if (ids.Count == 0) ids = snapshots.Select(static value => value.Definition.Id).ToArray();
        var byId = snapshots.ToDictionary(static value => value.Definition.Id);
        var alarms = ids.Select(id => byId.TryGetValue(id, out var value)
            ? new E30AlarmData(value.IsSet ? (byte)(value.Definition.Code | 0x80) : (byte)(value.Definition.Code & 0x7F), id, value.Definition.Text)
            : new E30AlarmData(0, id, string.Empty));
        await context.ReplyAsync(E30WireCodec.Alarms(alarms, _identifiers), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleS6F15Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        var eventId = E30WireCodec.ReadIdentifier(context.Primary.Item, _identifiers.CollectionEvent);
        var report = await CollectEventAsync(eventId, requireEnabled: false, cancellationToken).ConfigureAwait(false);
        await context.ReplyAsync(report is null ? new SecsListItem() : E30WireCodec.EventReport(report, _identifiers), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleFallbackAsync(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        var primary = context.Primary;
        if (primary.Stream.Value == 9 || _context.Runtime.Communication.State != GemCommunicationState.EnabledCommunicating) return;
        if (_context.Runtime.Control.State is GemControlState.EquipmentOffline or GemControlState.HostOffline or GemControlState.AttemptOnline)
        {
            await _session.SendAsync(E30WireCodec.FunctionZero(primary), cancellationToken).ConfigureAwait(false);
            return;
        }
        if (primary.Item is { } item && item.BodyLength > _options.MaximumSingleBlockBodyBytes)
        {
            await _session.SendAsync(E30WireCodec.StreamNine(primary, 11), cancellationToken).ConfigureAwait(false);
            return;
        }
        var knownStream = primary.Stream.Value is 1 or 2 or 5 or 6;
        await _session.SendAsync(E30WireCodec.StreamNine(primary, knownStream ? (byte)5 : (byte)3), cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessCommandsAsync()
    {
        try
        {
            await foreach (var work in _commands.Reader.ReadAllAsync(_lifetime.Token).ConfigureAwait(false))
            {
                _ = await _context.Runtime.Commands.ExecuteAsync(work.Name, work.Parameters, _options.CommandTimeout, _lifetime.Token).ConfigureAwait(false);
                try { await PublishEventAsync(work.CompletionEventId, _lifetime.Token).ConfigureAwait(false); }
                catch (Exception exception) when (exception is not OperationCanceledException) { }
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
    }

    private async Task<E30EventReport?> CollectEventAsync(ulong eventId, bool requireEnabled, CancellationToken cancellationToken)
    {
        var snapshot = _context.Runtime.Events.GetEventSnapshots().SingleOrDefault(value => value.Definition.Id == eventId);
        if (snapshot is null || (requireEnabled && !snapshot.Enabled)) return null;
        var definitions = _context.Runtime.Events.GetReportDefinitions().ToDictionary(static value => value.Id);
        var reports = new List<E30ReportValues>(snapshot.ReportIds.Count);
        foreach (var reportId in snapshot.ReportIds)
        {
            if (!definitions.TryGetValue(reportId, out var report)) continue;
            var values = new List<SecsItem>(report.VariableIds.Count);
            foreach (var variableId in report.VariableIds)
                values.Add(await _context.Runtime.Variables.ReadAsync(variableId, cancellationToken).ConfigureAwait(false));
            reports.Add(new(report.Id, values));
        }
        return new(0, eventId, reports);
    }

    private IReadOnlyList<E30RejectedCommandParameter> ValidateCommandParameters(GemRemoteCommandProfileDefinition definition, IReadOnlyList<E30CommandParameter> parameters)
    {
        var supplied = parameters.ToDictionary(static value => value.Name, StringComparer.Ordinal);
        var definitions = definition.Parameters.ToDictionary(static value => value.Name, StringComparer.Ordinal);
        var rejected = new List<E30RejectedCommandParameter>();
        foreach (var parameter in parameters)
        {
            if (!definitions.TryGetValue(parameter.Name, out var expected)) { rejected.Add(new(parameter.Name, 1)); continue; }
            if (parameter.Value.Format != expected.Format) { rejected.Add(new(parameter.Name, 3)); continue; }
            if (expected.Validator is not null)
            {
                try { if (!expected.Validator(parameter.Value)) rejected.Add(new(parameter.Name, 2)); }
                catch { rejected.Add(new(parameter.Name, 2)); }
            }
        }
        foreach (var missing in definition.Parameters.Where(value => value.Required && !supplied.ContainsKey(value.Name))) rejected.Add(new(missing.Name, 2));
        return rejected;
    }

    private async Task<E30CallResult<T>> RequestAsync<T>(
        SecsDialogueDefinition dialogue,
        SecsItem? item,
        Func<SecsItem?, (T Value, byte? Acknowledgement)> parser,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        try
        {
            var response = await _session.RequestAsync(dialogue, item, cancellationToken).ConfigureAwait(false);
            if (response.Function.Value == 0) return E30CallResult<T>.Ended(E30CallOutcome.FunctionZero);
            var parsed = parser(response.Item);
            return E30CallResult<T>.Complete(parsed.Value, parsed.Acknowledgement);
        }
        catch (OperationCanceledException) { return E30CallResult<T>.Ended(E30CallOutcome.Canceled); }
        catch (TimeoutException exception) { return E30CallResult<T>.Ended(E30CallOutcome.TimedOut, exception.GetType().Name); }
        catch (E30WireFormatException exception) { return E30CallResult<T>.Ended(E30CallOutcome.Malformed, exception.Message); }
    }

    private async Task<E30CallResult<byte>> RequestAcknowledgementAsync(SecsDialogueDefinition dialogue, SecsItem body, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _session.RequestAsync(dialogue, body, cancellationToken).ConfigureAwait(false);
            if (response.Function.Value == 0) return E30CallResult<byte>.Ended(E30CallOutcome.FunctionZero);
            var acknowledgement = E30WireCodec.ReadAcknowledgement(response.Item);
            return E30CallResult<byte>.CompleteWithAck(acknowledgement, acknowledgement);
        }
        catch (OperationCanceledException) { return E30CallResult<byte>.Ended(E30CallOutcome.Canceled); }
        catch (TimeoutException exception) { return E30CallResult<byte>.Ended(E30CallOutcome.TimedOut, exception.GetType().Name); }
        catch (E30WireFormatException exception) { return E30CallResult<byte>.Ended(E30CallOutcome.Malformed, exception.Message); }
    }

    private void EnsureSingleBlock(SecsItem item)
    {
        if (item.BodyLength > _options.MaximumSingleBlockBodyBytes)
            throw new NotSupportedException("The frozen v1 profile excludes multi-block S2F39/40 and S6F5/6; this body exceeds the configured single-block boundary.");
    }

    private static void RequireHeaderOnly(SecsMessage message)
    {
        if (message.Item is not null) throw new E30WireFormatException($"S{message.Stream.Value}F{message.Function.Value} must be header-only");
    }

    private void RetryCommunicationIfPending()
    {
        if (_context.Runtime.Communication.State != GemCommunicationState.EnabledNotCommunicating) return;
        try { _context.Runtime.Communication.Retry(); }
        catch (InvalidOperationException) when (_context.Runtime.Communication.State == GemCommunicationState.EnabledCommunicating) { }
    }

    private void AcceptCommunicationIfPending()
    {
        if (_context.Runtime.Communication.State != GemCommunicationState.EnabledNotCommunicating) return;
        try { _context.Runtime.Communication.Accept(); }
        catch (InvalidOperationException) when (_context.Runtime.Communication.State == GemCommunicationState.EnabledCommunicating) { }
    }

    private void EnsureCommunicating()
    {
        if (_context.Runtime.Communication.State != GemCommunicationState.EnabledCommunicating)
            throw new InvalidOperationException("Only S1F13/F14 traffic is permitted before GEM communication is established.");
    }

    private bool CanSendOnlineTraffic() =>
        _context.Runtime.Communication.State == GemCommunicationState.EnabledCommunicating &&
        _context.Runtime.Control.State is GemControlState.OnlineLocal or GemControlState.OnlineRemote;

    private void EnsureOnlineWireOperation()
    {
        ThrowIfDisposed();
        if (!CanSendOnlineTraffic())
            throw new InvalidOperationException("This operation requires communicating and online control states.");
    }

    private void CompleteOnlineAttempt(bool succeeded)
    {
        if (_context.Runtime.Control.State != GemControlState.AttemptOnline) return;
        if (succeeded) _context.Runtime.Control.AcceptOnline();
        else _context.Runtime.Control.RejectOnline();
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

    private sealed record CommandWork(string Name, IReadOnlyDictionary<string, SecsItem> Parameters, ulong CompletionEventId);
}
