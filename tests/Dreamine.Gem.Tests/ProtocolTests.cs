using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Abstractions.States;
using Dreamine.Gem.Protocol;
using Dreamine.Gem.StateMachines;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Xunit;

namespace Dreamine.Gem.Tests;

public sealed class ProtocolTests
{
    [Fact]
    public async Task EquipmentInitiatedS1F13AcceptsCorrelatedS1F14()
    {
        var transport = new FakeTransport
        {
            ResponseFactory = request => new(request.SessionId, new(1), new(14), false, request.SystemBytes, new SecsListItem(new SecsBinaryItem(0)))
        };
        var state = new GemCommunicationStateMachine();
        var engine = new GemProtocolEngine(transport, state, new("MODEL", "1.2"), retryDelay: TimeSpan.Zero);
        Assert.True(await engine.EstablishCommunicationsAsync());
        var request = Assert.Single(transport.Requests);
        Assert.Equal((byte)1, request.Stream.Value); Assert.Equal((byte)13, request.Function.Value); Assert.True(request.ReplyExpected);
        var identity = Assert.IsType<SecsListItem>(request.Item);
        Assert.Equal("MODEL", Assert.IsType<SecsAsciiItem>(identity.Items[0]).Value);
        Assert.Equal(GemCommunicationState.EnabledCommunicating, state.State);
    }

    [Fact]
    public async Task EquipmentInitiatedS1F13RejectionSchedulesRetry()
    {
        var transport = new FakeTransport { ResponseFactory = request => new(request.SessionId, new(1), new(14), false, request.SystemBytes, new SecsListItem(new SecsBinaryItem(1))) };
        var state = new GemCommunicationStateMachine(); var engine = new GemProtocolEngine(transport, state, new("MODEL", "1"), retryDelay: TimeSpan.Zero);
        Assert.False(await engine.EstablishCommunicationsAsync()); Assert.Equal(GemEstablishmentState.WaitDelay, state.EstablishmentState);
    }

    [Fact]
    public async Task InvalidS1F14CorrelationLeavesRetryState()
    {
        var transport = new FakeTransport { ResponseFactory = request => new(request.SessionId, new(1), new(14), false, new(request.SystemBytes.Value + 1), new SecsListItem(new SecsBinaryItem(0))) };
        var state = new GemCommunicationStateMachine(); var engine = new GemProtocolEngine(transport, state, new("MODEL", "1"), retryDelay: TimeSpan.Zero);
        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.EstablishCommunicationsAsync());
        Assert.Equal(GemEstablishmentState.WaitDelay, state.EstablishmentState);
    }

    [Fact]
    public async Task TransportTimeoutLeavesRetryState()
    {
        var transport = new FakeTransport { AsyncResponseFactory = _ => Task.FromException<SecsMessage>(new TimeoutException()) };
        var state = new GemCommunicationStateMachine(); var engine = new GemProtocolEngine(transport, state, new("MODEL", "1"), retryDelay: TimeSpan.Zero);
        await Assert.ThrowsAsync<TimeoutException>(() => engine.EstablishCommunicationsAsync());
        Assert.Equal(GemEstablishmentState.WaitDelay, state.EstablishmentState);
    }

    [Fact]
    public async Task HostInitiatedS1F13GetsS1F14WithIdentity()
    {
        var transport = new FakeTransport(); var state = new GemCommunicationStateMachine(); state.Enable(false);
        var engine = new GemProtocolEngine(transport, state, new("MODEL", "REV"), retryDelay: TimeSpan.Zero);
        Assert.True(await engine.HandleAsync(new(new(1), new(1), new(13), true, new(42))));
        var response = Assert.Single(transport.Sent);
        Assert.Equal((byte)14, response.Function.Value); Assert.Equal((uint)42, response.SystemBytes.Value);
        var body = Assert.IsType<SecsListItem>(response.Item); Assert.Equal((byte)0, Assert.IsType<SecsBinaryItem>(body.Items[0]).Values.Span[0]);
        Assert.Equal("MODEL", Assert.IsType<SecsAsciiItem>(Assert.IsType<SecsListItem>(body.Items[1]).Items[0]).Value);
    }

    [Fact]
    public async Task S1F1RequiresCommunicationAndReturnsIdentity()
    {
        var transport = new FakeTransport(); var state = new GemCommunicationStateMachine(); state.Enable(false); state.Accept();
        var engine = new GemProtocolEngine(transport, state, new("MODEL", "REV"), retryDelay: TimeSpan.Zero);
        Assert.True(await engine.HandleAsync(new(new(1), new(1), new(1), true, new(9))));
        Assert.Equal((byte)2, Assert.Single(transport.Sent).Function.Value);
    }

    [Fact]
    public async Task UnsupportedStreamIsNotClaimedOrAnswered()
    {
        var transport = new FakeTransport(); var state = new GemCommunicationStateMachine(); state.Enable(false);
        var engine = new GemProtocolEngine(transport, state, new("MODEL", "REV"), retryDelay: TimeSpan.Zero);
        Assert.False(await engine.HandleAsync(new(new(1), new(2), new(1), true, new(9)))); Assert.Empty(transport.Sent);
    }

    [Fact]
    public void RuntimeComposesServicesWithoutOwningConnection()
    {
        var transport = new FakeTransport(); var runtime = new GemRuntime(transport, new("MODEL", "1"), spoolCapacity: 2);
        Assert.Same(transport.Connection, runtime.SecsConnection); Assert.Same(transport, runtime.Transport); Assert.Equal(GemCommunicationState.Disabled, runtime.Communication.State);
    }

    private sealed class FakeTransport : IGemMessageTransport
    {
        private uint _systemBytes;
        public Func<SecsMessage, SecsMessage>? ResponseFactory { get; init; }
        public Func<SecsMessage, Task<SecsMessage>>? AsyncResponseFactory { get; init; }
        public List<SecsMessage> Sent { get; } = [];
        public List<SecsMessage> Requests { get; } = [];
        public ISecsConnection Connection { get; } = new FakeConnection();
        public SecsSessionId SessionId { get; } = new(1);
        public event EventHandler<SecsMessage>? MessageReceived;
        public SecsSystemBytes AllocateSystemBytes() => new(++_systemBytes);
        public Task SendAsync(SecsMessage message, CancellationToken cancellationToken = default) { cancellationToken.ThrowIfCancellationRequested(); Sent.Add(message); return Task.CompletedTask; }
        public Task<SecsMessage> RequestAsync(SecsMessage message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); Requests.Add(message);
            return AsyncResponseFactory?.Invoke(message) ?? Task.FromResult(ResponseFactory?.Invoke(message) ?? throw new InvalidOperationException("No response configured."));
        }
        public void Raise(SecsMessage message) => MessageReceived?.Invoke(this, message);
    }

    private sealed class FakeConnection : ISecsConnection
    {
        public string ProviderKey => "test";
        public ConnectionState State => ConnectionState.Connected;
        public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
