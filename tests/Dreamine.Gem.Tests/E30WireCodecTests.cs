using Dreamine.Gem.Protocol.E30;
using Dreamine.Gem.Abstractions.Model;
using Dreamine.Secs.Abstractions.Model;
using Xunit;

namespace Dreamine.Gem.Tests;

public sealed class E30WireCodecTests
{
    [Fact]
    public void FrozenManifestUsesExactDerivedNameAndExplicitBlockedEntries()
    {
        Assert.Equal("E30-0611 derived subset profile v1", E30DerivedSubsetManifest.ProfileName);
        Assert.Equal(20, E30DerivedSubsetManifest.IncludedDialogues.Count);
        Assert.Contains(E30DerivedSubsetManifest.Capabilities, value => value.Name == "S6F19/20" && value.Disposition == E30CapabilityDisposition.BlockedStandard);
        Assert.Contains(E30DerivedSubsetManifest.Capabilities, value => value.Name == "S2F35 empty-list unlink/delete variants" && value.Disposition == E30CapabilityDisposition.BlockedStandard);
        Assert.Contains(E30DerivedSubsetManifest.Capabilities, value => value.Name == "S2F39/40 and S6F5/6" && value.Disposition == E30CapabilityDisposition.IntentionallyExcluded);
    }

    [Fact]
    public void HandBuiltS2F33FixturePreservesReportVidOrderAndDuplicateVidsAcrossReports()
    {
        var fixture = new SecsListItem(
            new SecsUInt32Item(77),
            new SecsListItem(
                new SecsListItem(new SecsUInt32Item(12), new SecsListItem(new SecsUInt32Item(9), new SecsUInt32Item(3))),
                new SecsListItem(new SecsUInt32Item(4), new SecsListItem(new SecsUInt32Item(9), new SecsUInt32Item(8)))));

        var parsed = E30WireCodec.ReadReportDefinitions(fixture, new E30IdentifierPolicy());

        Assert.Equal(77ul, parsed.DataId);
        Assert.Equal([12ul, 4ul], parsed.Reports.Select(static value => value.ReportId));
        Assert.Equal([9ul, 3ul], parsed.Reports[0].VariableIds);
        Assert.Equal([9ul, 8ul], parsed.Reports[1].VariableIds);
    }

    [Fact]
    public void S2F33RejectsWrongConfiguredIdentifierFormat()
    {
        var malformed = new SecsListItem(new SecsUInt16Item(1), new SecsListItem());

        Assert.Throws<E30WireFormatException>(() => E30WireCodec.ReadReportDefinitions(malformed, new E30IdentifierPolicy()));
    }

    [Theory]
    [InlineData("2024022901020399", 2024, 2, 29)]
    [InlineData("700101010203", 1970, 1, 1)]
    [InlineData("690101010203", 2069, 1, 1)]
    public void TimeParserAcceptsA12AndA16WithDeterministicCentury(string text, int year, int month, int day)
    {
        var value = E30WireCodec.ReadTime(new SecsAsciiItem(text));

        Assert.Equal(year, value.Year);
        Assert.Equal(month, value.Month);
        Assert.Equal(day, value.Day);
        Assert.Equal(TimeSpan.Zero, value.Offset);
    }

    [Theory]
    [InlineData("2023022901020300")]
    [InlineData("2024130101020300")]
    [InlineData("20240101010203")]
    [InlineData("not-a-time!!")]
    public void TimeParserRejectsMalformedWithoutCoercion(string text) =>
        Assert.Throws<E30WireFormatException>(() => E30WireCodec.ReadTime(new SecsAsciiItem(text)));

    [Fact]
    public void ExpertF0AndS9PreserveOriginalCorrelationAndHeader()
    {
        var primary = new SecsMessage(new SecsSessionId(0x1234), new SecsStream(2), new SecsFunction(15), true, new SecsSystemBytes(0x89ABCDEF), new SecsListItem());

        var functionZero = E30WireCodec.FunctionZero(primary);
        var streamNine = E30WireCodec.StreamNine(primary, 7);
        var header = Assert.IsType<SecsBinaryItem>(streamNine.Item).Values.ToArray();

        Assert.Equal((byte)0, functionZero.Function.Value);
        Assert.Equal(primary.SessionId, functionZero.SessionId);
        Assert.Equal(primary.Stream, functionZero.Stream);
        Assert.Equal(primary.SystemBytes, functionZero.SystemBytes);
        Assert.Equal(primary.SystemBytes, streamNine.SystemBytes);
        Assert.Equal(new byte[] { 0x12, 0x34, 0x82, 0x0F, 0x00, 0x00, 0x89, 0xAB, 0xCD, 0xEF }, header);
    }

    [Fact]
    public void HandBuiltS2F41FixtureRejectsDuplicateParameterNames()
    {
        var fixture = new SecsListItem(
            new SecsAsciiItem("START"),
            new SecsListItem(
                new SecsListItem(new SecsAsciiItem("PPID"), new SecsAsciiItem("A")),
                new SecsListItem(new SecsAsciiItem("PPID"), new SecsAsciiItem("B"))));

        Assert.Throws<E30WireFormatException>(() => E30WireCodec.ReadHostCommand(fixture));
    }

    [Fact]
    public void S5F3UsesExactBinaryAledAndRejectsBooleanLookalike()
    {
        var enabled = E30WireCodec.AlarmEnablement(true, 7, new E30IdentifierPolicy());
        var disabled = E30WireCodec.AlarmEnablement(false, null, new E30IdentifierPolicy());

        Assert.Equal((byte)0x80, Assert.IsType<SecsBinaryItem>(enabled.Items[0]).Values.Span[0]);
        Assert.Equal((byte)0x00, Assert.IsType<SecsBinaryItem>(disabled.Items[0]).Values.Span[0]);
        Assert.True(E30WireCodec.ReadAlarmEnablement(enabled, new E30IdentifierPolicy()).Enabled);
        Assert.False(E30WireCodec.ReadAlarmEnablement(disabled, new E30IdentifierPolicy()).Enabled);
        Assert.Throws<E30WireFormatException>(() => E30WireCodec.ReadAlarmEnablement(
            new SecsListItem(new SecsBooleanItem(true), new SecsUInt32Item()), new E30IdentifierPolicy()));
        Assert.Throws<E30WireFormatException>(() => E30WireCodec.ReadAlarmEnablement(
            new SecsListItem(new SecsBinaryItem(1), new SecsUInt32Item()), new E30IdentifierPolicy()));
    }

    [Fact]
    public void DataIdFormatIsIndependentFromVidFormat()
    {
        var profilePolicy = new GemIdentifierFormatPolicy(
        [
            new(GemIdentifierFamily.Variable, SecsItemFormat.UInt8),
            new(GemIdentifierFamily.DataIdentifier, SecsItemFormat.UInt16)
        ]);

        var wirePolicy = new E30IdentifierPolicy(profilePolicy);
        var body = E30WireCodec.ReportDefinitions(300, [new E30ReportDefinition(1, [1])], wirePolicy);
        var reports = Assert.IsType<SecsListItem>(body.Items[1]);
        var report = Assert.IsType<SecsListItem>(reports.Items[0]);
        var vids = Assert.IsType<SecsListItem>(report.Items[1]);

        Assert.IsType<SecsUInt16Item>(body.Items[0]);
        Assert.IsType<SecsUInt8Item>(vids.Items[0]);
    }
}
