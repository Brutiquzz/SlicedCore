using ProjectTemplate.Dependencies;
using ProjectTemplate.Dependencies.Extensions;
using Microsoft.Extensions.Hosting;

namespace ProjectTemplate.Tests.Dependencies.LayerBuilders;

public sealed class LayerBuilderTests
{
    [Test]
    public async Task LayerBuilders_SupportHostApplicationBuilder_AndFactoryRegistrations()
    {
        var builder = Host.CreateApplicationBuilder();

        new ApplicationLayerBuilder(builder)
            .AddScoped<TestApplicationDependency>(_ => new TestApplicationDependency("application"));

        new InfrastructureLayerBuilder(builder)
            .AddScoped<TestInfrastructureDependency>(_ => new TestInfrastructureDependency("infrastructure"));

        using var host = builder.Build();

        var applicationDependency = host.Services.GetRequiredApplicationDependency<TestApplicationDependency>();
        var infrastructureDependency = host.Services.GetRequiredInfrastructureDependency<TestInfrastructureDependency>();

        await Assert.That(applicationDependency.Value).IsEqualTo("application");
        await Assert.That(infrastructureDependency.Value).IsEqualTo("infrastructure");
    }

    private sealed record TestApplicationDependency(string Value);

    private sealed record TestInfrastructureDependency(string Value);
}
