using Dreamine.Gem.Abstractions.States;
using Dreamine.Gem.StateMachines;
using Xunit;

namespace Dreamine.Gem.Tests;

public sealed class StateMachineTests
{
    [Fact]
    public void CommunicationEquipmentInitiatedFlowReachesCommunicating()
    {
        var machine = new GemCommunicationStateMachine();
        machine.Enable(true); machine.RequestSent(); machine.Accept();
        Assert.Equal(GemCommunicationState.EnabledCommunicating, machine.State);
        Assert.Equal(GemEstablishmentState.None, machine.EstablishmentState);
    }

    [Fact]
    public void CommunicationRetryAndLossReturnToExpectedSubstates()
    {
        var machine = new GemCommunicationStateMachine();
        machine.Enable(true); machine.RequestSent(); machine.Retry(); machine.RequestSent(); machine.Accept(); machine.CommunicationLost(false);
        Assert.Equal(GemEstablishmentState.WaitCrFromHost, machine.EstablishmentState);
    }

    [Fact]
    public void CommunicationRejectsInvalidTransition() =>
        Assert.Throws<InvalidOperationException>(() => new GemCommunicationStateMachine().Accept());

    [Fact]
    public void ControlTraversesOfflineLocalRemoteAndHostOffline()
    {
        var machine = new GemControlStateMachine();
        machine.AttemptOnline(); machine.AcceptOnline(); machine.SelectRemote(); machine.SelectLocal(); machine.HostOffline();
        Assert.Equal(GemControlState.HostOffline, machine.State);
    }

    [Fact]
    public void ControlRejectsRemoteWhileOffline() =>
        Assert.Throws<InvalidOperationException>(() => new GemControlStateMachine().SelectRemote());

    [Fact]
    public void ProcessingTraversesNominalAndPauseFlow()
    {
        var machine = new GemProcessingStateMachine();
        machine.CompleteInitialization(); machine.BeginSetup(); machine.Ready(); machine.Execute(); machine.Pause(); machine.Resume(); machine.Complete();
        Assert.Equal(GemProcessingState.Idle, machine.State);
    }

    [Fact]
    public void ProcessingAbortWorksForEveryActiveLevel()
    {
        var machine = new GemProcessingStateMachine(); machine.CompleteInitialization(); machine.BeginSetup(); machine.Abort();
        Assert.Equal(GemProcessingState.Idle, machine.State);
    }

    [Fact]
    public void ProcessingRejectsPauseOutsideExecution() =>
        Assert.Throws<InvalidOperationException>(() => new GemProcessingStateMachine().Pause());
}
