using Xunit;

namespace PopfileNet.IntegrationTests;

[CollectionDefinition("DatabaseTests")]
public class DatabaseTestsCollection : ICollectionFixture<DatabaseFixture>
{
}