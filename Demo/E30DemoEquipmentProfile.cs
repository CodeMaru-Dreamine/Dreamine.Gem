using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Abstractions.States;
using Dreamine.Gem.Profiles;
using Dreamine.Gem.Protocol.E30;
using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.Gem.Demo;

/// <summary>
/// \if KO
/// 공개 샘플과 Workbench가 함께 사용하는 작고 범용적인 Demo 장비 프로필입니다.
/// 고객 장비 의미를 포함하지 않으며 SEMI/GEM 적합성을 주장하지 않습니다.
/// \endif
/// \if EN
/// Provides the small generic Demo equipment profile shared by the public sample and Workbench.
/// It contains no customer-equipment semantics and makes no SEMI/GEM conformance claim.
/// \endif
/// </summary>
public static class E30DemoEquipmentProfile
{
    /// <summary>\if KO Demo 프로필 이름입니다. \endif \if EN Gets the Demo profile name. \endif</summary>
    public const string ProfileName = "Dreamine generic E30 demo equipment";
    /// <summary>\if KO 장비 상태 SVID입니다. \endif \if EN Gets the equipment-state SVID. \endif</summary>
    public const ulong EquipmentStateVariableId = 1;
    /// <summary>\if KO 완료 횟수 DVID입니다. \endif \if EN Gets the completed-count DVID. \endif</summary>
    public const ulong CompletedCountVariableId = 2;
    /// <summary>\if KO Demo batch 크기 ECID입니다. \endif \if EN Gets the Demo batch-size ECID. \endif</summary>
    public const ulong BatchSizeConstantId = 100;
    /// <summary>\if KO 기본 RPTID입니다. \endif \if EN Gets the default RPTID. \endif</summary>
    public const ulong StatusReportId = 10;
    /// <summary>\if KO 상태 CEID입니다. \endif \if EN Gets the status CEID. \endif</summary>
    public const ulong StatusEventId = 1000;
    /// <summary>\if KO 원격 명령 완료 CEID입니다. \endif \if EN Gets the remote-command-completion CEID. \endif</summary>
    public const ulong CommandCompletedEventId = 1001;
    /// <summary>\if KO Demo ALID입니다. \endif \if EN Gets the Demo ALID. \endif</summary>
    public const ulong AlarmId = 2000;
    /// <summary>\if KO Demo 원격 명령 이름입니다. \endif \if EN Gets the Demo remote-command name. \endif</summary>
    public const string StartCommand = "START";

    private static readonly Lazy<GemEquipmentProfile> Shared = new(CreateCore);

    /// <summary>
    /// \if KO 불변이며 여러 장비 context에서 공유 가능한 Demo 프로필을 반환합니다. \endif
    /// \if EN Returns the immutable Demo profile that can be shared by multiple equipment contexts. \endif
    /// </summary>
    public static GemEquipmentProfile Create() => Shared.Value;

    private static GemEquipmentProfile CreateCore() => new GemEquipmentProfileBuilder(
            ProfileName,
            new GemEquipmentIdentity("DREAMINE-DEMO", "1.0"),
            E30DerivedSubsetManifest.ProfileName)
        .AddVariable(
            new GemVariableDefinition(EquipmentStateVariableId, "EquipmentState", GemVariableKind.Status),
            SecsItemFormat.Ascii,
            static _ => ValueTask.FromResult<SecsItem>(new SecsAsciiItem("IDLE")))
        .AddVariable(
            new GemVariableDefinition(CompletedCountVariableId, "CompletedCount", GemVariableKind.Data, units: "count"),
            SecsItemFormat.UInt32,
            static _ => ValueTask.FromResult<SecsItem>(new SecsUInt32Item(0)))
        .AddEquipmentConstant(
            new GemEquipmentConstantDefinition(
                BatchSizeConstantId,
                "DemoBatchSize",
                new SecsUInt16Item(10),
                units: "substrates",
                minimumValue: new SecsUInt16Item(1),
                maximumValue: new SecsUInt16Item(100)),
            SecsItemFormat.UInt16,
            static value => value is SecsUInt16Item item && item.Count == 1 && item.Values.Span[0] is >= 1 and <= 100,
            [GemControlState.OnlineLocal, GemControlState.OnlineRemote])
        .AddReport(new GemReportDefinition(StatusReportId, [EquipmentStateVariableId, CompletedCountVariableId]))
        .AddCollectionEvent(new GemCollectionEventDefinition(StatusEventId, "DemoStatus", [StatusReportId], enabled: true))
        .AddCollectionEvent(new GemCollectionEventDefinition(CommandCompletedEventId, "DemoCommandCompleted", enabled: true))
        .AddAlarm(new GemAlarmDefinition(AlarmId, 1, "DEMO ALARM"))
        .AddRemoteCommand(
            new GemRemoteCommandProfileDefinition(StartCommand,
            [
                new GemRemoteCommandParameterDefinition(
                    "LOT",
                    SecsItemFormat.Ascii,
                    required: true,
                    static value => value is SecsAsciiItem item && item.Value.Length is >= 1 and <= 32)
            ]),
            static (_, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(new GemCommandResult(GemCommandStatus.Completed));
            })
        .Build();
}
