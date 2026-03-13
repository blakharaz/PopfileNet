using Xunit;

namespace PopfileNet.IntegrationTests;

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>, IAsyncLifetime
{
    public async Task InitializeAsync() => await Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await DatabaseFixture.Instance.DisposeAsync();
    }
}