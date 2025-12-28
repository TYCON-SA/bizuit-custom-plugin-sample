using Bizuit.Backend.Abstractions;
using Bizuit.Backend.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyPlugin.Features.Items;
using MyPlugin.Features.Products;
using MyPlugin.Features.AuditLogs;
using MyPlugin.Features.Me;

namespace MyPlugin;

/// <summary>
/// Main plugin class - entry point for the Backend Host.
///
/// This template demonstrates:
/// - Items: Public CRUD endpoints (no authentication)
/// - Products: Protected CRUD endpoints (requires auth, admin for delete)
/// - AuditLogs: [NoTransaction] endpoints for fire-and-forget logging
/// </summary>
public class MyPluginPlugin : IBackendPlugin
{
    public PluginInfo Info => new()
    {
        Name = "myplugin",
        Version = "1.0.0",
        Description = "My custom backend plugin",
        Author = "Your Name"
    };

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Get connection string from plugin configuration
        var connectionString = configuration.GetConnectionString("Default");

        // Get system configuration injected by Backend Host
        var dashboardApiUrl = configuration["System:DashboardApiUrl"];
        var tenantId = configuration["System:TenantId"];

        Console.WriteLine($"[MyPlugin] Loaded for tenant '{tenantId}' with Dashboard API: {dashboardApiUrl ?? "(not configured)"}");

        if (!string.IsNullOrEmpty(connectionString))
        {
            // Add Bizuit Backend Core services (SafeQueryBuilder, etc.)
            services.AddBizuitBackendCore(connectionString);
        }

        // Optional: Register HttpClient for Dashboard API calls
        if (!string.IsNullOrEmpty(dashboardApiUrl))
        {
            services.AddHttpClient("DashboardClient", client =>
            {
                client.BaseAddress = new Uri(dashboardApiUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }

        // Feature: Items (public CRUD)
        services.AddScoped<ItemsService>();
        services.AddScoped<ItemsRepository>();

        // Feature: Products (protected CRUD with auth)
        services.AddScoped<ProductsService>();
        services.AddScoped<ProductsRepository>();

        // Feature: AuditLogs ([NoTransaction] endpoints)
        services.AddScoped<AuditLogsRepository>();
    }

    public void ConfigureEndpoints(IPluginEndpointBuilder endpoints)
    {
        // Feature: Items - Public endpoints (no auth required)
        // GET/POST/PUT/DELETE /items
        ItemsEndpoints.Map(endpoints);

        // Feature: Products - Protected endpoints
        // GET: public, POST/PUT: requires auth, DELETE: requires admin
        ProductsEndpoints.Map(endpoints);

        // Feature: AuditLogs - [NoTransaction] endpoints
        // POST endpoints do NOT use transactions (fire-and-forget)
        AuditLogsEndpoints.Map(endpoints);

        // Feature: Me - Debug endpoint for user context
        // GET /me - returns ALL user information
        MeEndpoints.Map(endpoints);
    }

    public void OnUnloading()
    {
        // Cleanup if needed (close connections, flush caches, etc.)
    }
}
