using Moq;
using PopfileNet.Database.DatabaseMaintenance;
using PopfileNet.Database.Migration;
using Shouldly;
using Xunit;

namespace PopfileNet.Database.UnitTests;

public class MigrationCheckerTests
{
    private readonly Mock<IDatabaseFacade> _mockDatabaseFacade;

    public MigrationCheckerTests()
    {
        _mockDatabaseFacade = new Mock<IDatabaseFacade>();
    }

    private MigrationChecker CreateChecker() => new(_mockDatabaseFacade.Object);

    [Fact]
    public async Task HasLegacyTablesAsync_CannotConnect_ReturnsFalse()
    {
        _mockDatabaseFacade.Setup(d => d.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var checker = CreateChecker();

        var result = await checker.HasLegacyTablesAsync();

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task HasLegacyTablesAsync_MigrationHistoryExists_ReturnsFalse()
    {
        _mockDatabaseFacade.Setup(d => d.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockDatabaseFacade.Setup(d => d.ExecuteSqlRawAsync(
                It.Is<string>(s => s.Contains("__EFMigrationsHistory")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var checker = CreateChecker();

        var result = await checker.HasLegacyTablesAsync();

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task HasLegacyTablesAsync_NoMigrationHistory_HasOtherTables_ReturnsTrue()
    {
        _mockDatabaseFacade.Setup(d => d.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockDatabaseFacade.Setup(d => d.ExecuteSqlRawAsync(
                It.Is<string>(s => s.Contains("__EFMigrationsHistory")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockDatabaseFacade.Setup(d => d.ExecuteSqlRawAsync(
                It.Is<string>(s => s.Contains("table_schema")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var checker = CreateChecker();

        var result = await checker.HasLegacyTablesAsync();

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task HasLegacyTablesAsync_NoMigrationHistory_NoOtherTables_ReturnsFalse()
    {
        _mockDatabaseFacade.Setup(d => d.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockDatabaseFacade.Setup(d => d.ExecuteSqlRawAsync(
                It.Is<string>(s => s.Contains("__EFMigrationsHistory")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockDatabaseFacade.Setup(d => d.ExecuteSqlRawAsync(
                It.Is<string>(s => s.Contains("table_schema")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var checker = CreateChecker();

        var result = await checker.HasLegacyTablesAsync();

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task HasPendingMigrationsAsync_HasPending_ReturnsTrue()
    {
        var pendingMigrations = new[] { "20260305073949_UpdateEmailAndFolderIds" };
        _mockDatabaseFacade.Setup(d => d.GetPendingMigrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingMigrations);

        var checker = CreateChecker();

        var result = await checker.HasPendingMigrationsAsync();

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task HasPendingMigrationsAsync_NoPending_ReturnsFalse()
    {
        _mockDatabaseFacade.Setup(d => d.GetPendingMigrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var checker = CreateChecker();

        var result = await checker.HasPendingMigrationsAsync();

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ApplyMigrationsAsync_CallsMigrate()
    {
        _mockDatabaseFacade.Setup(d => d.MigrateAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var checker = CreateChecker();

        await checker.ApplyMigrationsAsync();

        _mockDatabaseFacade.Verify(d => d.MigrateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
