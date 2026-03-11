using FluentAssertions;
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

        dataSet.Data.Should().HaveCount(1);
    }

    [Fact]
    public void AddMail_MultipleEmails_AddsAll()
    {
        var dataSet = new EmailClassificationDataSet();
        
        dataSet.AddMail(CreateSampleEmail("Subject 1", "Body 1"), "spam");
        dataSet.AddMail(CreateSampleEmail("Subject 2", "Body 2"), "ham");
        dataSet.AddMail(CreateSampleEmail("Subject 3", "Body 3"), "spam");

        dataSet.Data.Should().HaveCount(3);
    }

    [Fact]
    public void Data_GetEnumerable_ReturnsCorrectData()
    {
        var dataSet = new EmailClassificationDataSet();
        
        dataSet.AddMail(CreateSampleEmail("Newsletter", "Content"), "spam");
        dataSet.AddMail(CreateSampleEmail("Meeting", "Content"), "ham");

        var result = dataSet.Data.ToList();

        result.Should().HaveCount(2);
        result[0].Subject.Should().Be("Newsletter");
        result[0].Label.Should().Be("spam");
        result[1].Subject.Should().Be("Meeting");
        result[1].Label.Should().Be("ham");
    }

    [Fact]
    public void AddMail_SetsCorrectFields()
    {
        var dataSet = new EmailClassificationDataSet();
        var email = CreateSampleEmail("Test Subject", "Test Body Content");

        dataSet.AddMail(email, "spam");

        var result = dataSet.Data.First();
        result.Subject.Should().Be("Test Subject");
        result.Content.Should().Be("Test Body Content");
        result.Label.Should().Be("spam");
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
