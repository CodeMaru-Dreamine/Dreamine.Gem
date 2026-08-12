using Dreamine.Gem.Abstractions.Interfaces;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Protocol;
using Dreamine.Gem.Profiles;
using Dreamine.Gem.Services;
using Dreamine.Gem.StateMachines;
using Dreamine.Secs.Abstractions.Interfaces;

namespace Dreamine.Gem;

/// <summary>\if KO GEM의 독립 상태 모델과 기능별 서비스를 조립하는 런타임입니다. 전송 연결의 소유권은 호출자에게 있습니다. \endif \if EN Composes independent GEM state models and feature services; the caller owns the transport connection. \endif</summary>
public sealed class GemRuntime : IGemRuntime
{
    /// <summary>\if KO 전송, 장비 식별 정보, 시간 정책 및 스풀 용량으로 런타임을 만듭니다. \endif \if EN Creates a runtime from transport, equipment identity, time policy, and spool capacity. \endif</summary>
    public GemRuntime(IGemMessageTransport transport, GemEquipmentIdentity identity, TimeProvider? timeProvider = null, int spoolCapacity = 1024)
    {
        Transport = transport ?? throw new ArgumentNullException(nameof(transport));
        ArgumentNullException.ThrowIfNull(identity);
        Communication = new(); Control = new(); Processing = new();
        Variables = new(); Constants = new(); Events = new(Variables, timeProvider); Alarms = new(); Commands = new(timeProvider, () => Control.State); ProcessPrograms = new(); Clock = new(timeProvider); Spool = new(spoolCapacity);
        Protocol = new(transport, Communication, identity, timeProvider);
    }
    /// <inheritdoc />
    public ISecsConnection SecsConnection => Transport.Connection;
    /// <summary>\if KO GEM 메시지 전송 경계입니다. \endif \if EN Gets the GEM message transport. \endif</summary>
    public IGemMessageTransport Transport { get; }
    /// <summary>\if KO 이 런타임을 구성한 frozen 장비 프로필입니다. 기존 생성자로 만든 런타임은 null입니다. \endif \if EN Gets the frozen equipment profile that configured this runtime; null for runtimes created by the legacy constructor. \endif</summary>
    public GemEquipmentProfile? Profile { get; private set; }
    /// <summary>\if KO 통신 상태 모델입니다. \endif \if EN Gets the communication state model. \endif</summary>
    public GemCommunicationStateMachine Communication { get; }
    /// <summary>\if KO 제어 상태 모델입니다. \endif \if EN Gets the control state model. \endif</summary>
    public GemControlStateMachine Control { get; }
    /// <summary>\if KO 장비 처리 상태 모델입니다. \endif \if EN Gets the equipment-processing state model. \endif</summary>
    public GemProcessingStateMachine Processing { get; }
    /// <summary>\if KO 변수 카탈로그입니다. \endif \if EN Gets the variable catalog. \endif</summary>
    public GemVariableCatalog Variables { get; }
    /// <summary>\if KO 장비 상수 서비스입니다. \endif \if EN Gets the equipment-constant service. \endif</summary>
    public GemEquipmentConstantService Constants { get; }
    /// <summary>\if KO 이벤트·보고서 서비스입니다. \endif \if EN Gets the event/report service. \endif</summary>
    public GemEventReportService Events { get; }
    /// <summary>\if KO 알람 서비스입니다. \endif \if EN Gets the alarm service. \endif</summary>
    public GemAlarmService Alarms { get; }
    /// <summary>\if KO 원격 명령 서비스입니다. \endif \if EN Gets the remote-command service. \endif</summary>
    public GemRemoteCommandService Commands { get; }
    /// <summary>\if KO 공정 프로그램 서비스입니다. \endif \if EN Gets the process-program service. \endif</summary>
    public GemProcessProgramService ProcessPrograms { get; }
    /// <summary>\if KO 논리 GEM 시계입니다. \endif \if EN Gets the logical GEM clock. \endif</summary>
    public GemClockService Clock { get; }
    /// <summary>\if KO 제한 용량 메모리 스풀입니다. \endif \if EN Gets the bounded memory spool. \endif</summary>
    public GemSpoolService Spool { get; }
    /// <summary>\if KO 지원되는 S1 시나리오 엔진입니다. \endif \if EN Gets the supported S1 scenario engine. \endif</summary>
    public GemProtocolEngine Protocol { get; }

    internal void ApplyProfile(GemEquipmentProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (Profile is not null) throw new InvalidOperationException("A GEM profile is already applied.");
        Profile = profile;
        profile.Configure(this);
    }
}
