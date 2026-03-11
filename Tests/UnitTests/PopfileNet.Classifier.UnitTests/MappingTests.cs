using FluentAssertions;
using PopfileNet.Common;
using Xunit;

namespace PopfileNet.Classifier.UnitTests;

public class MappingTests
{
    [Fact]
    public void MapToTrainingData_ValidEmail_ReturnsCorrectData()
    {
        var email = new Email
        {
            Subject = "Test Subject",
            Body = "Test Body Content"
        };
        const string label = "spam";

        var result = PopfileNet.Classifier.Mapping.MapToTrainingData(email, label);

        result.Should().NotBeNull();
        result.Subject.Should().Be("Test Subject");
        result.Content.Should().Be("Test Body Content");
        result.Label.Should().Be("spam");
    }

    [Fact]
    public void MapToInput_ValidEmail_ReturnsCorrectInput()
    {
        var email = new Email
        {
            Subject = "Test Subject",
            Body = "Test Body Content"
        };

        var result = PopfileNet.Classifier.Mapping.MapToInput(email);

        result.Should().NotBeNull();
        result.Subject.Should().Be("Test Subject");
        result.Content.Should().Be("Test Body Content");
    }

    [Fact]
    public void MapToTrainingData_NullEmail_ThrowsArgumentNullException()
    {
        const string label = "spam";

        var action = () => PopfileNet.Classifier.Mapping.MapToTrainingData(null!, label);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MapToInput_NullEmail_ThrowsArgumentNullException()
    {
        var action = () => PopfileNet.Classifier.Mapping.MapToInput(null!);

        action.Should().Throw<ArgumentNullException>();
    }
}
