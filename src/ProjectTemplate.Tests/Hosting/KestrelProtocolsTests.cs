using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace ProjectTemplate.Tests.Hosting;

public sealed class KestrelProtocolsTests
{
    [Test]
    public async Task ConfigureHosting_EnablesHttpsWithHttp1Http2AndHttp3OnPort5001()
    {
        var builder = WebApplication.CreateBuilder();

        ProjectTemplate.Program.ConfigureHosting(builder);

        var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value;

        var codeBackedListenOptionsProperty = typeof(KestrelServerOptions).GetProperty(
            "CodeBackedListenOptions",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        await Assert.That(codeBackedListenOptionsProperty).IsNotNull();

        if (codeBackedListenOptionsProperty is null)
        {
            throw new InvalidOperationException("Expected Kestrel code-backed listen options property to exist.");
        }

        var listenOptions = (IReadOnlyList<ListenOptions>?)codeBackedListenOptionsProperty.GetValue(options);

        await Assert.That(listenOptions).IsNotNull();

        if (listenOptions is null)
        {
            throw new InvalidOperationException("Expected configured Kestrel listen options.");
        }

        var endpoint = listenOptions.Single();

        await Assert.That(endpoint.IPEndPoint?.Port).IsEqualTo(5001);
        await Assert.That(endpoint.Protocols).IsEqualTo(HttpProtocols.Http1AndHttp2AndHttp3);

        await app.DisposeAsync();
    }
}
