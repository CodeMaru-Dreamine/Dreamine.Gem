using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Hsms;

namespace Dreamine.Gem.Transport;

/// <summary>\if KO provider-neutral <see cref="ISecsMessageSession"/>을 GEM 메시지 전송 경계에 연결하며 기존 <see cref="HsmsSession"/> 생성자와 호환됩니다. \endif \if EN Adapts a provider-neutral <see cref="ISecsMessageSession"/> to the GEM message transport boundary while retaining the legacy <see cref="HsmsSession"/> constructor. \endif</summary>
public sealed class HsmsGemTransport : IGemMessageTransport
{
    private readonly ISecsMessageSession _session;
    /// <summary>\if KO HSMS 세션과 SECS Session ID로 어댑터를 만듭니다. 세션 소유권은 호출자에게 있습니다. \endif \if EN Creates an adapter from an HSMS session and SECS session ID; the caller retains session ownership. \endif</summary>
    public HsmsGemTransport(HsmsSession session, SecsSessionId sessionId) { _session = session ?? throw new ArgumentNullException(nameof(session)); SessionId = sessionId; }
    /// <summary>\if KO provider-neutral 메시지 세션으로 어댑터를 만들고 설정된 Session ID를 사용합니다. 세션 소유권은 호출자에게 있습니다. \endif \if EN Creates an adapter from a provider-neutral message session, uses its configured session ID, and leaves session ownership with the caller. \endif</summary>
    public HsmsGemTransport(ISecsMessageSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        SessionId = session.ConnectionIdentity.SessionId;
    }
    /// <inheritdoc />
    public ISecsConnection Connection => _session;
    /// <inheritdoc />
    public SecsSessionId SessionId { get; }
    /// <inheritdoc />
    public event EventHandler<SecsMessage>? MessageReceived
    {
        add => _session.MessageReceived += value;
        remove => _session.MessageReceived -= value;
    }
    /// <inheritdoc />
    public Task SendAsync(SecsMessage message, CancellationToken cancellationToken = default) => _session.SendAsync(message, cancellationToken);
    /// <inheritdoc />
    public Task<SecsMessage> RequestAsync(SecsMessage message, CancellationToken cancellationToken = default) => _session.SendPrimaryAsync(message, cancellationToken);
    /// <inheritdoc />
    public SecsSystemBytes AllocateSystemBytes() => _session.AllocateSystemBytes();
}
