using Dreamine.Gem.Abstractions.States;

namespace Dreamine.Gem.StateMachines;

/// <summary>\if KO E30 장비 제어 상태 전이를 직렬화합니다. \endif \if EN Serializes E30 equipment-control transitions. \endif</summary>
public sealed class GemControlStateMachine
{
    private readonly object _gate = new();
    private GemControlState _state = GemControlState.EquipmentOffline;
    /// <summary>\if KO 현재 제어 상태입니다. \endif \if EN Gets the current control state. \endif</summary>
    public GemControlState State { get { lock (_gate) return _state; } }
    /// <summary>\if KO 장비가 온라인 전환을 시도하게 합니다. \endif \if EN Starts an equipment-initiated online attempt. \endif</summary>
    public void AttemptOnline() { lock (_gate) { Require(_state == GemControlState.EquipmentOffline, "Online may only be attempted from equipment offline."); _state = GemControlState.AttemptOnline; } }
    /// <summary>\if KO 온라인 시도를 로컬 상태로 수락합니다. \endif \if EN Accepts an online attempt into local control. \endif</summary>
    public void AcceptOnline() { lock (_gate) { Require(_state is GemControlState.AttemptOnline or GemControlState.HostOffline, "Online cannot be accepted in the current state."); _state = GemControlState.OnlineLocal; } }
    /// <summary>\if KO 온라인 시도를 거부하여 장비 오프라인으로 돌아갑니다. \endif \if EN Rejects an online attempt and returns equipment offline. \endif</summary>
    public void RejectOnline() { lock (_gate) { Require(_state == GemControlState.AttemptOnline, "No online attempt is active."); _state = GemControlState.EquipmentOffline; } }
    /// <summary>\if KO 호스트 오프라인으로 전환합니다. \endif \if EN Transitions to host offline. \endif</summary>
    public void HostOffline() { lock (_gate) { Require(_state is GemControlState.OnlineLocal or GemControlState.OnlineRemote, "Host offline requires an online state."); _state = GemControlState.HostOffline; } }
    /// <summary>\if KO 장비 오프라인으로 전환합니다. \endif \if EN Transitions to equipment offline. \endif</summary>
    public void EquipmentOffline() { lock (_gate) _state = GemControlState.EquipmentOffline; }
    /// <summary>\if KO 온라인 로컬로 전환합니다. \endif \if EN Selects online local control. \endif</summary>
    public void SelectLocal() { lock (_gate) { Require(_state is GemControlState.OnlineRemote or GemControlState.OnlineLocal, "Local control requires an online state."); _state = GemControlState.OnlineLocal; } }
    /// <summary>\if KO 온라인 원격으로 전환합니다. \endif \if EN Selects online remote control. \endif</summary>
    public void SelectRemote() { lock (_gate) { Require(_state is GemControlState.OnlineLocal or GemControlState.OnlineRemote, "Remote control requires an online state."); _state = GemControlState.OnlineRemote; } }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
