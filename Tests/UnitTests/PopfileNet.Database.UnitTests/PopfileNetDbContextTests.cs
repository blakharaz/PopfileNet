using Shouldly;
using Microsoft.EntityFrameworkCore;
using PopfileNet.Common;
using Xunit;

namespace PopfileNet.Database.UnitTests;

public class PopfileNetDbContextTests
{
    private PopfileNetDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PopfileNetDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PopfileNetDbContext(options);
    }

    [Fact]
    public async Task Email_Insert_SavesSuccessfully()
    {
        await using var context = CreateInMemoryContext();

        var email = new Email
        {
            Id = Guid.NewGuid().ToString(),
            Subject = "Test Subject",
            Body = "Test Body",
            FromAddress = "from@example.com",
            ToAddresses = "to@example.com",
            ReceivedDate = DateTime.UtcNow
        };

        context.Emails.Add(email);
        await context.SaveChangesAsync();

        context.Emails.Count().ShouldBe(1);
    }

    [Fact]
    public async Task Email_QueryById_ReturnsCorrectEmail()
    {
        await using var context = CreateInMemoryContext();

        var email = new Email
        {
            Id = "unique-email-id",
            Subject = "Test Subject",
            Body = "Test Body",
            FromAddress = "from@example.com",
            ToAddresses = "to@example.com",
            ReceivedDate = DateTime.UtcNow
        };

        context.Emails.Add(email);
        await context.SaveChangesAsync();

        var result = await context.Emails.FindAsync("unique-email-id");

        result.ShouldNotBeNull();
        result!.Subject.ShouldBe("Test Subject");
    }

    [Fact]
    public async Task Email_WithFolder_ForeignKeySet()
    {
        await using var context = CreateInMemoryContext();

        var folder = new MailFolder
        {
            Id = Guid.NewGuid().ToString(),
            Name = "TestFolder"
        };
        context.MailFolders.Add(folder);
        await context.SaveChangesAsync();

        var email = new Email
        {
            Id = Guid.NewGuid().ToString(),
            Subject = "Test Subject",
            Body = "Test Body",
            FromAddress = "from@example.com",
            ToAddresses = "to@example.com",
            ReceivedDate = DateTime.UtcNow,
            Folder = folder.Id
        };
        context.Emails.Add(email);
        await context.SaveChangesAsync();

        var result = await context.Emails
            .Include(e => e.FolderNavigation)
            .FirstOrDefaultAsync(e => e.Id == email.Id);

        result.ShouldNotBeNull();
        result!.FolderNavigation.ShouldNotBeNull();
        result.FolderNavigation!.Name.ShouldBe("TestFolder");
    }

    [Fact]
    public async Task Email_WithHeaders_CascadeDelete()
    {
        await using var context = CreateInMemoryContext();

        var emailId = "email-with-headers";
        var email = new Email
        {
            Id = emailId,
            Subject = "Test Subject",
            Body = "Test Body",
            FromAddress = "from@example.com",
            ToAddresses = "to@example.com",
            ReceivedDate = DateTime.UtcNow,
            Headers =
            [
                new() { EmailId = emailId, Name = "Header1", Value = "Value1" },
                new() { EmailId = emailId, Name = "Header2", Value = "Value2" }
            ]
        };
        context.Emails.Add(email);
        await context.SaveChangesAsync();

        context.Emails.Remove(email);
        await context.SaveChangesAsync();

        var result = await context.Emails.FirstOrDefaultAsync(e => e.Id == emailId);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task Bucket_Insert_SavesSuccessfully()
    {
        await using var context = CreateInMemoryContext();

        var bucket = new Bucket
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Spam",
            Description = "Spam emails"
        };

        context.Buckets.Add(bucket);
        await context.SaveChangesAsync();

        var result = await context.Buckets.FindAsync(bucket.Id);

        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Spam");
    }

    [Fact]
    public async Task Bucket_WithAssociatedFolder_LinksCorrectly()
    {
        await using var context = CreateInMemoryContext();

        var bucket = new Bucket
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Spam",
            Description = "Spam emails"
        };
        context.Buckets.Add(bucket);
        await context.SaveChangesAsync();

        var folder = new MailFolder
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Spam",
            BucketId = bucket.Id
        };
        context.MailFolders.Add(folder);
        await context.SaveChangesAsync();

        var result = await context.MailFolders
            .Include(f => f.Bucket)
            .FirstOrDefaultAsync(f => f.Id == folder.Id);

        result.ShouldNotBeNull();
        result!.Bucket.ShouldNotBeNull();
        result.Bucket!.Name.ShouldBe("Spam");
    }

    [Fact]
    public async Task MailFolder_Insert_SavesSuccessfully()
    {
        await using var context = CreateInMemoryContext();

        var folder = new MailFolder
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Inbox"
        };

        context.MailFolders.Add(folder);
        await context.SaveChangesAsync();

        var result = await context.MailFolders.FindAsync(folder.Id);

        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Inbox");
    }
}
