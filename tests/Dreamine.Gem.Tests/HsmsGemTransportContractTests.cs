using System.Runtime.CompilerServices;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Gem.Transport;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Hsms;
using Xunit;

namespace Dreamine.Gem.Tests;

public sealed class HsmsGemTransportContractTests
{
    [Fact]
    public async Task ProviderNeutralAdapterDelegatesExpertBoundary()
    {
        var session = new FakeMessageSession(new SecsSessionId(23));
        var transport = new HsmsGemTransport(session);
        using var cancellation = new CancellationTokenSource();

        Assert.Same(session, transport.Connection);
        Assert.Equal(new SecsSessionId(23), transport.SessionId);
        Assert.Equal(new SecsSystemBytes(41), transport.AllocateSystemBytes());

        var sent = new SecsMessage(new(23), new(1), new(1), false, new(7));
        await transport.SendAsync(sent, cancellation.Token);
        Assert.Same(sent, session.LastSent);
        Assert.Equal(cancellation.Token, session.LastSendCancellationToken);

        var request = new SecsMessage(new(23), new(1), new(1), true, new(8));
        var response = new SecsMessage(new(23), new(1), new(2), false, new(8));
        session.Response = response;
        Assert.Same(response, await transport.RequestAsync(request, cancellation.Token));
        Assert.Same(request, session.LastRequest);
        Assert.Equal(cancellation.Token, session.LastRequestCancellationToken);
        Assert.Equal(0, session.SafeSendCount);
        Assert.Equal(0, session.SafeRequestCount);
    }

    [Fact]
    public void ProviderNeutralAdapterForwardsMessageSubscription()
    {
        var session = new FakeMessageSession(new SecsSessionId(23));
        var transport = new HsmsGemTransport(session);
        var received = new SecsMessage(new(23), new(1), new(1), true, new(9));
        var count = 0;
        EventHandler<SecsMessage> handler = (_, message) =>
        {
            Assert.Same(received, message);
            count++;
        };

        transport.MessageReceived += handler;
        session.Raise(received);
        transport.MessageReceived -= handler;
        session.Raise(received);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task LegacyConstructorSignatureAndExplicitSessionIdRemain()
    {
        Assert.NotNull(typeof(HsmsGemTransport).GetConstructor([typeof(HsmsSession), typeof(SecsSessionId)]));
        await using var session = new HsmsSession(new HsmsSessionOptions
        {
            Host = "127.0.0.1",
            Port = 5000,
            Mode = SecsConnectionMode.Passive,
            Role = SecsRole.Equipment,
            SessionId = new SecsSessionId(1)
        });

        var transport = new HsmsGemTransport(session, new SecsSessionId(2));

        Assert.Same(session, transport.Connection);
        Assert.Equal(new SecsSessionId(2), transport.SessionId);
    }

    [Fact]
    public void ProviderNeutralConstructorRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new HsmsGemTransport((ISecsMessageSession)null!));
    }

    private sealed class FakeMessageSession : ISecsMessageSession
    {
        private uint _systemBytes = 40;

        public FakeMessageSession(SecsSessionId sessionId)
        {
            ConnectionIdentity = new SecsConnectionIdentity(
                "fake",
                Guid.NewGuid(),
                0,
                sessionId,
                SecsRole.Equipment,
                SecsConnectionMode.Passive);
        }

        public string ProviderKey => "fake";
        public ConnectionState State => ConnectionState.Connected;
        public SecsConnectionIdentity ConnectionIdentity { get; }
        public HsmsConnectionState HsmsState => HsmsConnectionState.Selected;
        public ISecsPrimaryDispatcher PrimaryDispatcher => throw new NotSupportedException();
        public bool IsWireObservationEnabled => false;
        public long DroppedWireObservationCount => 0;
        public SecsMessage? LastSent { get; private set; }
        public SecsMessage? LastRequest { get; private set; }
        public SecsMessage? Response { get; set; }
        public CancellationToken LastSendCancellationToken { get; private set; }
        public CancellationToken LastRequestCancellationToken { get; private set; }
        public int SafeSendCount { get; private set; }
        public int SafeRequestCount { get; private set; }

        public event EventHandler<SecsMessage>? MessageReceived;
        public event EventHandler<SecsDiagnosticEvent>? DiagnosticReceived
        {
            add { }
            remove { }
        }
        public event EventHandler<SecsSessionStateChangedEventArgs>? StateChanged
        {
            add { }
            remove { }
        }

        public void Raise(SecsMessage message) => MessageReceived?.Invoke(this, message);
        public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SelectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeselectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LinktestAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SeparateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public SecsSystemBytes AllocateSystemBytes() => new(++_systemBytes);

        public Task SendAsync(SecsMessage message, CancellationToken cancellationToken = default)
        {
            LastSent = message;
            LastSendCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task<SecsMessage> SendPrimaryAsync(SecsMessage message, CancellationToken cancellationToken = default)
        {
            LastRequest = message;
            LastRequestCancellationToken = cancellationToken;
            return Task.FromResult(Response ?? throw new InvalidOperationException("No response configured."));
        }

        public Task SendAsync(
            SecsStream stream,
            SecsFunction function,
            SecsItem? item = null,
            CancellationToken cancellationToken = default)
        {
            SafeSendCount++;
            return Task.CompletedTask;
        }

        public Task<SecsMessage> RequestAsync(
            SecsDialogueDefinition dialogue,
            SecsItem? item = null,
            CancellationToken cancellationToken = default)
        {
            SafeRequestCount++;
            return Task.FromResult(Response ?? throw new InvalidOperationException("No response configured."));
        }

        public async IAsyncEnumerable<HsmsWireObservation> ReadWireObservationsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
