using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Protocol.E30;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.Gem.Host;

/// <summary>\if KO provider-neutral session 위에서 동결 E30-0611 파생 부분 프로필 v1 대화를 제공하는 typed host client입니다. 표준 적합성 주장이 아닙니다. \endif \if EN Provides typed host dialogues for the frozen E30-0611 derived subset profile v1 over a provider-neutral session; this is not a standards-conformance claim. \endif</summary>
public sealed class E30HostClient : IDisposable
{
    private readonly ISecsMessageSession _session;
    private readonly E30IdentifierPolicy _identifiers;
    private readonly IGemClockService? _clock;
    private readonly IDisposable[] _registrations;
    private int _disposed;

    /// <summary>\if KO nonzero Session ID의 host client를 만들고 장비 주도 필수 대화를 정확한 S/F로 등록합니다. \endif \if EN Creates a host client for a nonzero Session ID and registers equipment-initiated fundamental dialogues by exact S/F. \endif</summary>
    public E30HostClient(ISecsMessageSession session, E30IdentifierPolicy? identifiers = null, IGemClockService? clock = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        if (_session.ConnectionIdentity.SessionId.Value == 0) throw new ArgumentException("The E30 derived subset requires a nonzero Session ID.", nameof(session));
        _identifiers = identifiers ?? new E30IdentifierPolicy();
        _clock = clock;
        _registrations =
        [
            _session.PrimaryDispatcher.Register(E30Dialogues.S1F13, HandleEquipmentS1F13Async),
            _session.PrimaryDispatcher.Register(E30Dialogues.S1F1, HandleEquipmentS1F1Async),
            _session.PrimaryDispatcher.Register(E30Dialogues.S2F17, HandleEquipmentS2F17Async),
            _session.PrimaryDispatcher.Register(E30Dialogues.S5F1, HandleAlarmAsync),
            _session.PrimaryDispatcher.Register(E30Dialogues.S6F11, HandleEventReportAsync)
        ];
    }

    /// <summary>\if KO 장비가 보낸 S5F1을 구조 검증한 뒤 발생합니다. \endif \if EN Raised after structural validation of an equipment S5F1. \endif</summary>
    public event EventHandler<E30AlarmData>? AlarmReceived;

    /// <summary>\if KO 장비가 보낸 S6F11을 구조 검증한 뒤 발생합니다. \endif \if EN Raised after structural validation of an equipment S6F11. \endif</summary>
    public event EventHandler<E30EventReport>? EventReportReceived;

    /// <summary>\if KO host 주도 S1F13/F14를 수행합니다. \endif \if EN Performs host-initiated S1F13/F14. \endif</summary>
    public Task<E30CallResult<E30PeerIdentity?>> EstablishCommunicationsAsync(CancellationToken cancellationToken = default) => RequestAsync(
        E30Dialogues.S1F13,
        new SecsListItem(),
        static item =>
        {
            var value = E30WireCodec.ReadCommunicationAcknowledgement(item);
            return (value.Identity is null ? null : new E30PeerIdentity(value.Identity.Value.ModelNumber, value.Identity.Value.SoftwareRevision), (byte?)value.Acknowledgement);
        }, cancellationToken);

    /// <summary>\if KO S1F1/F2로 장비 식별 정보를 조회합니다. \endif \if EN Reads equipment identification with S1F1/F2. \endif</summary>
    public Task<E30CallResult<E30PeerIdentity?>> AreYouThereAsync(CancellationToken cancellationToken = default) => RequestAsync<E30PeerIdentity?>(
        E30Dialogues.S1F1, null,
        static item =>
        {
            var value = E30WireCodec.ReadIdentity(item);
            return (value is null ? null : new E30PeerIdentity(value.Value.ModelNumber, value.Value.SoftwareRevision), null);
        }, cancellationToken);

    /// <summary>\if KO S1F15/F16 OFF-LINE 전환을 요청하고 raw OFLACK를 반환합니다. \endif \if EN Requests OFF-LINE with S1F15/F16 and returns raw OFLACK. \endif</summary>
    public Task<E30CallResult<byte>> RequestOfflineAsync(CancellationToken cancellationToken = default) => AckAsync(E30Dialogues.S1F15, null, cancellationToken);

