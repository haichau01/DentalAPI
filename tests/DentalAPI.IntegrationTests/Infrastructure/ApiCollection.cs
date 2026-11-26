namespace DentalAPI.IntegrationTests.Infrastructure;

/// <summary>Shares one API and database container across all integration test classes.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<DentalApiFactory>
{
    public const string Name = "Api";
}
