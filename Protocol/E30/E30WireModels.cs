using System.Collections.ObjectModel;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.Gem.Protocol.E30;

/// <summary>\if KO E30 파생 프로필 ID에 사용하는 고정 무부호 정수 형식입니다. \endif \if EN Defines a fixed unsigned-integer representation for E30-derived identifiers. \endif</summary>
public enum E30IdentifierFormat
{
    /// <summary>\if KO U1 식별자 형식입니다. \endif \if EN Selects the U1 identifier format. \endif</summary>
    UInt8,
    /// <summary>\if KO U2 식별자 형식입니다. \endif \if EN Selects the U2 identifier format. \endif</summary>
    UInt16,
    /// <summary>\if KO U4 식별자 형식입니다. \endif \if EN Selects the U4 identifier format. \endif</summary>
    UInt32,
    /// <summary>\if KO U8 식별자 형식입니다. \endif \if EN Selects the U8 identifier format. \endif</summary>
    UInt64
}

/// <summary>\if KO wire ID 계열별 고정 형식을 선택합니다. 동일한 컨텍스트에서 형식 추측을 허용하지 않습니다. \endif \if EN Selects a fixed wire format per identifier family and prevents per-message format guessing in one context. \endif</summary>
public sealed record E30IdentifierPolicy
{
    /// <summary>\if KO 기본 U4 정책을 만듭니다. \endif \if EN Creates the default U4 policy. \endif</summary>
    public E30IdentifierPolicy(
        E30IdentifierFormat variable = E30IdentifierFormat.UInt32,
        E30IdentifierFormat statusVariable = E30IdentifierFormat.UInt32,
        E30IdentifierFormat equipmentConstant = E30IdentifierFormat.UInt32,
        E30IdentifierFormat data = E30IdentifierFormat.UInt32,
        E30IdentifierFormat report = E30IdentifierFormat.UInt32,
        E30IdentifierFormat collectionEvent = E30IdentifierFormat.UInt32,
        E30IdentifierFormat alarm = E30IdentifierFormat.UInt32)
    {
        Variable = variable;
        StatusVariable = statusVariable;
        EquipmentConstant = equipmentConstant;
        Data = data;
        Report = report;
        CollectionEvent = collectionEvent;
        Alarm = alarm;
    }

    /// <summary>\if KO 장비 프로필의 계열별 형식 정책에서 wire 정책을 만듭니다. \endif \if EN Creates a wire policy from an equipment profile's per-family format policy. \endif</summary>
    public E30IdentifierPolicy(GemIdentifierFormatPolicy policy)
        : this(
            Convert(policy, GemIdentifierFamily.Variable),
            Convert(policy, GemIdentifierFamily.StatusVariable),
            Convert(policy, GemIdentifierFamily.EquipmentConstant),
            Convert(policy, GemIdentifierFamily.DataIdentifier),
            Convert(policy, GemIdentifierFamily.Report),
            Convert(policy, GemIdentifierFamily.CollectionEvent),
            Convert(policy, GemIdentifierFamily.Alarm)) { }

    /// <summary>\if KO 일반 VID 형식입니다. \endif \if EN Gets the general VID format. \endif</summary>
    public E30IdentifierFormat Variable { get; }
    /// <summary>\if KO SVID 형식입니다. \endif \if EN Gets the SVID format. \endif</summary>
    public E30IdentifierFormat StatusVariable { get; }
    /// <summary>\if KO ECID 형식입니다. \endif \if EN Gets the ECID format. \endif</summary>
    public E30IdentifierFormat EquipmentConstant { get; }
    /// <summary>\if KO DATAID 형식입니다. \endif \if EN Gets the DATAID format. \endif</summary>
    public E30IdentifierFormat Data { get; }
    /// <summary>\if KO RPTID 형식입니다. \endif \if EN Gets the RPTID format. \endif</summary>
    public E30IdentifierFormat Report { get; }
    /// <summary>\if KO CEID 형식입니다. \endif \if EN Gets the CEID format. \endif</summary>
    public E30IdentifierFormat CollectionEvent { get; }
    /// <summary>\if KO ALID 형식입니다. \endif \if EN Gets the ALID format. \endif</summary>
    public E30IdentifierFormat Alarm { get; }

