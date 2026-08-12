using System.Net;
using System.Net.Sockets;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Protocol;
using Dreamine.Gem.StateMachines;
using Dreamine.Gem.Transport;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Interfaces;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Hsms;
using Xunit;

namespace Dreamine.Gem.Tests;

public sealed class GemHsmsLoopbackTests
{
    [Fact]
    public async Task HostEstablishesCommunicationAndReadsIdentityAcrossHsmsLoopback()
    {
        var port = ReservePort();
        await using var equipmentSession = CreateSession(port, SecsConnectionMode.Passive, SecsRole.Equipment);
        await using var hostSession = CreateSession(port, SecsConnectionMode.Active, SecsRole.Host);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await ConnectPairAsync(equipmentSession, hostSession, timeout.Token);
        await hostSession.SelectAsync(timeout.Token);

        ISecsMessageSession equipmentMessageSession = equipmentSession;
        var equipmentTransport = new HsmsGemTransport(equipmentMessageSession);
        var hostTransport = new HsmsGemTransport(hostSession, new(1));
        var communication = new GemCommunicationStateMachine(); communication.Enable(equipmentInitiated: false);
        var engine = new GemProtocolEngine(equipmentTransport, communication, new GemEquipmentIdentity("MODEL", "REV"), retryDelay: TimeSpan.Zero);
        var handlerFailure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        equipmentTransport.MessageReceived += (_, message) => Observe(engine.HandleAsync(message, timeout.Token), handlerFailure);

        var establish = new SecsMessage(new(1), new(1), new(13), true, hostTransport.AllocateSystemBytes());
        var establishReply = await hostTransport.RequestAsync(establish, timeout.Token);
        Assert.Equal((byte)14, establishReply.Function.Value);
        Assert.Equal((byte)0, Assert.IsType<SecsBinaryItem>(Assert.IsType<SecsListItem>(establishReply.Item).Items[0]).Values.Span[0]);

        var identityRequest = new SecsMessage(new(1), new(1), new(1), true, hostTransport.AllocateSystemBytes());
        var identityReply = await hostTransport.RequestAsync(identityRequest, timeout.Token);
        var identity = Assert.IsType<SecsListItem>(identityReply.Item);
        Assert.Equal("MODEL", Assert.IsType<SecsAsciiItem>(identity.Items[0]).Value);
        Assert.Equal("REV", Assert.IsType<SecsAsciiItem>(identity.Items[1]).Value);
        Assert.False(handlerFailure.Task.IsCompleted);
    }

    private static void Observe(Task operation, TaskCompletionSource<Exception> failure) => _ = operation.ContinueWith(
        task => failure.TrySetResult(task.Exception!.GetBaseException()),
        CancellationToken.None,
        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
        TaskScheduler.Default);

    private static HsmsSession CreateSession(int port, SecsConnectionMode mode, SecsRole role) => new(
        new HsmsSessionOptions { Host = "127.0.0.1", Port = port, Mode = mode, Role = role, SessionId = new SecsSessionId(1) });

    private static async Task ConnectPairAsync(HsmsSession passive, HsmsSession active, CancellationToken cancellationToken)
    {
        var passiveConnect = passive.ConnectAsync(cancellationToken);
        await WaitUntilAsync(() => passive.State == ConnectionState.Listening, cancellationToken);
        await active.ConnectAsync(cancellationToken); await passiveConnect;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        while (!condition()) { cancellationToken.ThrowIfCancellationRequested(); await Task.Yield(); }
    }

    private static int ReservePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return port;
    }
}
