using Bizuit.Backend.Core.Auth;
using Bizuit.Backend.Core.Database;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;
using System.Text.Json;

namespace DevHost.Endpoints;

/// <summary>
/// Debug endpoints for DevHost - only available in Development mode.
/// Provides health checks, connection status, and runtime information.
/// </summary>
public static class DebugEndpoints
{
    public static void MapDebugEndpoints(this IEndpointRouteBuilder app, IWebHostEnvironment env)
    {
        if (!env.IsDevelopment())
        {
            // Debug endpoints are ONLY available in Development mode
            return;
        }

        var group = app.MapGroup("/api/_debug")
            .WithTags("Debug");

        group.MapGet("", GetDebugInfo)
            .WithName("GetDebugInfo")
            .WithSummary("Get DevHost debug information")
            .WithDescription("Returns current user, connection status, plugin info, and environment details. Only available in Development mode.");

        group.MapGet("/health", GetHealth)
            .WithName("GetHealth")
            .WithSummary("Health check endpoint")
            .WithDescription("Checks database connectivity and returns health status.");

        group.MapGet("/user", GetCurrentUser)
            .WithName("GetCurrentUser")
            .RequireAuthorization()
            .WithSummary("Get current authenticated user")
            .WithDescription("Returns current user context including roles and role settings. Requires authentication.");

        group.MapGet("/endpoints", GetRegisteredEndpoints)
            .WithName("GetRegisteredEndpoints")
            .WithSummary("Get all registered endpoints")
            .WithDescription("Returns list of all endpoints registered by the plugin.");
    }

    private static async Task<IResult> GetDebugInfo(
        HttpContext context,
        IConfiguration config,
        IWebHostEnvironment env,
        IConnectionFactory connFactory)
    {
        var connectionString = config.GetConnectionString("Default");
        var dashboardConnectionString = config.GetConnectionString("Dashboard");

        // Test database connection
        var dbStatus = "Unknown";
        var dbError = (string?)null;
        try
        {
            using var conn = await connFactory.CreateOpenConnectionAsync();
            dbStatus = "Connected";
            conn.Close();
        }
        catch (Exception ex)
        {
            dbStatus = "Failed";
            dbError = ex.Message;
        }

        // Get current user if authenticated
        var user = context.User.Identity?.IsAuthenticated == true
            ? new
            {
                username = context.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
                authenticated = true,
                roles = context.User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList()
            }
            : new
            {
                username = (string?)"anonymous",
                authenticated = false,
                roles = new List<string>()
            };

        var info = new
        {
            devHost = new
            {
                version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown",
                environment = env.EnvironmentName,
                contentRootPath = env.ContentRootPath,
                dotnetVersion = Environment.Version.ToString()
            },
            database = new
            {
                status = dbStatus,
                error = dbError,
                connectionString = MaskConnectionString(connectionString),
                dashboardConnectionString = !string.IsNullOrEmpty(dashboardConnectionString)
                    ? MaskConnectionString(dashboardConnectionString)
                    : null
            },
            authentication = new
            {
                mode = "Dashboard Token Authentication",
                currentUser = user,
                supportedTokens = new[] { "Encrypted Dashboard tokens", "Plain usernames" }
            },
            plugin = new
            {
                name = "MyPlugin",
                loaded = true,
                // Note: Plugin info would be populated if we had a reference to the plugin instance
            },
            timestamp = DateTime.UtcNow
        };

        return Results.Ok(info);
    }

    private static async Task<IResult> GetHealth(IConnectionFactory connFactory)
    {
        try
        {
            using var conn = await connFactory.CreateOpenConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            cmd.ExecuteScalar();
            conn.Close();

            return Results.Ok(new
            {
                status = "Healthy",
                database = "Connected",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return Results.Ok(new
            {
                status = "Unhealthy",
                database = "Failed",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    private static IResult GetCurrentUser(BizuitUserContext user)
    {
        var userInfo = new
        {
            username = user.Username,
            tenantId = user.TenantId,
            isAuthenticated = user.IsAuthenticated,
            tokenType = user.TokenType,
            roles = user.Roles,
            roleSettings = user.RoleSettings.Select(rs => new
            {
                role = rs.Role,
                name = rs.Name,
                value = rs.Value
            }).ToList(),
            timestamp = DateTime.UtcNow
        };

        return Results.Ok(userInfo);
    }

    private static IResult GetRegisteredEndpoints(HttpContext context)
    {
        var endpoints = context.RequestServices
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => new
            {
                route = e.RoutePattern.RawText,
                methods = e.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods.ToList() ?? new List<string>(),
                displayName = e.DisplayName,
                requiresAuth = e.Metadata.OfType<Microsoft.AspNetCore.Authorization.IAuthorizeData>().Any()
            })
            .OrderBy(e => e.route)
            .ToList();

        return Results.Ok(new
        {
            totalEndpoints = endpoints.Count,
            endpoints,
            timestamp = DateTime.UtcNow
        });
    }

    private static string MaskConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "Not configured";

        // Mask password in connection string
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.Password))
        {
            builder.Password = "********";
        }
        return builder.ConnectionString;
    }
}
