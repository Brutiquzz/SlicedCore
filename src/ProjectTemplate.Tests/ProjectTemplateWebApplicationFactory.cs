using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProjectTemplate.Data;
using TUnit.Core.Interfaces;

namespace ProjectTemplate.Tests;

public sealed class ProjectTemplateWebApplicationFactory : WebApplicationFactory<ProjectTemplate.Program>, IAsyncInitializer, IAsyncDisposable
{
    // Keep a single open connection so the SQLite :memory: database survives for the lifetime of the factory.
    private readonly SqliteConnection _dbConnection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _dbConnection.Open();

        builder.ConfigureServices(services =>
        {
            // In EF Core 9, AddDbContext registers options via the internal IDbContextOptionsConfiguration<TContext>.
            // We must remove ALL configurations for AppDbContext (including the SQL Server one from the app)
            // before adding our own SQLite override, otherwise both providers end up in the same context.
            var appDbContextTypeName = typeof(AppDbContext).FullName;
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(AppDbContext)
                         || (d.ServiceType.IsGenericType
                             && d.ServiceType.GetGenericArguments() is [var arg]
                             && arg == typeof(AppDbContext)
                             && d.ServiceType.GetGenericTypeDefinition().FullName is { } fn
                             && fn.Contains("IDbContextOptionsConfiguration")))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            // Re-register AppDbContext backed by the persistent in-memory SQLite connection.
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_dbConnection));

            // Replace JWT Bearer authentication with a test handler that returns a fixed authenticated identity.
            // This allows tests to call protected endpoints without needing real JWT tokens.
            services.RemoveAll<IAuthenticationSchemeProvider>();
            services
                .AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
    }

    public async Task InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _dbConnection.DisposeAsync();
        await base.DisposeAsync();
    }
}
