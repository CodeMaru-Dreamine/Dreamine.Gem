using Dreamine.Gem.Abstractions.States;

namespace Dreamine.Gem.StateMachines;

/// <summary>\if KO E30 장비 처리 상태 전이를 직렬화합니다. \endif \if EN Serializes E30 equipment-processing transitions. \endif</summary>
public sealed class GemProcessingStateMachine
{
    private readonly object _gate = new();
    private GemProcessingState _state = GemProcessingState.Initializing;
    /// <summary>\if KO 현재 처리 상태입니다. \endif \if EN Gets the current processing state. \endif</summary>
    public GemProcessingState State { get { lock (_gate) return _state; } }
    /// <summary>\if KO 초기화를 완료합니다. \endif \if EN Completes initialization. \endif</summary>
    public void CompleteInitialization() => Move(GemProcessingState.Initializing, GemProcessingState.Idle);
    /// <summary>\if KO 공정 설정을 시작합니다. \endif \if EN Begins process setup. \endif</summary>
    public void BeginSetup() => Move(GemProcessingState.Idle, GemProcessingState.Setup);
    /// <summary>\if KO 공정 실행 준비를 완료합니다. \endif \if EN Marks the process ready. \endif</summary>
    public void Ready() => Move(GemProcessingState.Setup, GemProcessingState.Ready);
    /// <summary>\if KO 공정 실행을 시작합니다. \endif \if EN Starts execution. \endif</summary>
    public void Execute() => Move(GemProcessingState.Ready, GemProcessingState.Executing);
    /// <summary>\if KO 실행을 일시 정지합니다. \endif \if EN Pauses execution. \endif</summary>
    public void Pause() => Move(GemProcessingState.Executing, GemProcessingState.Paused);
    /// <summary>\if KO 일시 정지된 실행을 재개합니다. \endif \if EN Resumes paused execution. \endif</summary>
    public void Resume() => Move(GemProcessingState.Paused, GemProcessingState.Executing);
    /// <summary>\if KO 실행을 완료하고 유휴 상태로 돌아갑니다. \endif \if EN Completes execution and returns idle. \endif</summary>
    public void Complete() => Move(GemProcessingState.Executing, GemProcessingState.Idle);
    /// <summary>\if KO 활성 공정을 중단하고 유휴 상태로 돌아갑니다. \endif \if EN Aborts an active process and returns idle. \endif</summary>
    public void Abort()
    {
        lock (_gate)
        {
            if (_state is not (GemProcessingState.Setup or GemProcessingState.Ready or GemProcessingState.Executing or GemProcessingState.Paused)) throw new InvalidOperationException("No active process can be aborted.");
            _state = GemProcessingState.Idle;
        }
    }
    private void Move(GemProcessingState expected, GemProcessingState next) { lock (_gate) { if (_state != expected) throw new InvalidOperationException($"Expected {expected}, but state is {_state}."); _state = next; } }
}
