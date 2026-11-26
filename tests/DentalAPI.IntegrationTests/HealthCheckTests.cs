using System.Net;
using DentalAPI.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace DentalAPI.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class HealthCheckTests(DentalApiFactory factory)
{
    [Fact]
    public async Task GetHealth_DatabaseIsReachable_ReturnsOk()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
