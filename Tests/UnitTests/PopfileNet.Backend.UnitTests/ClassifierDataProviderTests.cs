using Shouldly;
using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Models;
using PopfileNet.Common;
using PopfileNet.Database;
using PopfileNet.Backend.Services;
using Xunit;

namespace PopfileNet.Backend.UnitTests;

public sealed class ClassifierDataProviderTests
{
    private static PopfileNetDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<PopfileNetDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new PopfileNetDbContext(options);
    }

    private static Email CreateEmail(string id, string folder, string subject = "Test")
    {
        return new Email
        {
            Id = id,
            Subject = subject,
            Folder = folder,
        };
    }

    [Fact]
    public async Task FetchFilteredAsync_WithNoFilter_ReturnsAllEmails()
    {
        using var db = CreateContext(nameof(FetchFilteredAsync_WithNoFilter_ReturnsAllEmails));
        db.Emails.AddRange(
            CreateEmail("1", "Inbox"),
            CreateEmail("2", "Sent"),
            CreateEmail("3", "Drafts"));
        await db.SaveChangesAsync();

        var provider = new ClassifierDataProvider(db);
        var request = new EmailFilterRequest("all");

        var result = await provider.FetchFilteredAsync(request);

        result.Count.ShouldBe(3);
    }

    [Fact]
    public async Task FetchFilteredAsync_WithFolderFilter_ReturnsOnlyMatchingEmails()
    {
        using var db = CreateContext(nameof(FetchFilteredAsync_WithFolderFilter_ReturnsOnlyMatchingEmails));
        db.Emails.AddRange(
            CreateEmail("1", "Inbox"),
            CreateEmail("2", "Sent"),
            CreateEmail("3", "Drafts"));
        await db.SaveChangesAsync();

        var provider = new ClassifierDataProvider(db);
        var request = new EmailFilterRequest("Inbox");

        var result = await provider.FetchFilteredAsync(request);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe("1");
    }

    [Fact]
    public async Task FetchFilteredAsync_WithNonExistentFolder_ReturnsEmptyList()
    {
        using var db = CreateContext(nameof(FetchFilteredAsync_WithNonExistentFolder_ReturnsEmptyList));
        db.Emails.AddRange(
            CreateEmail("1", "Inbox"),
            CreateEmail("2", "Sent"));
        await db.SaveChangesAsync();

        var provider = new ClassifierDataProvider(db);
        var request = new EmailFilterRequest("NonExistent");

        var result = await provider.FetchFilteredAsync(request);

        result.ShouldBeEmpty();
    }
}
