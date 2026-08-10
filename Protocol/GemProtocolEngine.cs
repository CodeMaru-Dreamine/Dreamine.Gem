using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Abstractions.States;
using Dreamine.Gem.StateMachines;
using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.Gem.Protocol;

/// <summary>\if KO E30-0611 근거의 S1 통신 수립·온라인 식별 시나리오를 처리합니다. 다른 GEM Stream/Function은 이 클래스가 지원한다고 주장하지 않습니다. \endif \if EN Handles E30-0611-based S1 communication-establishment and online-identification scenarios; it does not claim support for other GEM stream/functions. \endif</summary>
public sealed class GemProtocolEngine
{
    private readonly IGemMessageTransport _transport;
    private readonly GemCommunicationStateMachine _communication;
    private readonly GemEquipmentIdentity _identity;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _retryDelay;
    /// <summary>\if KO 주입된 전송, 상태, 식별 정보 및 시간 정책으로 엔진을 만듭니다. \endif \if EN Creates the engine from injected transport, state, identity, and time policy. \endif</summary>
    public GemProtocolEngine(IGemMessageTransport transport, GemCommunicationStateMachine communication, GemEquipmentIdentity identity, TimeProvider? timeProvider = null, TimeSpan? retryDelay = null)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _communication = communication ?? throw new ArgumentNullException(nameof(communication));
        _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        _timeProvider = timeProvider ?? TimeProvider.System; _retryDelay = retryDelay ?? TimeSpan.FromSeconds(5);
        if (_retryDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(retryDelay));
    }

    /// <summary>\if KO 장비 주도 S1F13을 한 번 수행합니다. 거부 시 재시도 대기 상태를 반환합니다. \endif \if EN Performs one equipment-initiated S1F13 attempt and enters retry-wait state on rejection. \endif</summary>
    public async Task<bool> EstablishCommunicationsAsync(CancellationToken cancellationToken = default)
    {
        if (_communication.State == GemCommunicationState.Disabled) _communication.Enable(equipmentInitiated: true);
        if (_communication.State == GemCommunicationState.EnabledCommunicating) return true;
        if (_communication.EstablishmentState == GemEstablishmentState.WaitDelay && _retryDelay > TimeSpan.Zero)
            await Task.Delay(_retryDelay, _timeProvider, cancellationToken).ConfigureAwait(false);
        _communication.RequestSent();
        var request = new SecsMessage(_transport.SessionId, new(1), new(13), true, _transport.AllocateSystemBytes(), IdentityList());
        try
        {
            var response = await _transport.RequestAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.Stream.Value != 1 || response.Function.Value != 14 || response.SystemBytes != request.SystemBytes) throw new InvalidOperationException("The communication response is not a correlated S1F14.");
            if (ReadCommAck(response.Item) == 0) { _communication.Accept(); return true; }
            _communication.Retry(); return false;
        }
        catch
        {
            if (_communication.State == GemCommunicationState.EnabledNotCommunicating) _communication.Retry();
            throw;
        }
    }

    /// <summary>\if KO 지원되는 host-initiated S1 primary를 처리하고 응답 여부를 반환합니다. \endif \if EN Handles a supported host-initiated S1 primary and returns whether it responded. \endif</summary>
    public async Task<bool> HandleAsync(SecsMessage primary, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(primary);
        if (!primary.Function.IsPrimary) return false;
        if (primary.Stream.Value != 1) return false;
        if (primary.Function.Value == 13)
        {
            if (_communication.State == GemCommunicationState.Disabled) return false;
            if (_communication.State == GemCommunicationState.EnabledNotCommunicating) _communication.Accept();
            await _transport.SendAsync(new(primary.SessionId, new(1), new(14), false, primary.SystemBytes, new SecsListItem(new SecsBinaryItem(0), IdentityList())), cancellationToken).ConfigureAwait(false);
            return true;
        }
        if (primary.Function.Value == 1)
        {
            if (_communication.State != GemCommunicationState.EnabledCommunicating) return false;
            await _transport.SendAsync(new(primary.SessionId, new(1), new(2), false, primary.SystemBytes, IdentityList()), cancellationToken).ConfigureAwait(false);
            return true;
        }
        return false;
    }

    private SecsListItem IdentityList() => new(new SecsAsciiItem(_identity.ModelNumber), new SecsAsciiItem(_identity.SoftwareRevision));
    private static byte ReadCommAck(SecsItem? item)
    {
        if (item is SecsListItem { Count: > 0 } list && list.Items[0] is SecsBinaryItem binary && binary.Values.Length == 1) return binary.Values.Span[0];
        throw new InvalidOperationException("S1F14 does not contain a one-byte COMMACK.");
    }
}
