using Bizuit.Backend.Abstractions;
using Bizuit.Backend.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyPlugin.Features.Items;  // example-feature
using MyPlugin.Features.Products;  // example-feature
using MyPlugin.Features.AuditLogs;  // example-feature
using MyPlugin.Features.Me;
using MyPlugin.Shared;

namespace MyPlugin;

/// <summary>
/// Main plugin class - entry point for the Backend Host.
/// Example features, removed by scripts/remove-examples.mjs when a product starts from this template:  // example-feature
/// - Items: Public CRUD endpoints (no authentication)  // example-feature
/// - Products: Protected CRUD endpoints (requires auth, admin for delete)  // example-feature
/// - AuditLogs: [NoTransaction] endpoints for fire-and-forget logging  // example-feature
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

        // Feature: Items (public CRUD)  // example-feature
        services.AddScoped<ItemsService>();  // example-feature
        services.AddScoped<ItemsRepository>();  // example-feature

        // Feature: Products (protected CRUD with auth)  // example-feature
        services.AddScoped<ProductsService>();  // example-feature
        services.AddScoped<ProductsRepository>();  // example-feature

        // Feature: AuditLogs ([NoTransaction] endpoints)  // example-feature
        services.AddScoped<AuditLogsRepository>();  // example-feature

        // Shared: Plugin Logger (writes to BZExtPluginLogs table)
        // IMPORTANT: Use factory delegate because IConfiguration is NOT available via DI in Backend Host!
        // See README.md section "IConfiguration is NOT available via DI" for details.
        services.AddSingleton<PluginLogger>(sp =>
        {
            return new PluginLogger(configuration);  // Pass configuration from this scope
        });
    }

    public void ConfigureEndpoints(IPluginEndpointBuilder endpoints)
    {
        // Feature: Items - Public endpoints (no auth required)  // example-feature
        // GET/POST/PUT/DELETE /items  // example-feature
        ItemsEndpoints.Map(endpoints);  // example-feature

        // Feature: Products - Protected endpoints  // example-feature
        // GET: public, POST/PUT: requires auth, DELETE: requires admin  // example-feature
        ProductsEndpoints.Map(endpoints);  // example-feature

        // Feature: AuditLogs - [NoTransaction] endpoints  // example-feature
        // POST endpoints do NOT use transactions (fire-and-forget)  // example-feature
        AuditLogsEndpoints.Map(endpoints);  // example-feature

        // Feature: Me - Debug endpoint for user context
        // GET /me - returns ALL user information
        MeEndpoints.Map(endpoints);
    }

    public void OnUnloading()
    {
        // Cleanup if needed (close connections, flush caches, etc.)
    }
}
