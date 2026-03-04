using FluentAssertions;
using MailKit;
using MimeKit;
using PopfileNet.Common;
using Xunit;

namespace PopfileNet.Imap.Tests;

public class MappingTests
{
    [Fact]
    public void ConvertToEmail_ValidMimeMessage_ReturnsEmail()
    {
        var message = CreateMimeMessage("Test Subject", "Test Body", "from@example.com");
        var uid = new UniqueId(1, 1);

        var result = PopfileNet.Imap.Models.Mapping.ConvertToEmail(uid, message);

        result.Should().NotBeNull();
        result.Subject.Should().Be("Test Subject");
        result.Body.Should().Be("Test Body");
        result.FromAddress.Should().Contain("from@example.com");
    }

    [Fact]
    public void ConvertToEmail_NullSubject_UsesEmptyString()
    {
        var message = new MimeMessage();
        message.Body = new TextPart("plain") { Text = "Test Body" };
        var uid = new UniqueId(1, 1);

        var result = PopfileNet.Imap.Models.Mapping.ConvertToEmail(uid, message);

        result.Subject.Should().BeEmpty();
    }

    [Fact]
    public void ConvertToEmail_NullFromAddress_UsesEmptyString()
    {
        var message = new MimeMessage();
        message.Subject = "Test Subject";
        message.Body = new TextPart("plain") { Text = "Test Body" };
        var uid = new UniqueId(1, 1);

        var result = PopfileNet.Imap.Models.Mapping.ConvertToEmail(uid, message);

        result.FromAddress.Should().BeEmpty();
    }

    [Fact]
    public void ConvertToEmail_NullBody_UsesHtmlBody()
    {
        var message = new MimeMessage();
        message.Subject = "Test Subject";
        message.Body = new TextPart("html") { Text = "<p>Test HTML</p>" };
        var uid = new UniqueId(1, 1);

        var result = PopfileNet.Imap.Models.Mapping.ConvertToEmail(uid, message);

        result.Body.Should().Be("<p>Test HTML</p>");
        result.IsHtml.Should().BeTrue();
    }

    [Fact]
    public void ConvertToEmail_BothBodiesNull_UsesEmptyString()
    {
        var message = new MimeMessage();
        message.Subject = "Test Subject";
        var uid = new UniqueId(1, 1);

        var result = PopfileNet.Imap.Models.Mapping.ConvertToEmail(uid, message);

        result.Body.Should().BeEmpty();
    }

    [Fact]
    public void MapToEmailId_ValidUid_ReturnsEmailId()
    {
        var uid = new UniqueId(1, 123);

        var result = PopfileNet.Imap.Models.Mapping.MapToEmailId(uid);

        result.Validity.Should().Be(1u);
        result.Id.Should().Be(123u);
    }

    [Fact]
    public void MapToUniqueIds_ValidEmailIds_ReturnsUniqueIds()
    {
        var emailIds = new[]
        {
            new EmailId(Validity: 1, Id: 1),
            new EmailId(Validity: 1, Id: 2),
            new EmailId(Validity: 2, Id: 3)
        };

        var result = PopfileNet.Imap.Models.Mapping.MapToUniqueIds(emailIds).ToList();

        result.Should().HaveCount(3);
        result[0].Should().Be(new UniqueId(1, 1));
        result[1].Should().Be(new UniqueId(1, 2));
        result[2].Should().Be(new UniqueId(2, 3));
    }

    [Fact]
    public void ConvertToEmail_WithHeaders_PopulatesHeaders()
    {
        var message = new MimeMessage();
        message.Subject = "Test Subject";
        message.Body = new TextPart("plain") { Text = "Test Body" };
        message.Headers.Add(HeaderId.MessageId, "<test@example.com>");
        message.Headers.Add(HeaderId.Date, "Mon, 1 Jan 2024 12:00:00 +0000");
        var uid = new UniqueId(1, 1);

        var result = PopfileNet.Imap.Models.Mapping.ConvertToEmail(uid, message);

        result.Headers.Should().NotBeEmpty();
    }

    private static MimeMessage CreateMimeMessage(string subject, string body, string from)
    {
        var message = new MimeMessage();
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse("to@example.com"));
        return message;
    }
}
