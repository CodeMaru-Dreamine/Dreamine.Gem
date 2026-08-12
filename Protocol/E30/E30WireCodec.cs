using System.Globalization;
using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.Gem.Protocol.E30;

/// <summary>\if KO 동결 E30 파생 구조와 맞지 않는 body를 나타냅니다. \endif \if EN Indicates that a body does not match a frozen E30-derived structure. \endif</summary>
public sealed class E30WireFormatException : FormatException
{
    /// <summary>\if KO 일반화된 구조 오류를 만듭니다. \endif \if EN Creates a generalized structural error. \endif</summary>
    public E30WireFormatException(string message) : base(message) { }
}

/// <summary>\if KO E5-0813 구조를 사용하는 E30-0611 파생 부분 프로필 v1 body codec입니다. \endif \if EN Encodes and decodes the E30-0611 derived subset profile v1 bodies using E5-0813 structures. \endif</summary>
public static class E30WireCodec
{
    /// <summary>\if KO ID 형식에 맞는 단일 무부호 정수 item을 만듭니다. \endif \if EN Creates one unsigned-integer item in the selected identifier format. \endif</summary>
    public static SecsItem Identifier(ulong value, E30IdentifierFormat format)
    {
        if (value == 0) throw new ArgumentOutOfRangeException(nameof(value));
        return CreateUnsigned(value, format);
    }

    /// <summary>\if KO zero를 허용하는 DATAID item을 만듭니다. \endif \if EN Creates a DATAID item, for which zero is permitted. \endif</summary>
    public static SecsItem DataIdentifier(ulong value, E30IdentifierFormat format) => CreateUnsigned(value, format);

    private static SecsItem CreateUnsigned(ulong value, E30IdentifierFormat format) => format switch
        {
            E30IdentifierFormat.UInt8 when value <= byte.MaxValue => new SecsUInt8Item((byte)value),
            E30IdentifierFormat.UInt16 when value <= ushort.MaxValue => new SecsUInt16Item((ushort)value),
            E30IdentifierFormat.UInt32 when value <= uint.MaxValue => new SecsUInt32Item((uint)value),
            E30IdentifierFormat.UInt64 => new SecsUInt64Item(value),
            _ => throw new ArgumentOutOfRangeException(nameof(value), "The identifier does not fit the configured wire format.")
        };

