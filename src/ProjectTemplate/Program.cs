namespace ProjectTemplate;

/// <summary>
/// Application entry point and composition root.
/// Split across partial files: <c>Program.Services.cs</c> (hosting + DI registration),
/// <c>Program.Middleware.cs</c> (HTTP pipeline), and <c>Program.Dependencies.cs</c> (layer wiring).
/// </summary>
public partial class Program
{
    /// <summary>Builds and runs the web application.</summary>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureHosting(builder);
        ConfigureServices(builder);
        var app = builder.Build();
        ConfigureMiddleware(app);
        app.Run();
    }
}