    /// <summary>\if KO S1F17/F18 ON-LINE 전환을 요청하고 raw ONLACK를 반환합니다. \endif \if EN Requests ON-LINE with S1F17/F18 and returns raw ONLACK. \endif</summary>
    public Task<E30CallResult<byte>> RequestOnlineAsync(CancellationToken cancellationToken = default) => AckAsync(E30Dialogues.S1F17, null, cancellationToken);

    /// <summary>\if KO S1F3/F4로 선택 상태 값을 요청 순서대로 읽습니다. 빈 ID 목록은 전체를 뜻합니다. \endif \if EN Reads selected status values in request order with S1F3/F4; an empty ID list means all. \endif</summary>
    public Task<E30CallResult<IReadOnlyList<SecsItem>>> ReadStatusAsync(IEnumerable<ulong> statusVariableIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(statusVariableIds);
        return RequestAsync<IReadOnlyList<SecsItem>>(E30Dialogues.S1F3, E30WireCodec.IdentifierList(statusVariableIds, _identifiers.StatusVariable),
            static item => (E30WireCodec.ReadValues(item), null), cancellationToken);
    }

    /// <summary>\if KO S1F11/F12로 상태 변수 이름과 단위를 읽습니다. \endif \if EN Reads status-variable names and units with S1F11/F12. \endif</summary>
    public Task<E30CallResult<IReadOnlyList<E30StatusVariableName>>> ReadStatusVariableNamesAsync(IEnumerable<ulong> statusVariableIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(statusVariableIds);
        return RequestAsync<IReadOnlyList<E30StatusVariableName>>(E30Dialogues.S1F11, E30WireCodec.IdentifierList(statusVariableIds, _identifiers.StatusVariable),
            item => (E30WireCodec.ReadStatusVariableNames(item, _identifiers), null), cancellationToken);
    }

    /// <summary>\if KO S2F13/F14로 장비 상수 값을 요청 순서대로 읽습니다. \endif \if EN Reads equipment-constant values in request order with S2F13/F14. \endif</summary>
    public Task<E30CallResult<IReadOnlyList<SecsItem>>> ReadEquipmentConstantsAsync(IEnumerable<ulong> equipmentConstantIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(equipmentConstantIds);
        return RequestAsync<IReadOnlyList<SecsItem>>(E30Dialogues.S2F13, E30WireCodec.IdentifierList(equipmentConstantIds, _identifiers.EquipmentConstant),
            static item => (E30WireCodec.ReadValues(item), null), cancellationToken);
    }