    /// <summary>\if KO 선택 형식의 ID vector를 만듭니다. 빈 vector는 'all' 의미에 사용할 수 있습니다. \endif \if EN Creates an ID vector in the selected format; an empty vector may represent 'all' where defined. \endif</summary>
    public static SecsItem IdentifierVector(IEnumerable<ulong> values, E30IdentifierFormat format)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copy = values.ToArray();
        if (copy.Any(static value => value == 0)) throw new ArgumentException("Identifiers must be nonzero.", nameof(values));
        return format switch
        {
            E30IdentifierFormat.UInt8 when copy.All(static value => value <= byte.MaxValue) => new SecsUInt8Item(copy.Select(static value => (byte)value).ToArray()),
            E30IdentifierFormat.UInt16 when copy.All(static value => value <= ushort.MaxValue) => new SecsUInt16Item(copy.Select(static value => (ushort)value).ToArray()),
            E30IdentifierFormat.UInt32 when copy.All(static value => value <= uint.MaxValue) => new SecsUInt32Item(copy.Select(static value => (uint)value).ToArray()),
            E30IdentifierFormat.UInt64 => new SecsUInt64Item(copy),
            _ => throw new ArgumentOutOfRangeException(nameof(values), "An identifier does not fit the configured wire format.")
        };
    }

    /// <summary>\if KO 선택 형식의 단일 ID를 엄격히 읽습니다. \endif \if EN Strictly reads one identifier in the selected format. \endif</summary>
    public static ulong ReadIdentifier(SecsItem? item, E30IdentifierFormat format) => ReadIdentifiers(item, format, requireSingle: true)[0];

    /// <summary>\if KO zero를 허용하는 단일 DATAID를 읽습니다. \endif \if EN Reads one DATAID, for which zero is permitted. \endif</summary>
    public static ulong ReadDataIdentifier(SecsItem? item, E30IdentifierFormat format)
    {
        var values = ReadIdentifiers(item, format, requireSingle: true, allowZero: true);
        return values[0];
    }

    /// <summary>\if KO 선택 형식의 ID vector를 엄격히 읽습니다. \endif \if EN Strictly reads an identifier vector in the selected format. \endif</summary>
    public static IReadOnlyList<ulong> ReadIdentifierVector(SecsItem? item, E30IdentifierFormat format) => ReadIdentifiers(item, format, requireSingle: false);

    /// <summary>\if KO 새 구현용 ID list 구조를 만듭니다. \endif \if EN Creates the list-of-ID structure used by new implementations. \endif</summary>
    public static SecsListItem IdentifierList(IEnumerable<ulong> values, E30IdentifierFormat format)
    {
        ArgumentNullException.ThrowIfNull(values);
        return new(values.Select(value => Identifier(value, format)).ToArray());
    }

    /// <summary>\if KO 새 구현용 ID list 구조를 엄격히 읽습니다. \endif \if EN Strictly reads the list-of-ID structure used by new implementations. \endif</summary>
    public static IReadOnlyList<ulong> ReadIdentifierList(SecsItem? item, E30IdentifierFormat format)
    {
        var list = RequireList(item, null, "identifier list");
        return list.Items.Select(value => ReadIdentifier(value, format)).ToArray();
    }

    /// <summary>\if KO MDLN/SOFTREV body를 만듭니다. \endif \if EN Creates an MDLN/SOFTREV body. \endif</summary>
    public static SecsListItem Identity(string modelNumber, string softwareRevision) => new(new SecsAsciiItem(modelNumber), new SecsAsciiItem(softwareRevision));

    /// <summary>\if KO MDLN/SOFTREV 또는 host zero-length list를 읽습니다. \endif \if EN Reads MDLN/SOFTREV or the host zero-length list. \endif</summary>
    public static (string ModelNumber, string SoftwareRevision)? ReadIdentity(SecsItem? item)
    {
        var list = RequireList(item, null, "identity");
        if (list.Count == 0) return null;
        if (list.Count != 2 || list.Items[0] is not SecsAsciiItem model || list.Items[1] is not SecsAsciiItem revision)
            throw Malformed("identity must be L,2 of ASCII MDLN and SOFTREV");
        return (model.Value, revision.Value);
    }

    /// <summary>\if KO COMMACK와 식별 데이터를 포함한 S1F14 body를 만듭니다. \endif \if EN Creates an S1F14 body containing COMMACK and identification data. \endif</summary>
    public static SecsListItem CommunicationAcknowledgement(byte acknowledgement, string modelNumber, string softwareRevision, bool hostResponse = false) =>
        new(new SecsBinaryItem(acknowledgement), hostResponse ? new SecsListItem() : Identity(modelNumber, softwareRevision));

    /// <summary>\if KO S1F14 body를 읽습니다. \endif \if EN Reads an S1F14 body. \endif</summary>
    public static (byte Acknowledgement, (string ModelNumber, string SoftwareRevision)? Identity) ReadCommunicationAcknowledgement(SecsItem? item)
    {
        var list = RequireList(item, 2, "S1F14");
        return (ReadAcknowledgement(list.Items[0]), ReadIdentity(list.Items[1]));
    }

    /// <summary>\if KO 한 바이트 Binary ACK를 읽습니다. \endif \if EN Reads a one-byte Binary ACK. \endif</summary>
    public static byte ReadAcknowledgement(SecsItem? item)
    {
        if (item is not SecsBinaryItem binary || binary.Count != 1) throw Malformed("acknowledgement must be B,1");
        return binary.Values.Span[0];
    }

    /// <summary>\if KO 한 바이트 Binary ACK를 만듭니다. \endif \if EN Creates a one-byte Binary ACK. \endif</summary>
    public static SecsBinaryItem Acknowledgement(byte value) => new(value);

    /// <summary>\if KO S1F12 상태 변수 이름 body를 만듭니다. \endif \if EN Creates an S1F12 status-variable name body. \endif</summary>
    public static SecsListItem StatusVariableNames(IEnumerable<E30StatusVariableName> values, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(values); ArgumentNullException.ThrowIfNull(policy);
        return new(values.Select(value => (SecsItem)new SecsListItem(
            Identifier(value.Id, policy.StatusVariable), new SecsAsciiItem(value.Name), new SecsAsciiItem(value.Units))).ToArray());
    }

    /// <summary>\if KO S1F12 상태 변수 이름 body를 읽습니다. \endif \if EN Reads an S1F12 status-variable name body. \endif</summary>
    public static IReadOnlyList<E30StatusVariableName> ReadStatusVariableNames(SecsItem? item, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return RequireList(item, null, "S1F12").Items.Select(entry =>
        {
            var fields = RequireList(entry, 3, "S1F12 entry");
            if (fields.Items[1] is not SecsAsciiItem name || fields.Items[2] is not SecsAsciiItem units) throw Malformed("S1F12 name and units must be ASCII");
            return new E30StatusVariableName(ReadIdentifier(fields.Items[0], policy.StatusVariable), name.Value, units.Value);
        }).ToArray();
    }

    /// <summary>\if KO 요청 순서의 value list를 만듭니다. \endif \if EN Creates a value list in request order. \endif</summary>
    public static SecsListItem Values(IEnumerable<SecsItem> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return new(values.ToArray());
    }

    /// <summary>\if KO value list를 읽습니다. \endif \if EN Reads a value list. \endif</summary>
    public static IReadOnlyList<SecsItem> ReadValues(SecsItem? item) => RequireList(item, null, "value list").Items.ToArray();

    /// <summary>\if KO S2F15 ECID/ECV body를 만듭니다. \endif \if EN Creates an S2F15 ECID/ECV body. \endif</summary>
    public static SecsListItem EquipmentConstantUpdates(IEnumerable<KeyValuePair<ulong, SecsItem>> values, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(values); ArgumentNullException.ThrowIfNull(policy);
        return new(values.Select(pair => (SecsItem)new SecsListItem(Identifier(pair.Key, policy.EquipmentConstant), pair.Value)).ToArray());
    }

    /// <summary>\if KO S2F15 ECID/ECV body를 stage용 불변 목록으로 읽습니다. \endif \if EN Reads an S2F15 ECID/ECV body into an immutable staging list. \endif</summary>
    public static IReadOnlyList<KeyValuePair<ulong, SecsItem>> ReadEquipmentConstantUpdates(SecsItem? item, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var result = RequireList(item, null, "S2F15").Items.Select(entry =>
        {
            var pair = RequireList(entry, 2, "S2F15 entry");
            return KeyValuePair.Create(ReadIdentifier(pair.Items[0], policy.EquipmentConstant), pair.Items[1]);
        }).ToArray();
        if (result.Select(static pair => pair.Key).Distinct().Count() != result.Length) throw Malformed("S2F15 contains duplicate ECID values");
        return result;
    }

    /// <summary>\if KO S2F30 장비 상수 이름 body를 만듭니다. \endif \if EN Creates an S2F30 equipment-constant name body. \endif</summary>
    public static SecsListItem EquipmentConstantNames(IEnumerable<E30EquipmentConstantName> values, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(values); ArgumentNullException.ThrowIfNull(policy);
        return new(values.Select(value => (SecsItem)new SecsListItem(
            Identifier(value.Id, policy.EquipmentConstant), new SecsAsciiItem(value.Name), value.Minimum,
            value.Maximum, value.Default, new SecsAsciiItem(value.Units))).ToArray());
    }

    /// <summary>\if KO S2F30 장비 상수 이름 body를 읽습니다. \endif \if EN Reads an S2F30 equipment-constant name body. \endif</summary>
    public static IReadOnlyList<E30EquipmentConstantName> ReadEquipmentConstantNames(SecsItem? item, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return RequireList(item, null, "S2F30").Items.Select(entry =>
        {
            var fields = RequireList(entry, 6, "S2F30 entry");
            if (fields.Items[1] is not SecsAsciiItem name || fields.Items[5] is not SecsAsciiItem units) throw Malformed("S2F30 name and units must be ASCII");
            return new E30EquipmentConstantName(ReadIdentifier(fields.Items[0], policy.EquipmentConstant), name.Value,
                fields.Items[2], fields.Items[3], fields.Items[4], units.Value);
        }).ToArray();
    }

    /// <summary>\if KO 12 또는 16자리 UTC TIME을 만듭니다. \endif \if EN Creates a 12- or 16-character UTC TIME. \endif</summary>
    public static SecsAsciiItem Time(DateTimeOffset value, bool fourDigitYear = true) =>
        new(value.UtcDateTime.ToString(fourDigitYear ? "yyyyMMddHHmmssff" : "yyMMddHHmmss", CultureInfo.InvariantCulture));

    /// <summary>\if KO 12 또는 16자리 TIME을 엄격히 UTC로 읽습니다. \endif \if EN Strictly reads a 12- or 16-character TIME as UTC. \endif</summary>
    public static DateTimeOffset ReadTime(SecsItem? item)
    {
        if (item is not SecsAsciiItem ascii) throw Malformed("TIME must be ASCII");
        var text = ascii.Value;
        try
        {
            if (text.Length == 16 && DateTime.TryParseExact(text, "yyyyMMddHHmmssff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var longValue))
                return new DateTimeOffset(DateTime.SpecifyKind(longValue, DateTimeKind.Utc));
            if (text.Length == 12)
            {
                if (!int.TryParse(text[..2], NumberStyles.None, CultureInfo.InvariantCulture, out var year)) throw Malformed("TIME contains a nonnumeric year");
                var century = year >= 70 ? 1900 : 2000;
                var expanded = (century + year).ToString("0000", CultureInfo.InvariantCulture) + text[2..];
                if (DateTime.TryParseExact(expanded, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var shortValue))
                    return new DateTimeOffset(DateTime.SpecifyKind(shortValue, DateTimeKind.Utc));
            }
        }
        catch (ArgumentOutOfRangeException) { }
        throw Malformed("TIME must be a valid A12 or A16 calendar value");
    }

    /// <summary>\if KO S2F33 report 정의 body를 만듭니다. \endif \if EN Creates an S2F33 report-definition body. \endif</summary>
    public static SecsListItem ReportDefinitions(ulong dataId, IEnumerable<E30ReportDefinition> reports, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(reports); ArgumentNullException.ThrowIfNull(policy);
        return new(DataIdentifier(dataId, policy.Data), new SecsListItem(reports.Select(report => (SecsItem)new SecsListItem(
            Identifier(report.ReportId, policy.Report), IdentifierList(report.VariableIds, policy.Variable))).ToArray()));
    }

    /// <summary>\if KO S2F33 report 정의를 stage용으로 읽고 report·VID 순서를 보존합니다. \endif \if EN Reads S2F33 report definitions for staging while preserving report and VID order. \endif</summary>
    public static (ulong DataId, IReadOnlyList<E30ReportDefinition> Reports) ReadReportDefinitions(SecsItem? item, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var body = RequireList(item, 2, "S2F33");
        var reports = RequireList(body.Items[1], null, "S2F33 reports").Items.Select(entry =>
        {
            var pair = RequireList(entry, 2, "S2F33 report");
            return new E30ReportDefinition(ReadIdentifier(pair.Items[0], policy.Report), ReadIdentifierList(pair.Items[1], policy.Variable));
        }).ToArray();
        if (reports.Select(static report => report.ReportId).Distinct().Count() != reports.Length) throw Malformed("S2F33 contains duplicate RPTID values");
        return (ReadDataIdentifier(body.Items[0], policy.Data), reports);
    }

    /// <summary>\if KO S2F35 event/report link body를 만듭니다. \endif \if EN Creates an S2F35 event/report-link body. \endif</summary>
    public static SecsListItem EventLinks(ulong dataId, IEnumerable<E30EventLink> links, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(links); ArgumentNullException.ThrowIfNull(policy);
        return new(DataIdentifier(dataId, policy.Data), new SecsListItem(links.Select(link => (SecsItem)new SecsListItem(
            Identifier(link.CollectionEventId, policy.CollectionEvent), IdentifierList(link.ReportIds, policy.Report))).ToArray()));
    }

    /// <summary>\if KO S2F35 event/report link를 stage용으로 읽습니다. \endif \if EN Reads S2F35 event/report links for staging. \endif</summary>
    public static (ulong DataId, IReadOnlyList<E30EventLink> Links) ReadEventLinks(SecsItem? item, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var body = RequireList(item, 2, "S2F35");
        var links = RequireList(body.Items[1], null, "S2F35 links").Items.Select(entry =>
        {
            var pair = RequireList(entry, 2, "S2F35 link");
            return new E30EventLink(ReadIdentifier(pair.Items[0], policy.CollectionEvent), ReadIdentifierList(pair.Items[1], policy.Report));
        }).ToArray();
        if (links.Select(static link => link.CollectionEventId).Distinct().Count() != links.Length) throw Malformed("S2F35 contains duplicate CEID values");
        return (ReadDataIdentifier(body.Items[0], policy.Data), links);
    }

    /// <summary>\if KO S2F37 enable/disable body를 만듭니다. 빈 CEID 목록은 전체 event를 의미합니다. \endif \if EN Creates an S2F37 enable/disable body; an empty CEID list means all events. \endif</summary>
    public static SecsListItem EventEnablement(bool enabled, IEnumerable<ulong> collectionEventIds, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(collectionEventIds); ArgumentNullException.ThrowIfNull(policy);
        return new(new SecsBooleanItem(enabled), IdentifierList(collectionEventIds, policy.CollectionEvent));
    }

    /// <summary>\if KO S2F37 enable/disable body를 stage용으로 읽습니다. \endif \if EN Reads an S2F37 enable/disable body for staging. \endif</summary>
    public static (bool Enabled, IReadOnlyList<ulong> CollectionEventIds) ReadEventEnablement(SecsItem? item, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var body = RequireList(item, 2, "S2F37");
        if (body.Items[0] is not SecsBooleanItem enabled || enabled.Count != 1) throw Malformed("CEED must be BOOLEAN,1");
        var ids = ReadIdentifierList(body.Items[1], policy.CollectionEvent);
        if (ids.Distinct().Count() != ids.Count) throw Malformed("S2F37 contains duplicate CEID values");
        return (enabled.Values.Span[0], ids);
    }

    /// <summary>\if KO S2F41 명령 body를 만듭니다. \endif \if EN Creates an S2F41 command body. \endif</summary>
    public static SecsListItem HostCommand(string name, IEnumerable<E30CommandParameter> parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name); ArgumentNullException.ThrowIfNull(parameters);
        var copy = parameters.ToArray();
        if (copy.Any(static value => string.IsNullOrWhiteSpace(value.Name) || value.Value is null) || copy.Select(static value => value.Name).Distinct(StringComparer.Ordinal).Count() != copy.Length)
            throw new ArgumentException("Command parameters must have unique nonempty names and values.", nameof(parameters));
        return new(new SecsAsciiItem(name), new SecsListItem(copy.Select(value => (SecsItem)new SecsListItem(new SecsAsciiItem(value.Name), value.Value)).ToArray()));
    }

    /// <summary>\if KO S2F41 명령 body를 stage용으로 읽습니다. \endif \if EN Reads an S2F41 command body for staging. \endif</summary>
    public static (string Name, IReadOnlyList<E30CommandParameter> Parameters) ReadHostCommand(SecsItem? item)
    {
        var body = RequireList(item, 2, "S2F41");
        if (body.Items[0] is not SecsAsciiItem name || string.IsNullOrWhiteSpace(name.Value)) throw Malformed("RCMD must be nonempty ASCII");
        var parameters = RequireList(body.Items[1], null, "S2F41 parameters").Items.Select(entry =>
        {
            var pair = RequireList(entry, 2, "S2F41 parameter");
            if (pair.Items[0] is not SecsAsciiItem parameter || string.IsNullOrWhiteSpace(parameter.Value)) throw Malformed("CPNAME must be nonempty ASCII");
            return new E30CommandParameter(parameter.Value, pair.Items[1]);
        }).ToArray();
        if (parameters.Select(static value => value.Name).Distinct(StringComparer.Ordinal).Count() != parameters.Length) throw Malformed("S2F41 contains duplicate CPNAME values");
        return (name.Value, parameters);
    }

    /// <summary>\if KO S2F42 HCACK/CPACK body를 만듭니다. \endif \if EN Creates an S2F42 HCACK/CPACK body. \endif</summary>
    public static SecsListItem HostCommandAcknowledgement(E30HostCommandAcknowledgement acknowledgement)
    {
        ArgumentNullException.ThrowIfNull(acknowledgement);
        return new(new SecsBinaryItem(acknowledgement.Acknowledgement), new SecsListItem(acknowledgement.RejectedParameters.Select(value =>
            (SecsItem)new SecsListItem(new SecsAsciiItem(value.Name), new SecsBinaryItem(value.Acknowledgement))).ToArray()));
    }

    /// <summary>\if KO S2F42 HCACK/CPACK body를 읽습니다. \endif \if EN Reads an S2F42 HCACK/CPACK body. \endif</summary>
    public static E30HostCommandAcknowledgement ReadHostCommandAcknowledgement(SecsItem? item)
    {
        var body = RequireList(item, 2, "S2F42");
        var parameters = RequireList(body.Items[1], null, "S2F42 parameters").Items.Select(entry =>
        {
            var pair = RequireList(entry, 2, "S2F42 parameter");
            if (pair.Items[0] is not SecsAsciiItem name) throw Malformed("S2F42 CPNAME must be ASCII");
            return new E30RejectedCommandParameter(name.Value, ReadAcknowledgement(pair.Items[1]));
        }).ToArray();
        return new(ReadAcknowledgement(body.Items[0]), parameters);
    }

    /// <summary>\if KO S5F1 alarm body를 만듭니다. \endif \if EN Creates an S5F1 alarm body. \endif</summary>
    public static SecsListItem Alarm(E30AlarmData alarm, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(alarm); ArgumentNullException.ThrowIfNull(policy);
        return new(new SecsBinaryItem(alarm.Code), Identifier(alarm.AlarmId, policy.Alarm), new SecsAsciiItem(alarm.Text));
    }

    /// <summary>\if KO S5F1 alarm body를 읽습니다. \endif \if EN Reads an S5F1 alarm body. \endif</summary>
    public static E30AlarmData ReadAlarm(SecsItem? item, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var body = RequireList(item, 3, "S5F1 alarm");
        if (body.Items[0] is not SecsBinaryItem code || code.Count != 1 || body.Items[2] is not SecsAsciiItem text) throw Malformed("S5F1 requires B,1 ALCD and ASCII ALTX");
        return new(code.Values.Span[0], ReadIdentifier(body.Items[1], policy.Alarm), text.Value);
    }

    /// <summary>\if KO S5F3 enable/disable body를 만듭니다. ALID null은 전체 alarm을 의미합니다. \endif \if EN Creates an S5F3 enable/disable body; a null ALID means all alarms. \endif</summary>
    public static SecsListItem AlarmEnablement(bool enabled, ulong? alarmId, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return new(new SecsBinaryItem(enabled ? (byte)0x80 : (byte)0x00),
            alarmId.HasValue ? Identifier(alarmId.Value, policy.Alarm) : IdentifierVector([], policy.Alarm));
    }

    /// <summary>\if KO S5F3 enable/disable body를 읽습니다. \endif \if EN Reads an S5F3 enable/disable body. \endif</summary>
    public static (bool Enabled, ulong? AlarmId) ReadAlarmEnablement(SecsItem? item, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var body = RequireList(item, 2, "S5F3");
        if (body.Items[0] is not SecsBinaryItem enabled || enabled.Count != 1) throw Malformed("ALED must be B,1");
        var enableCode = enabled.Values.Span[0];
        if (enableCode is not (0x00 or 0x80)) throw Malformed("ALED must be 0x00 or 0x80");
        var values = ReadIdentifierVector(body.Items[1], policy.Alarm);
        if (values.Count > 1) throw Malformed("S5F3 ALID must contain zero or one value");
        return (enableCode == 0x80, values.Count == 0 ? null : values[0]);
    }

    /// <summary>\if KO S5F6 alarm 목록 body를 만듭니다. \endif \if EN Creates an S5F6 alarm-list body. \endif</summary>
    public static SecsListItem Alarms(IEnumerable<E30AlarmData> alarms, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(alarms); ArgumentNullException.ThrowIfNull(policy);
        return new(alarms.Select(value => (SecsItem)Alarm(value, policy)).ToArray());
    }

    /// <summary>\if KO S5F6 alarm 목록 body를 읽습니다. \endif \if EN Reads an S5F6 alarm-list body. \endif</summary>
    public static IReadOnlyList<E30AlarmData> ReadAlarms(SecsItem? item, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return RequireList(item, null, "S5F6").Items.Select(value => ReadAlarm(value, policy)).ToArray();
    }

    /// <summary>\if KO S6F11/F16 event report body를 만듭니다. \endif \if EN Creates an S6F11/F16 event-report body. \endif</summary>
    public static SecsListItem EventReport(E30EventReport report, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(report); ArgumentNullException.ThrowIfNull(policy);
        return new(DataIdentifier(report.DataId, policy.Data), Identifier(report.CollectionEventId, policy.CollectionEvent),
            new SecsListItem(report.Reports.Select(group => (SecsItem)new SecsListItem(Identifier(group.ReportId, policy.Report), Values(group.Values))).ToArray()));
    }

    /// <summary>\if KO S6F11/F16 event report body를 읽고 report/value 순서를 보존합니다. \endif \if EN Reads an S6F11/F16 event-report body while preserving report and value order. \endif</summary>
    public static E30EventReport ReadEventReport(SecsItem? item, E30IdentifierPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var body = RequireList(item, 3, "S6F11/F16");
        var reports = RequireList(body.Items[2], null, "event reports").Items.Select(entry =>
        {
            var pair = RequireList(entry, 2, "event report");
            return new E30ReportValues(ReadIdentifier(pair.Items[0], policy.Report), ReadValues(pair.Items[1]));
        }).ToArray();
        return new(ReadDataIdentifier(body.Items[0], policy.Data), ReadIdentifier(body.Items[1], policy.CollectionEvent), reports);
    }

    /// <summary>\if KO offending message의 10-byte HSMS data header를 MHEAD/SHEAD로 만듭니다. \endif \if EN Creates the 10-byte HSMS data header of an offending message for MHEAD/SHEAD. \endif</summary>
    public static SecsBinaryItem MessageHeader(SecsMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var id = message.SessionId.Value;
        var system = message.SystemBytes.Value;
        return new SecsBinaryItem(
            (byte)(id >> 8), (byte)id,
            (byte)(message.Stream.Value | (message.ReplyExpected ? 0x80 : 0)), message.Function.Value,
            0, 0,
            (byte)(system >> 24), (byte)(system >> 16), (byte)(system >> 8), (byte)system);
    }

    /// <summary>\if KO 동일 Session ID·Stream·System Bytes를 보존한 expert Function 0을 만듭니다. \endif \if EN Creates an expert Function 0 preserving the same session ID, stream, and System Bytes. \endif</summary>
    public static SecsMessage FunctionZero(SecsMessage primary)
    {
        ArgumentNullException.ThrowIfNull(primary);
        return new(primary.SessionId, primary.Stream, new SecsFunction(0), false, primary.SystemBytes);
    }

    /// <summary>\if KO offending header와 System Bytes를 연결한 expert S9 message를 만듭니다. \endif \if EN Creates an expert S9 message correlated with the offending header and System Bytes. \endif</summary>
    public static SecsMessage StreamNine(SecsMessage offending, byte function)
    {
        ArgumentNullException.ThrowIfNull(offending);
        if (function is not (1 or 3 or 5 or 7 or 9 or 11)) throw new ArgumentOutOfRangeException(nameof(function));
        return new(offending.SessionId, new SecsStream(9), new SecsFunction(function), false, offending.SystemBytes, MessageHeader(offending));
    }

    private static IReadOnlyList<ulong> ReadIdentifiers(SecsItem? item, E30IdentifierFormat format, bool requireSingle, bool allowZero = false)
    {
        ulong[] values = (item, format) switch
        {
            (SecsUInt8Item value, E30IdentifierFormat.UInt8) => value.Values.ToArray().Select(static item => (ulong)item).ToArray(),
            (SecsUInt16Item value, E30IdentifierFormat.UInt16) => value.Values.ToArray().Select(static item => (ulong)item).ToArray(),
            (SecsUInt32Item value, E30IdentifierFormat.UInt32) => value.Values.ToArray().Select(static item => (ulong)item).ToArray(),
            (SecsUInt64Item value, E30IdentifierFormat.UInt64) => value.Values.ToArray(),
            _ => throw Malformed("identifier format does not match the configured profile")
        };
        if (requireSingle && values.Length != 1) throw Malformed("identifier must contain exactly one value");
        if (!allowZero && values.Any(static value => value == 0)) throw Malformed("identifier values must be nonzero");
        return values;
    }

    private static SecsListItem RequireList(SecsItem? item, int? count, string name)
    {
        if (item is not SecsListItem list || (count.HasValue && list.Count != count.Value))
            throw Malformed(count.HasValue ? $"{name} must be L,{count.Value}" : $"{name} must be a list");
        return list;
    }

    private static E30WireFormatException Malformed(string message) => new(message);
}
