using System.Collections.ObjectModel;
using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.Gem.Protocol.E30;

/// <summary>\if KO E30-0611 파생 부분 프로필의 기능 판정입니다. 표준 적합성 판정이 아닙니다. \endif \if EN Describes a capability disposition in the E30-0611 derived subset profile; it is not a standards-conformance verdict. \endif</summary>
public enum E30CapabilityDisposition
{
    /// <summary>\if KO 로컬 구조·상태·loopback 검증 범위에서 구현되었습니다. \endif \if EN Implemented within the local structural, state, and loopback verification boundary. \endif</summary>
    Implemented,
    /// <summary>\if KO 구현되었으나 독립 외부 검증이 남아 있습니다. \endif \if EN Implemented while independent external verification remains outstanding. \endif</summary>
    ImplementedUnverified,
    /// <summary>\if KO 현재 보유 표준 근거만으로 의미를 닫을 수 없어 차단되었습니다. \endif \if EN Blocked because the available standards evidence does not close the required semantics. \endif</summary>
    BlockedStandard,
    /// <summary>\if KO 현재 provider/router 관찰 경계에서 필요한 원본 문맥을 얻을 수 없어 차단되었습니다. \endif \if EN Blocked because the current provider/router observation boundary does not expose the required original context. \endif</summary>
    BlockedBoundary,
    /// <summary>\if KO 동결된 v1 범위에서 의도적으로 제외되었습니다. \endif \if EN Intentionally excluded from the frozen v1 scope. \endif</summary>
    IntentionallyExcluded
}

/// <summary>\if KO 하나의 E30 파생 기능과 근거 제한을 설명합니다. \endif \if EN Describes one E30-derived capability and its evidence boundary. \endif</summary>
public sealed record E30CapabilityEntry
{
    /// <summary>\if KO 기능 항목을 만듭니다. \endif \if EN Creates a capability entry. \endif</summary>
    public E30CapabilityEntry(string name, E30CapabilityDisposition disposition, string rationale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(rationale);
        Name = name;
        Disposition = disposition;
        Rationale = rationale;
    }

    /// <summary>\if KO 기능 이름입니다. \endif \if EN Gets the capability name. \endif</summary>
    public string Name { get; }
    /// <summary>\if KO 구현 판정입니다. \endif \if EN Gets the implementation disposition. \endif</summary>
    public E30CapabilityDisposition Disposition { get; }
    /// <summary>\if KO 판정 근거와 제한입니다. \endif \if EN Gets the rationale and limitation. \endif</summary>
    public string Rationale { get; }
}

/// <summary>\if KO 동결된 E30-0611 파생 부분 프로필 v1의 대화 목록과 명시적 제외 목록입니다. \endif \if EN Exposes the dialogue catalog and explicit exclusions for the frozen E30-0611 derived subset profile v1. \endif</summary>
public static class E30DerivedSubsetManifest
{
    /// <summary>\if KO 표준 적합성 주장이 아닌 동결 프로필 이름입니다. \endif \if EN Gets the frozen profile name, which is not a standards-conformance claim. \endif</summary>
    public const string ProfileName = "E30-0611 derived subset profile v1";

    private static readonly ReadOnlyCollection<SecsDialogueDefinition> IncludedValue = Array.AsReadOnly(
    new SecsDialogueDefinition[]
    {
        E30Dialogues.S1F1, E30Dialogues.S1F3, E30Dialogues.S1F11, E30Dialogues.S1F13,
        E30Dialogues.S1F15, E30Dialogues.S1F17,
        E30Dialogues.S2F13, E30Dialogues.S2F15, E30Dialogues.S2F17, E30Dialogues.S2F29,
        E30Dialogues.S2F31, E30Dialogues.S2F33, E30Dialogues.S2F35, E30Dialogues.S2F37,
        E30Dialogues.S2F41,
        E30Dialogues.S5F1, E30Dialogues.S5F3, E30Dialogues.S5F5,
        E30Dialogues.S6F11, E30Dialogues.S6F15
    });

