using Xunit;

namespace Dreamine.Gem.Tests;

public sealed class AssemblyBoundaryTests
{
    [Fact]
    public void MarkerBelongsToExpectedAssembly() =>
        Assert.Equal("Dreamine.Gem", typeof(GemAssemblyMarker).Assembly.GetName().Name);
}
