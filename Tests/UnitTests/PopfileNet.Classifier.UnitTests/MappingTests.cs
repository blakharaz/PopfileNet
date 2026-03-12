using Shouldly;
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

        result.ShouldNotBeNull();
        result.Subject.ShouldBe("Test Subject");
        result.Content.ShouldBe("Test Body Content");
        result.Label.ShouldBe("spam");
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

        result.ShouldNotBeNull();
        result.Subject.ShouldBe("Test Subject");
        result.Content.ShouldBe("Test Body Content");
    }

    [Fact]
    public void MapToTrainingData_NullEmail_ThrowsArgumentNullException()
    {
        const string label = "spam";

        var action = () => PopfileNet.Classifier.Mapping.MapToTrainingData(null!, label);

        action.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void MapToInput_NullEmail_ThrowsArgumentNullException()
    {
        var action = () => PopfileNet.Classifier.Mapping.MapToInput(null!);

        action.ShouldThrow<ArgumentNullException>();
    }
}
