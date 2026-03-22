using Xunit;

namespace PopfileNet.IntegrationTests;

[CollectionDefinition("GreenMailTests")]
public class GreenMailTestsCollection : ICollectionFixture<GreenMailFixture>
{
}