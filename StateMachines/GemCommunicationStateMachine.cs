using Dreamine.Gem.Abstractions.States;

namespace Dreamine.Gem.StateMachines;

/// <summary>\if KO E30 통신 상태와 통신 수립 하위 상태를 직렬화합니다. \endif \if EN Serializes E30 communication and establishment state transitions. \endif</summary>
public sealed class GemCommunicationStateMachine
{
    private readonly object _gate = new();
    private GemCommunicationState _state = GemCommunicationState.Disabled;
    private GemEstablishmentState _establishmentState;

    /// <summary>\if KO 현재 통신 상태입니다. \endif \if EN Gets the current communication state. \endif</summary>
    public GemCommunicationState State { get { lock (_gate) return _state; } }
    /// <summary>\if KO 현재 통신 수립 하위 상태입니다. \endif \if EN Gets the current establishment substate. \endif</summary>
    public GemEstablishmentState EstablishmentState { get { lock (_gate) return _establishmentState; } }

    /// <summary>\if KO 통신 기능을 활성화합니다. \endif \if EN Enables communication. \endif</summary>
    public void Enable(bool equipmentInitiated)
    {
        lock (_gate)
        {
            Require(_state == GemCommunicationState.Disabled, "Communication is already enabled.");
            _state = GemCommunicationState.EnabledNotCommunicating;
            _establishmentState = equipmentInitiated ? GemEstablishmentState.WaitDelay : GemEstablishmentState.WaitCrFromHost;
        }
    }

    /// <summary>\if KO 통신 기능을 비활성화합니다. \endif \if EN Disables communication. \endif</summary>
    public void Disable() { lock (_gate) { _state = GemCommunicationState.Disabled; _establishmentState = GemEstablishmentState.None; } }

    /// <summary>\if KO 장비 주도 통신 요청의 전송을 기록합니다. \endif \if EN Records transmission of an equipment-initiated communication request. \endif</summary>
    public void RequestSent()
    {
        lock (_gate)
        {
            Require(_state == GemCommunicationState.EnabledNotCommunicating && _establishmentState is GemEstablishmentState.WaitDelay or GemEstablishmentState.WaitCra, "A communication request cannot be sent in the current state.");
            _establishmentState = GemEstablishmentState.WaitCra;
        }
    }

    /// <summary>\if KO 통신 수립 수락을 기록합니다. 동시 수립 시도에도 사용할 수 있습니다. \endif \if EN Records accepted communication establishment, including simultaneous attempts. \endif</summary>
    public void Accept()
    {
        lock (_gate)
        {
            Require(_state == GemCommunicationState.EnabledNotCommunicating, "Communication cannot be accepted in the current state.");
            _state = GemCommunicationState.EnabledCommunicating; _establishmentState = GemEstablishmentState.None;
        }
    }

    /// <summary>\if KO 통신 요청 거부 또는 제한 시간 만료 후 재시도를 예약합니다. \endif \if EN Schedules a retry after rejection or timeout. \endif</summary>
    public void Retry()
    {
        lock (_gate)
        {
            Require(_state == GemCommunicationState.EnabledNotCommunicating, "A retry cannot be scheduled in the current state.");
            _establishmentState = GemEstablishmentState.WaitDelay;
        }
    }

    /// <summary>\if KO 수립된 통신의 손실을 기록합니다. \endif \if EN Records loss of established communication. \endif</summary>
    public void CommunicationLost(bool equipmentInitiated)
    {
        lock (_gate)
        {
            Require(_state == GemCommunicationState.EnabledCommunicating, "Communication is not established.");
            _state = GemCommunicationState.EnabledNotCommunicating;
            _establishmentState = equipmentInitiated ? GemEstablishmentState.WaitDelay : GemEstablishmentState.WaitCrFromHost;
        }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