    private static readonly ReadOnlyCollection<E30CapabilityEntry> EntriesValue = Array.AsReadOnly(
    new E30CapabilityEntry[]
    {
        new("S1F1/2, S1F3/4, S1F11/12, S1F13/14, S1F15/16, S1F17/18", E30CapabilityDisposition.ImplementedUnverified, "E5-0813 message structures and E30-0611 fundamental state gates; external simulator verification remains separate."),
        new("S2F13/14, S2F15/16, S2F17/18, S2F29/30, S2F31/32", E30CapabilityDisposition.ImplementedUnverified, "Single-block equipment-constant and clock dialogues with atomic update and no-mutation rejection paths."),
        new("S2F33/34, S2F35/36, S2F37/38", E30CapabilityDisposition.ImplementedUnverified, "Single-block dynamic report definition, link, and enablement only."),
        new("S2F35 empty-list unlink/delete variants", E30CapabilityDisposition.BlockedStandard, "The available trace does not close whether an empty outer link list or an empty RPTID list unlinks one or all events; v1 terminates those variants without mutation."),
        new("S2F41/42", E30CapabilityDisposition.ImplementedUnverified, "Online-remote acceptance is separate from asynchronous command completion."),
        new("S5F1/2, S5F3/4, S5F5/6", E30CapabilityDisposition.ImplementedUnverified, "ACKC5 nonzero values remain exposed as raw rejection codes."),
        new("S6F11/12, S6F15/16", E30CapabilityDisposition.ImplementedUnverified, "Single-block event reports preserve report and VID order; ACKC6 nonzero values remain raw."),
        new("S6F19/20", E30CapabilityDisposition.BlockedStandard, "The available E5 text does not close the response representation for an unknown RPTID."),
        new("S2F39/40 and S6F5/6", E30CapabilityDisposition.IntentionallyExcluded, "The v1 profile supports only messages that fit the configured single-block size boundary."),
        new("S9F3/S9F5", E30CapabilityDisposition.ImplementedUnverified, "The equipment-side exact dispatcher fallback distinguishes an unrecognized host stream from an unrecognized function and preserves the offending header."),
        new("S9F7", E30CapabilityDisposition.ImplementedUnverified, "Host-to-equipment known-dialogue structural failures observable by the equipment router emit illegal-data with the offending header."),
        new("S9F11", E30CapabilityDisposition.ImplementedUnverified, "Host-to-equipment bodies exceeding the configured single-block boundary emit data-too-long with the offending header."),
        new("S9F1", E30CapabilityDisposition.BlockedBoundary, "A mismatched Session ID is rejected below the hosted router and no source-bound offending message context is exposed to it."),
        new("S9F9", E30CapabilityDisposition.BlockedBoundary, "The safe transaction API reports timeout without exposing the original on-wire header/System Bytes needed for SHEAD emission."),
        new("S9F13", E30CapabilityDisposition.BlockedBoundary, "The frozen subset has no implemented conversation state whose timeout supplies both MEXP and EDID; no synthetic conversation is invented."),
        new("S2F23/24 and S6F1/2 trace", E30CapabilityDisposition.IntentionallyExcluded, "Trace is outside the frozen v1 subset."),
        new("S2F45/46/47/48 limits", E30CapabilityDisposition.IntentionallyExcluded, "Limits monitoring is outside the frozen v1 subset."),
        new("S2F43/44 and S6F23/24 spooling", E30CapabilityDisposition.IntentionallyExcluded, "Wire spooling is outside the frozen v1 subset."),
        new("S7 process programs, S10 terminal, material handling, S2F49/50 enhanced remote command", E30CapabilityDisposition.IntentionallyExcluded, "These capability families are outside the frozen v1 subset.")
    });

    /// <summary>\if KO 정상 인접 Secondary를 갖는 포함 대화의 불변 목록입니다. \endif \if EN Gets the immutable list of included dialogues with adjacent normal secondaries. \endif</summary>
    public static IReadOnlyList<SecsDialogueDefinition> IncludedDialogues => IncludedValue;

    /// <summary>\if KO 구현·차단·제외 상태의 불변 목록입니다. \endif \if EN Gets the immutable capability disposition list. \endif</summary>
    public static IReadOnlyList<E30CapabilityEntry> Capabilities => EntriesValue;
}

