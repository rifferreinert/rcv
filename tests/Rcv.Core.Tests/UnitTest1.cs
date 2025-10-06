namespace Rcv.Core.Tests;

/// <summary>
/// Basic smoke tests to verify library loads correctly
/// </summary>
public class SmokeTests
{
    [Fact]
    public void LibraryLoads()
    {
        // Simple smoke test to verify the assembly loads
        var assembly = typeof(Class1).Assembly;
        Assert.NotNull(assembly);
    }
}