    private static E30IdentifierFormat Convert(GemIdentifierFormatPolicy policy, GemIdentifierFamily family)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return policy.GetFormat(family) switch
        {
            SecsItemFormat.UInt8 => E30IdentifierFormat.UInt8,
            SecsItemFormat.UInt16 => E30IdentifierFormat.UInt16,
            SecsItemFormat.UInt32 => E30IdentifierFormat.UInt32,
            SecsItemFormat.UInt64 => E30IdentifierFormat.UInt64,
            _ => throw new ArgumentException("The profile identifier policy is not an unsigned integer format.", nameof(policy))
        };
    }
}

/// <summary>\if KO wire 요청 결과의 종료 형태입니다. ACK 승인과 명령 완료를 혼동하지 않습니다. \endif \if EN Defines how a wire request ended without conflating ACK acceptance with command completion. \endif</summary>
public enum E30CallOutcome
{
    /// <summary>\if KO 정상 Secondary를 수신했습니다. \endif \if EN A normal secondary was received. \endif</summary>
    Completed,
    /// <summary>\if KO 원격 측이 Function 0으로 transaction을 종료했습니다. \endif \if EN The peer terminated the transaction with Function 0. \endif</summary>
    FunctionZero,
    /// <summary>\if KO transaction 제한 시간이 만료되었습니다. \endif \if EN The transaction timed out. \endif</summary>
    TimedOut,
    /// <summary>\if KO 호출이 취소되었습니다. \endif \if EN The call was canceled. \endif</summary>
    Canceled,
    /// <summary>\if KO 정상 Secondary의 구조가 동결 프로필과 맞지 않습니다. \endif \if EN The normal secondary did not match the frozen profile structure. \endif</summary>
    Malformed,
    /// <summary>\if KO 상태 또는 보고 정책 때문에 wire 메시지를 전송하지 않았습니다. \endif \if EN No wire message was sent because state or reporting policy suppressed it. \endif</summary>
    NotSent
}

/// <summary>\if KO typed 값, raw ACK, timeout·취소를 함께 노출하는 host 호출 결과입니다. \endif \if EN Represents a host call result that exposes its typed value, raw ACK, timeout, and cancellation outcome. \endif</summary>
/// <typeparam name="T">\if KO 정상 결과 값 형식입니다. \endif \if EN The normal result value type. \endif</typeparam>
public sealed record E30CallResult<T>
{
    private E30CallResult(E30CallOutcome outcome, T? value, byte? acknowledgement, string? error)
    {
        Outcome = outcome;
        Value = value;
        Acknowledgement = acknowledgement;
        Error = error;
    }

    /// <summary>\if KO 종료 형태입니다. \endif \if EN Gets the call outcome. \endif</summary>
    public E30CallOutcome Outcome { get; }
    /// <summary>\if KO 정상 typed 결과입니다. \endif \if EN Gets the normal typed value. \endif</summary>
    public T? Value { get; }
    /// <summary>\if KO 메시지에 ACK가 있을 때의 raw 한 바이트 값입니다. \endif \if EN Gets the raw one-byte ACK when the dialogue contains one. \endif</summary>
    public byte? Acknowledgement { get; }
    /// <summary>\if KO bounded 일반 오류 설명입니다. \endif \if EN Gets a bounded generalized error description. \endif</summary>
    public string? Error { get; }
    /// <summary>\if KO 정상 Secondary 수신 여부입니다. raw ACK 0 여부와는 독립적입니다. \endif \if EN Gets whether a normal secondary was received, independently of whether a raw ACK is zero. \endif</summary>
    public bool HasNormalSecondary => Outcome == E30CallOutcome.Completed;
    /// <summary>\if KO raw ACK가 0인지 나타냅니다. \endif \if EN Gets whether a present raw ACK is zero. \endif</summary>
    public bool IsAcknowledged => HasNormalSecondary && Acknowledgement == 0;

