using FinPilot.Api.Controllers;
using Xunit;

namespace FinPilot.IntegrationTests.Smoke;

public sealed class ApiAssemblySmokeTests
{
    [Fact]
    public void HealthController_ShouldBeDiscoverable()
    {
        Assert.NotNull(typeof(HealthController).Assembly);
    }
}
