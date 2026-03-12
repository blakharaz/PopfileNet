using Xunit;

namespace PopfileNet.IntegrationTests;

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}