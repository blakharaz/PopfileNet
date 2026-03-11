using FluentAssertions;
using Xunit;

namespace PopfileNet.Common.UnitTests;

public class EmailIdTests
{
    [Fact]
    public void TestToString()
    {
        var sut = new EmailId(validity: 5u, id: 7u);

        sut.ToString().Should().Be("5:7");
    }

    [Fact]
    public void TestFromString()
    {
        var sut = new EmailId("5:7");

        sut.Validity.Should().Be(5u);
        sut.Id.Should().Be(7u);
    }
}