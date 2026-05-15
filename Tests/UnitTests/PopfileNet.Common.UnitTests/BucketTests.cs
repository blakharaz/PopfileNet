using PopfileNet.Common;
using Shouldly;
using Xunit;

namespace PopfileNet.Common.UnitTests;

public class BucketTests
{
    [Fact]
    public void Bucket_CreatesWithGuidId()
    {
        var bucket = new Bucket();

        bucket.Id.ShouldNotBeNullOrEmpty();
        Guid.TryParse(bucket.Id, out _).ShouldBeTrue();
    }

    [Fact]
    public void Bucket_CreatesWithEmptyName()
    {
        var bucket = new Bucket();

        bucket.Name.ShouldBe(string.Empty);
    }

    [Fact]
    public void Bucket_CreatesWithEmptyDescription()
    {
        var bucket = new Bucket();

        bucket.Description.ShouldBe(string.Empty);
    }

    [Fact]
    public void Bucket_CreatesWithEmptyFoldersCollection()
    {
        var bucket = new Bucket();

        bucket.Folders.ShouldNotBeNull();
        bucket.Folders.ShouldBeEmpty();
    }

    [Fact]
    public void Bucket_CanSetName()
    {
        var bucket = new Bucket();

        bucket.Name = "Work Emails";

        bucket.Name.ShouldBe("Work Emails");
    }

    [Fact]
    public void Bucket_CanSetDescription()
    {
        var bucket = new Bucket();

        bucket.Description = "Work-related emails";

        bucket.Description.ShouldBe("Work-related emails");
    }

    [Fact]
    public void Bucket_CanAddFolders()
    {
        var bucket = new Bucket();
        var folder = new MailFolder { Id = "folder-1", Name = "Inbox" };

        bucket.Folders.Add(folder);

        bucket.Folders.Count.ShouldBe(1);
        bucket.Folders.ShouldContain(folder);
    }

    [Fact]
    public void Bucket_WithInitId_PreservesId()
    {
        var bucket = new Bucket { Id = "custom-id" };

        bucket.Id.ShouldBe("custom-id");
    }
}
