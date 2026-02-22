using MailKit;
using MimeKit;
using PopfileNet.Common;

namespace PopfileNet.Imap.Models;

public static class Mapping
{
    public static Email ConvertToEmail(UniqueId uid, MimeMessage message)
    {
        var messageId = message.MessageId ?? Guid.NewGuid().ToString();

        return new Email
        {
            UniqueId = MapToEmailId(uid),
            Id = messageId,
            Subject = message.Subject ?? string.Empty,
            FromAddress = message.From.FirstOrDefault()?.ToString() ?? string.Empty,
            ToAddresses = string.Join(";", message.To.Select(static x => x.ToString())),
            Body = message.TextBody ?? message.HtmlBody ?? string.Empty,
            ReceivedDate = message.Date.DateTime,
            IsHtml = !string.IsNullOrEmpty(message.HtmlBody),
            Headers = message.Headers.Select(h => ConvertHeader(h, messageId)).ToList()
        };
    }

    private static MailHeader ConvertHeader(Header header, string messageId)
    {
        return new MailHeader
        {
            EmailId = messageId,
            Name = header.Id.ToString(),
            Value = header.Value
        };
    }

    public static EmailId MapToEmailId(UniqueId uid)
    {
        return new EmailId(Validity: uid.Validity, Id: uid.Id);
    }

    public static IEnumerable<UniqueId> MapToUniqueIds(IEnumerable<EmailId> ids)
    {
        return ids.Select(id => new UniqueId(id.Validity, id.Id));
    }
}