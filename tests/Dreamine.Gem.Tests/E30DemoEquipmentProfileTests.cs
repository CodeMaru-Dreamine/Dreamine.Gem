using Dreamine.Gem.Abstractions.Model;
using Dreamine.Gem.Demo;
using Dreamine.Gem.Profiles;
using Dreamine.Gem.Protocol.E30;
using Dreamine.Secs.Abstractions.Model;
using Xunit;

namespace Dreamine.Gem.Tests;

public sealed class E30DemoEquipmentProfileTests
{
    [Fact]
    public void PublicDemoProfileIsSmallFrozenAndUsesOnlyTheDeclaredDerivedSubset()
    {
        var first = E30DemoEquipmentProfile.Create();
        var second = E30DemoEquipmentProfile.Create();

        Assert.Same(first, second);
        Assert.Equal(E30DemoEquipmentProfile.ProfileName, first.Name);
        Assert.Equal(E30DerivedSubsetManifest.ProfileName, first.TargetRevision);
        Assert.Equal("DREAMINE-DEMO", first.Identity.ModelNumber);
        Assert.Equal("1.0", first.Identity.SoftwareRevision);
        Assert.Equal([1ul, 2ul], first.Variables.Select(static value => value.Definition.Id));
        Assert.Equal([100ul], first.EquipmentConstants.Select(static value => value.Definition.Id));
        Assert.Equal([10ul], first.Reports.Select(static value => value.Id));
        Assert.Equal([1000ul, 1001ul], first.CollectionEvents.Select(static value => value.Id));
        Assert.Equal([2000ul], first.Alarms.Select(static value => value.Id));
        Assert.Equal(["START"], first.RemoteCommands.Select(static value => value.Definition.Name));
        Assert.Equal(GemEquipmentProfile.V1Capabilities, first.Capabilities);

        var command = Assert.Single(first.RemoteCommands);
        var parameter = Assert.Single(command.Definition.Parameters);
        Assert.Equal("LOT", parameter.Name);
        Assert.Equal(SecsItemFormat.Ascii, parameter.Format);
        Assert.True(parameter.Required);
        Assert.True(parameter.Validator!(new SecsAsciiItem("DEMO")));
        Assert.False(parameter.Validator(new SecsAsciiItem(string.Empty)));
    }
}
