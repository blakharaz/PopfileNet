using Shouldly;
using Xunit;

namespace PopfileNet.Common.UnitTests;

public class EmailIdTests
{
    [Fact]
    public void TestToString()
    {
        var sut = new EmailId(validity: 5u, id: 7u);

        sut.ToString().ShouldBe("5:7");
    }

    [Fact]
    public void TestFromString()
    {
        var sut = new EmailId("5:7");

        sut.Validity.ShouldBe(5u);
        sut.Id.ShouldBe(7u);
    }
}