    /// <summary>\if KO 정상 결과를 만듭니다. \endif \if EN Creates a completed result. \endif</summary>
    public static E30CallResult<T> Complete(T value, byte? acknowledgement = null) => new(E30CallOutcome.Completed, value, acknowledgement, null);
    /// <summary>\if KO 값 없는 정상 ACK 결과를 만듭니다. \endif \if EN Creates a completed ACK result without a separate value. \endif</summary>
    public static E30CallResult<T> CompleteWithAck(byte acknowledgement, T? value = default) => new(E30CallOutcome.Completed, value, acknowledgement, null);
    /// <summary>\if KO 비정상 종료 결과를 만듭니다. \endif \if EN Creates a non-normal outcome. \endif</summary>
    public static E30CallResult<T> Ended(E30CallOutcome outcome, string? error = null)
    {
        if (outcome == E30CallOutcome.Completed) throw new ArgumentException("Use a completed factory for a normal result.", nameof(outcome));
        if (error is { Length: > 512 }) error = error[..512];
        return new(outcome, default, null, error);
    }
}

/// <summary>\if KO 상태 변수 이름 응답 한 항목입니다. \endif \if EN Represents one status-variable name response entry. \endif</summary>
public sealed record E30StatusVariableName(ulong Id, string Name, string Units);

/// <summary>\if KO peer의 MDLN과 SOFTREV입니다. \endif \if EN Represents a peer's MDLN and SOFTREV. \endif</summary>
public sealed record E30PeerIdentity(string ModelNumber, string SoftwareRevision);

/// <summary>\if KO 장비 상수 이름 응답 한 항목입니다. \endif \if EN Represents one equipment-constant name response entry. \endif</summary>
public sealed record E30EquipmentConstantName(ulong Id, string Name, SecsItem Minimum, SecsItem Maximum, SecsItem Default, string Units);

/// <summary>\if KO ordered VID 목록을 보존하는 report 정의입니다. \endif \if EN Represents a report definition preserving ordered VID values. \endif</summary>
public sealed class E30ReportDefinition
{
    private readonly ReadOnlyCollection<ulong> _variableIds;
    /// <summary>\if KO report 정의를 만듭니다. 중복 VID는 의도적으로 보존됩니다. \endif \if EN Creates a report definition; duplicate VIDs are intentionally preserved. \endif</summary>
    public E30ReportDefinition(ulong reportId, IEnumerable<ulong> variableIds)
    {
        if (reportId == 0) throw new ArgumentOutOfRangeException(nameof(reportId));
        ArgumentNullException.ThrowIfNull(variableIds);
        var values = variableIds.ToArray();
        if (values.Any(static value => value == 0)) throw new ArgumentException("VID values must be nonzero.", nameof(variableIds));
        ReportId = reportId;
        _variableIds = Array.AsReadOnly(values);
    }
    /// <summary>\if KO RPTID입니다. \endif \if EN Gets the RPTID. \endif</summary>
    public ulong ReportId { get; }
    /// <summary>\if KO 순서와 중복을 보존한 VID 목록입니다. \endif \if EN Gets the ordered VID list with duplicates preserved. \endif</summary>
    public IReadOnlyList<ulong> VariableIds => _variableIds;
}

/// <summary>\if KO ordered RPTID 목록을 보존하는 event link입니다. \endif \if EN Represents an event link preserving ordered RPTID values. \endif</summary>
public sealed class E30EventLink
{
    private readonly ReadOnlyCollection<ulong> _reportIds;
    /// <summary>\if KO event link를 만듭니다. \endif \if EN Creates an event link. \endif</summary>
    public E30EventLink(ulong collectionEventId, IEnumerable<ulong> reportIds)
    {
        if (collectionEventId == 0) throw new ArgumentOutOfRangeException(nameof(collectionEventId));
        ArgumentNullException.ThrowIfNull(reportIds);
        var values = reportIds.ToArray();
        if (values.Any(static value => value == 0) || values.Distinct().Count() != values.Length)
            throw new ArgumentException("RPTID values must be nonzero and unique within one event link.", nameof(reportIds));
        CollectionEventId = collectionEventId;
        _reportIds = Array.AsReadOnly(values);
    }
    /// <summary>\if KO CEID입니다. \endif \if EN Gets the CEID. \endif</summary>
    public ulong CollectionEventId { get; }
    /// <summary>\if KO 순서를 보존한 RPTID 목록입니다. \endif \if EN Gets the ordered RPTID list. \endif</summary>
    public IReadOnlyList<ulong> ReportIds => _reportIds;
}

/// <summary>\if KO 명령 매개변수입니다. \endif \if EN Represents a command parameter. \endif</summary>
public sealed record E30CommandParameter(string Name, SecsItem Value);

