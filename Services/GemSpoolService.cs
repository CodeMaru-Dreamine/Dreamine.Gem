using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Abstractions.States;
using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.Gem.Services;

/// <summary>\if KO 제한 용량 FIFO 메모리 스풀입니다. 영속 저장소나 완전한 wire 시나리오를 주장하지 않습니다. \endif \if EN Provides a bounded FIFO memory spool without claiming persistent storage or complete wire scenarios. \endif</summary>
public sealed class GemSpoolService : IGemSpoolService
{
    private readonly object _gate = new();
    private readonly Queue<SecsMessage> _messages = new();
    private readonly int _capacity;
    private GemSpoolState _state;
    /// <summary>\if KO 양의 용량으로 스풀을 만듭니다. \endif \if EN Creates a spool with positive capacity. \endif</summary>
    public GemSpoolService(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity)); _capacity = capacity;
    }
    /// <inheritdoc />
    public GemSpoolState State { get { lock (_gate) return _state; } }
    /// <inheritdoc />
    public int Count { get { lock (_gate) return _messages.Count; } }
    /// <inheritdoc />
    public void Start() { lock (_gate) { if (_state == GemSpoolState.Transmitting) throw new InvalidOperationException("Cannot start spooling while transmitting."); _state = GemSpoolState.Spooling; } }
    /// <inheritdoc />
    public bool Enqueue(SecsMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        lock (_gate)
        {
            if (_state != GemSpoolState.Spooling) return false;
            var overwritten = _messages.Count == _capacity;
            if (overwritten) _messages.Dequeue();
            _messages.Enqueue(message); return overwritten;
        }
    }
    /// <inheritdoc />
    public async Task DrainAsync(Func<SecsMessage, CancellationToken, Task> sender, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sender);
        lock (_gate) { if (_state != GemSpoolState.Spooling) throw new InvalidOperationException("Spooling is not active."); _state = GemSpoolState.Transmitting; }
        try
        {
            while (true)
            {
                SecsMessage message;
                lock (_gate) { if (_messages.Count == 0) break; message = _messages.Peek(); }
                await sender(message, cancellationToken).ConfigureAwait(false);
                lock (_gate) _messages.Dequeue();
            }
        }
        finally { lock (_gate) _state = _messages.Count == 0 ? GemSpoolState.Disabled : GemSpoolState.Spooling; }
    }
    /// <inheritdoc />
    public void Purge() { lock (_gate) { _messages.Clear(); _state = GemSpoolState.Disabled; } }
}
