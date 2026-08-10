using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Hsms;

namespace Dreamine.Gem.Transport;

/// <summary>\if KO <see cref="HsmsSession"/>을 GEM 메시지 전송 경계에 연결합니다. \endif \if EN Adapts <see cref="HsmsSession"/> to the GEM message transport boundary. \endif</summary>
public sealed class HsmsGemTransport : IGemMessageTransport
{
    private readonly HsmsSession _session;
    /// <summary>\if KO HSMS 세션과 SECS Session ID로 어댑터를 만듭니다. 세션 소유권은 호출자에게 있습니다. \endif \if EN Creates an adapter from an HSMS session and SECS session ID; the caller retains session ownership. \endif</summary>
    public HsmsGemTransport(HsmsSession session, SecsSessionId sessionId) { _session = session ?? throw new ArgumentNullException(nameof(session)); SessionId = sessionId; }
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