/// <summary>\if KO 거부된 명령 매개변수와 raw CPACK입니다. \endif \if EN Represents a rejected command parameter and its raw CPACK. \endif</summary>
public sealed record E30RejectedCommandParameter(string Name, byte Acknowledgement);

/// <summary>\if KO HCACK와 매개변수별 CPACK를 분리해 보존합니다. \endif \if EN Preserves HCACK separately from per-parameter CPACK values. \endif</summary>
public sealed class E30HostCommandAcknowledgement
{
    private readonly ReadOnlyCollection<E30RejectedCommandParameter> _rejectedParameters;
    /// <summary>\if KO 명령 ACK 결과를 만듭니다. \endif \if EN Creates a command acknowledgement. \endif</summary>
    public E30HostCommandAcknowledgement(byte acknowledgement, IEnumerable<E30RejectedCommandParameter>? rejectedParameters = null)
    {
        Acknowledgement = acknowledgement;
        _rejectedParameters = Array.AsReadOnly((rejectedParameters ?? []).ToArray());
    }
    /// <summary>\if KO raw HCACK입니다. \endif \if EN Gets the raw HCACK. \endif</summary>
    public byte Acknowledgement { get; }
    /// <summary>\if KO HCACK=3일 때의 매개변수별 raw CPACK 목록입니다. \endif \if EN Gets per-parameter raw CPACK values when HCACK is 3. \endif</summary>
    public IReadOnlyList<E30RejectedCommandParameter> RejectedParameters => _rejectedParameters;
}

/// <summary>\if KO 한 report의 ordered 값 목록입니다. \endif \if EN Represents the ordered values in one report. \endif</summary>
public sealed class E30ReportValues
{
    private readonly ReadOnlyCollection<SecsItem> _values;
    /// <summary>\if KO report 값 묶음을 만듭니다. \endif \if EN Creates a report-value group. \endif</summary>
    public E30ReportValues(ulong reportId, IEnumerable<SecsItem> values)
    {
        if (reportId == 0) throw new ArgumentOutOfRangeException(nameof(reportId));
        ArgumentNullException.ThrowIfNull(values);
        var copy = values.ToArray();
        if (copy.Any(static value => value is null)) throw new ArgumentException("Report values cannot contain null.", nameof(values));
        ReportId = reportId;
        _values = Array.AsReadOnly(copy);
    }
    /// <summary>\if KO RPTID입니다. \endif \if EN Gets the RPTID. \endif</summary>
    public ulong ReportId { get; }
    /// <summary>\if KO VID 정의 순서를 따르는 값 목록입니다. \endif \if EN Gets values in VID-definition order. \endif</summary>
    public IReadOnlyList<SecsItem> Values => _values;
}

/// <summary>\if KO S6F11/F16 event report의 typed 표현입니다. \endif \if EN Represents a typed S6F11/F16 event report. \endif</summary>
public sealed class E30EventReport
{
    private readonly ReadOnlyCollection<E30ReportValues> _reports;
    /// <summary>\if KO event report를 만듭니다. \endif \if EN Creates an event report. \endif</summary>
    public E30EventReport(ulong dataId, ulong collectionEventId, IEnumerable<E30ReportValues> reports)
    {
        if (collectionEventId == 0) throw new ArgumentOutOfRangeException(nameof(collectionEventId));
        ArgumentNullException.ThrowIfNull(reports);
        DataId = dataId;
        CollectionEventId = collectionEventId;
        _reports = Array.AsReadOnly(reports.ToArray());
    }
    /// <summary>\if KO DATAID입니다. \endif \if EN Gets the DATAID. \endif</summary>
    public ulong DataId { get; }
    /// <summary>\if KO CEID입니다. \endif \if EN Gets the CEID. \endif</summary>
    public ulong CollectionEventId { get; }
    /// <summary>\if KO 정의 순서의 report 목록입니다. \endif \if EN Gets reports in definition order. \endif</summary>
    public IReadOnlyList<E30ReportValues> Reports => _reports;
}

/// <summary>\if KO 알람 wire 데이터입니다. \endif \if EN Represents alarm wire data. \endif</summary>
public sealed record E30AlarmData(byte Code, ulong AlarmId, string Text);
