using TickerQ.DependencyInjection;

namespace ProjectTemplate.Worker;

/// <summary>Worker entry point and composition root.</summary>
public partial class Program
{
    /// <summary>Builds and runs the worker host.</summary>
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        ConfigureServices(builder);
        var host = builder.Build();
        host.UseTickerQ();
        await host.RunAsync();
    }
}