    /// <summary>\if KO S2F15/F16으로 장비 상수를 일괄 변경하고 raw EAC를 반환합니다. \endif \if EN Changes equipment constants as one batch with S2F15/F16 and returns raw EAC. \endif</summary>
    public Task<E30CallResult<byte>> SetEquipmentConstantsAsync(IEnumerable<KeyValuePair<ulong, SecsItem>> updates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates);
        return AckAsync(E30Dialogues.S2F15, E30WireCodec.EquipmentConstantUpdates(updates, _identifiers), cancellationToken);
    }

    /// <summary>\if KO S2F17/F18로 장비 UTC 시각을 읽습니다. \endif \if EN Reads equipment UTC time with S2F17/F18. \endif</summary>
    public Task<E30CallResult<DateTimeOffset>> ReadTimeAsync(CancellationToken cancellationToken = default) => RequestAsync<DateTimeOffset>(
        E30Dialogues.S2F17, null, static item => (E30WireCodec.ReadTime(item), null), cancellationToken);

    /// <summary>\if KO S2F29/F30으로 장비 상수 메타데이터를 읽습니다. \endif \if EN Reads equipment-constant metadata with S2F29/F30. \endif</summary>
    public Task<E30CallResult<IReadOnlyList<E30EquipmentConstantName>>> ReadEquipmentConstantNamesAsync(IEnumerable<ulong> equipmentConstantIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(equipmentConstantIds);
        return RequestAsync<IReadOnlyList<E30EquipmentConstantName>>(E30Dialogues.S2F29, E30WireCodec.IdentifierList(equipmentConstantIds, _identifiers.EquipmentConstant),
            item => (E30WireCodec.ReadEquipmentConstantNames(item, _identifiers), null), cancellationToken);
    }

    /// <summary>\if KO S2F31/F32로 UTC 시각을 설정하고 raw TIACK를 반환합니다. \endif \if EN Sets UTC time with S2F31/F32 and returns raw TIACK. \endif</summary>
    public Task<E30CallResult<byte>> SetTimeAsync(DateTimeOffset value, bool fourDigitYear = true, CancellationToken cancellationToken = default) =>
        AckAsync(E30Dialogues.S2F31, E30WireCodec.Time(value, fourDigitYear), cancellationToken);

    /// <summary>\if KO single-block S2F33/F34로 report를 정의하고 raw DRACK를 반환합니다. \endif \if EN Defines reports with single-block S2F33/F34 and returns raw DRACK. \endif</summary>
    public Task<E30CallResult<byte>> DefineReportsAsync(ulong dataId, IEnumerable<E30ReportDefinition> reports, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reports);
        return AckAsync(E30Dialogues.S2F33, E30WireCodec.ReportDefinitions(dataId, reports, _identifiers), cancellationToken);
    }

    /// <summary>\if KO single-block S2F35/F36으로 event/report를 연결하고 raw LRACK를 반환합니다. \endif \if EN Links events and reports with single-block S2F35/F36 and returns raw LRACK. \endif</summary>
    public Task<E30CallResult<byte>> LinkEventReportsAsync(ulong dataId, IEnumerable<E30EventLink> links, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(links);
        return AckAsync(E30Dialogues.S2F35, E30WireCodec.EventLinks(dataId, links, _identifiers), cancellationToken);
    }

    /// <summary>\if KO S2F37/F38로 event report를 enable/disable하고 raw ERACK를 반환합니다. \endif \if EN Enables or disables event reports with S2F37/F38 and returns raw ERACK. \endif</summary>
    public Task<E30CallResult<byte>> SetEventEnablementAsync(bool enabled, IEnumerable<ulong> collectionEventIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(collectionEventIds);
        return AckAsync(E30Dialogues.S2F37, E30WireCodec.EventEnablement(enabled, collectionEventIds, _identifiers), cancellationToken);
    }

    /// <summary>\if KO S2F41/F42 명령 승인 결과를 읽습니다. HCACK 0/4는 완료 증거가 아닙니다. \endif \if EN Reads S2F41/F42 command acceptance; HCACK 0/4 is not completion evidence. \endif</summary>
    public Task<E30CallResult<E30HostCommandAcknowledgement>> SendRemoteCommandAsync(string name, IEnumerable<E30CommandParameter> parameters, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name); ArgumentNullException.ThrowIfNull(parameters);
        return RequestAsync(E30Dialogues.S2F41, E30WireCodec.HostCommand(name, parameters), static item =>
        {
            var value = E30WireCodec.ReadHostCommandAcknowledgement(item);
            return (value, (byte?)value.Acknowledgement);
        }, cancellationToken);
    }

    /// <summary>\if KO S5F3/F4로 하나 또는 모든 alarm 보고를 enable/disable하고 raw ACKC5를 반환합니다. \endif \if EN Enables or disables one or all alarm reports with S5F3/F4 and returns raw ACKC5. \endif</summary>
    public Task<E30CallResult<byte>> SetAlarmEnablementAsync(bool enabled, ulong? alarmId = null, CancellationToken cancellationToken = default) =>
        AckAsync(E30Dialogues.S5F3, E30WireCodec.AlarmEnablement(enabled, alarmId, _identifiers), cancellationToken);

    /// <summary>\if KO S5F5/F6으로 선택 alarm 데이터를 읽습니다. 빈 ID vector는 전체를 뜻합니다. \endif \if EN Reads selected alarm data with S5F5/F6; an empty ID vector means all. \endif</summary>
    public Task<E30CallResult<IReadOnlyList<E30AlarmData>>> ReadAlarmsAsync(IEnumerable<ulong> alarmIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alarmIds);
        return RequestAsync<IReadOnlyList<E30AlarmData>>(E30Dialogues.S5F5, E30WireCodec.IdentifierVector(alarmIds, _identifiers.Alarm),
            item => (E30WireCodec.ReadAlarms(item, _identifiers), null), cancellationToken);
    }

    /// <summary>\if KO S6F15/F16으로 CEID의 현재 report를 읽습니다. \endif \if EN Reads the current report for a CEID with S6F15/F16. \endif</summary>
    public Task<E30CallResult<E30EventReport>> ReadEventReportAsync(ulong collectionEventId, CancellationToken cancellationToken = default) => RequestAsync<E30EventReport>(
        E30Dialogues.S6F15, E30WireCodec.Identifier(collectionEventId, _identifiers.CollectionEvent),
        item =>
        {
            if (item is SecsListItem { Count: 0 }) return (new E30EventReport(0, collectionEventId, []), null);
            return (E30WireCodec.ReadEventReport(item, _identifiers), null);
        }, cancellationToken);

    /// <summary>\if KO exact dispatcher 등록을 해제합니다. session 소유권은 호출자에게 남습니다. \endif \if EN Removes exact dispatcher registrations; session ownership remains with the caller. \endif</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        foreach (var registration in _registrations) registration.Dispose();
    }

    private Task<E30CallResult<byte>> AckAsync(SecsDialogueDefinition dialogue, SecsItem? item, CancellationToken cancellationToken) =>
        RequestAsync(dialogue, item, static body =>
        {
            var acknowledgement = E30WireCodec.ReadAcknowledgement(body);
            return (acknowledgement, (byte?)acknowledgement);
        }, cancellationToken);

    private async Task<E30CallResult<T>> RequestAsync<T>(
        SecsDialogueDefinition dialogue,
        SecsItem? item,
        Func<SecsItem?, (T Value, byte? Acknowledgement)> parser,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
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

    private async ValueTask HandleEquipmentS1F13Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        if (!context.CanReply) return;
        try
        {
            _ = E30WireCodec.ReadIdentity(context.Primary.Item);
            await context.ReplyAsync(E30WireCodec.CommunicationAcknowledgement(0, string.Empty, string.Empty, hostResponse: true), cancellationToken).ConfigureAwait(false);
        }
        catch (E30WireFormatException) { await TerminateTransactionAsync(context.Primary, cancellationToken).ConfigureAwait(false); }
    }

    private async ValueTask HandleEquipmentS1F1Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        if (!context.CanReply) return;
        if (context.Primary.Item is not null) { await TerminateTransactionAsync(context.Primary, cancellationToken).ConfigureAwait(false); return; }
        await context.ReplyAsync(new SecsListItem(), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleEquipmentS2F17Async(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        if (!context.CanReply) return;
        if (context.Primary.Item is not null) { await TerminateTransactionAsync(context.Primary, cancellationToken).ConfigureAwait(false); return; }
        var now = _clock?.GetUtcNow() ?? TimeProvider.System.GetUtcNow();
        await context.ReplyAsync(E30WireCodec.Time(now), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleAlarmAsync(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        try
        {
            var alarm = E30WireCodec.ReadAlarm(context.Primary.Item, _identifiers);
            InvokeSafely(AlarmReceived, alarm);
            if (context.CanReply)
                await context.ReplyAsync(E30WireCodec.Acknowledgement(0), cancellationToken).ConfigureAwait(false);
        }
        catch (E30WireFormatException) { await TerminateTransactionAsync(context.Primary, cancellationToken).ConfigureAwait(false); }
    }

    private async ValueTask HandleEventReportAsync(ISecsPrimaryContext context, CancellationToken cancellationToken)
    {
        if (!context.CanReply) return;
        try
        {
            var report = E30WireCodec.ReadEventReport(context.Primary.Item, _identifiers);
            InvokeSafely(EventReportReceived, report);
            await context.ReplyAsync(E30WireCodec.Acknowledgement(0), cancellationToken).ConfigureAwait(false);
        }
        catch (E30WireFormatException) { await TerminateTransactionAsync(context.Primary, cancellationToken).ConfigureAwait(false); }
    }

    private Task TerminateTransactionAsync(SecsMessage offending, CancellationToken cancellationToken) =>
        offending.ReplyExpected
            ? _session.SendAsync(E30WireCodec.FunctionZero(offending), cancellationToken)
            : Task.CompletedTask;

    private void InvokeSafely<T>(EventHandler<T>? handlers, T value)
    {
        if (handlers is null) return;
        foreach (EventHandler<T> handler in handlers.GetInvocationList())
            try { handler(this, value); }
            catch { }
    }
}
