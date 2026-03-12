using Shouldly;
using PopfileNet.Common;
using Xunit;

namespace PopfileNet.Classifier.UnitTests;

public class EmailClassificationDataSetTests
{
    [Fact]
    public void AddMail_ValidEmail_AddsToData()
    {
        var dataSet = new EmailClassificationDataSet();
        var email = CreateSampleEmail("Test Subject", "Test Body");

        dataSet.AddMail(email, "spam");

        dataSet.Data.Count().ShouldBe(1);
    }

    [Fact]
    public void AddMail_MultipleEmails_AddsAll()
    {
        var dataSet = new EmailClassificationDataSet();
        
        dataSet.AddMail(CreateSampleEmail("Subject 1", "Body 1"), "spam");
        dataSet.AddMail(CreateSampleEmail("Subject 2", "Body 2"), "ham");
        dataSet.AddMail(CreateSampleEmail("Subject 3", "Body 3"), "spam");

        dataSet.Data.Count().ShouldBe(3);
    }

    [Fact]
    public void Data_GetEnumerable_ReturnsCorrectData()
    {
        var dataSet = new EmailClassificationDataSet();
        
        dataSet.AddMail(CreateSampleEmail("Newsletter", "Content"), "spam");
        dataSet.AddMail(CreateSampleEmail("Meeting", "Content"), "ham");

        var result = dataSet.Data.ToList();

        result.Count.ShouldBe(2);
        result[0].Subject.ShouldBe("Newsletter");
        result[0].Label.ShouldBe("spam");
        result[1].Subject.ShouldBe("Meeting");
        result[1].Label.ShouldBe("ham");
    }

    [Fact]
    public void AddMail_SetsCorrectFields()
    {
        var dataSet = new EmailClassificationDataSet();
        var email = CreateSampleEmail("Test Subject", "Test Body Content");

        dataSet.AddMail(email, "spam");

        var result = dataSet.Data.First();
        result.Subject.ShouldBe("Test Subject");
        result.Content.ShouldBe("Test Body Content");
        result.Label.ShouldBe("spam");
    }

    private static Email CreateSampleEmail(string subject, string body)
    {
        return new Email
        {
            Id = Guid.NewGuid().ToString(),
            Subject = subject,
            Body = body,
            FromAddress = "test@example.com",
            ToAddresses = "recipient@example.com",
            ReceivedDate = DateTime.Now
        };
    }
}
