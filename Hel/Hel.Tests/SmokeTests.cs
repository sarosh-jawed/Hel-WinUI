using FluentAssertions;
using Xunit;

namespace Hel.Tests;

public class SmokeTests
{
    [Fact]
    public void Test_Project_Loads()
    {
        true.Should().BeTrue();
    }
}