/// <summary>\if KO E30-0611 파생 부분 프로필 v1에서 사용하는 정상 SECS 대화 정의입니다. \endif \if EN Defines the normal SECS dialogues used by the E30-0611 derived subset profile v1. \endif</summary>
public static class E30Dialogues
{
    /// <summary>\if KO S1F1/F2 대화입니다. \endif \if EN Gets the S1F1/F2 dialogue. \endif</summary>
    public static SecsDialogueDefinition S1F1 { get; } = W1(1, 1);
    /// <summary>\if KO S1F3/F4 대화입니다. \endif \if EN Gets the S1F3/F4 dialogue. \endif</summary>
    public static SecsDialogueDefinition S1F3 { get; } = W1(1, 3);
    /// <summary>\if KO S1F11/F12 대화입니다. \endif \if EN Gets the S1F11/F12 dialogue. \endif</summary>
    public static SecsDialogueDefinition S1F11 { get; } = W1(1, 11);
    /// <summary>\if KO S1F13/F14 대화입니다. \endif \if EN Gets the S1F13/F14 dialogue. \endif</summary>
    public static SecsDialogueDefinition S1F13 { get; } = W1(1, 13);
    /// <summary>\if KO S1F15/F16 대화입니다. \endif \if EN Gets the S1F15/F16 dialogue. \endif</summary>
    public static SecsDialogueDefinition S1F15 { get; } = W1(1, 15);
    /// <summary>\if KO S1F17/F18 대화입니다. \endif \if EN Gets the S1F17/F18 dialogue. \endif</summary>
    public static SecsDialogueDefinition S1F17 { get; } = W1(1, 17);
    /// <summary>\if KO S2F13/F14 대화입니다. \endif \if EN Gets the S2F13/F14 dialogue. \endif</summary>
    public static SecsDialogueDefinition S2F13 { get; } = W1(2, 13);
    /// <summary>\if KO S2F15/F16 대화입니다. \endif \if EN Gets the S2F15/F16 dialogue. \endif</summary>
    public static SecsDialogueDefinition S2F15 { get; } = W1(2, 15);
    /// <summary>\if KO S2F17/F18 대화입니다. \endif \if EN Gets the S2F17/F18 dialogue. \endif</summary>
    public static SecsDialogueDefinition S2F17 { get; } = W1(2, 17);
    /// <summary>\if KO S2F29/F30 대화입니다. \endif \if EN Gets the S2F29/F30 dialogue. \endif</summary>
    public static SecsDialogueDefinition S2F29 { get; } = W1(2, 29);
    /// <summary>\if KO S2F31/F32 대화입니다. \endif \if EN Gets the S2F31/F32 dialogue. \endif</summary>
    public static SecsDialogueDefinition S2F31 { get; } = W1(2, 31);
    /// <summary>\if KO S2F33/F34 대화입니다. \endif \if EN Gets the S2F33/F34 dialogue. \endif</summary>
    public static SecsDialogueDefinition S2F33 { get; } = W1(2, 33);
    /// <summary>\if KO S2F35/F36 대화입니다. \endif \if EN Gets the S2F35/F36 dialogue. \endif</summary>
    public static SecsDialogueDefinition S2F35 { get; } = W1(2, 35);
    /// <summary>\if KO S2F37/F38 대화입니다. \endif \if EN Gets the S2F37/F38 dialogue. \endif</summary>
    public static SecsDialogueDefinition S2F37 { get; } = W1(2, 37);
    /// <summary>\if KO S2F41/F42 대화입니다. \endif \if EN Gets the S2F41/F42 dialogue. \endif</summary>
    public static SecsDialogueDefinition S2F41 { get; } = W1(2, 41);
    /// <summary>\if KO S5F1/F2 대화입니다. \endif \if EN Gets the S5F1/F2 dialogue. \endif</summary>
    public static SecsDialogueDefinition S5F1 { get; } = W1(5, 1);
    /// <summary>\if KO S5F3/F4 대화입니다. \endif \if EN Gets the S5F3/F4 dialogue. \endif</summary>
    public static SecsDialogueDefinition S5F3 { get; } = W1(5, 3);
    /// <summary>\if KO S5F5/F6 대화입니다. \endif \if EN Gets the S5F5/F6 dialogue. \endif</summary>
    public static SecsDialogueDefinition S5F5 { get; } = W1(5, 5);
    /// <summary>\if KO S6F11/F12 대화입니다. \endif \if EN Gets the S6F11/F12 dialogue. \endif</summary>
    public static SecsDialogueDefinition S6F11 { get; } = W1(6, 11);
    /// <summary>\if KO S6F15/F16 대화입니다. \endif \if EN Gets the S6F15/F16 dialogue. \endif</summary>
    public static SecsDialogueDefinition S6F15 { get; } = W1(6, 15);

    private static SecsDialogueDefinition W1(byte stream, byte primary) => new(new(stream), new(primary), new((byte)(primary + 1)));
}
