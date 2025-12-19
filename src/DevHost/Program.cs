using Bizuit.Backend.Abstractions;
using Bizuit.Backend.Core.Auth;
using Bizuit.Backend.Core.Database;
using Bizuit.Backend.Core.Middleware;
using Dapper;
using DevHost.Endpoints;
using DevHost.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add Swagger with Auth support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MyPlugin DevHost", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Enter username (e.g., 'admin') to authenticate with real roles from Dashboard DB",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add Authentication and Authorization
builder.Services.AddAuthentication("DevHostBearer")
    .AddScheme<AuthenticationSchemeOptions, DevHostBearerAuthenticationHandler>("DevHostBearer", null);
builder.Services.AddAuthorization();

// Register connection factory using appsettings.json connection string
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found in appsettings.json");

// Register logging connection factory for SQL query debugging (Development only)
var enableSqlLogging = builder.Environment.IsDevelopment() && builder.Configuration.GetValue("DevHost:EnableSqlLogging", true);

builder.Services.AddScoped<IDbConnection>(sp =>
{
    if (enableSqlLogging)
    {
        var logger = sp.GetRequiredService<ILogger<DapperLoggingConnection>>();
        return new DapperLoggingConnection(connectionString, logger);
    }
    return new SqlConnection(connectionString);
});

builder.Services.AddScoped<IConnectionFactory>(sp =>
{
    if (enableSqlLogging)
    {
        var logger = sp.GetRequiredService<ILogger<DapperLoggingConnection>>();
        return new LoggingConnectionFactory(connectionString, logger);
    }
    return new SqlConnectionFactory(connectionString);
});

// Register Dashboard Token Service for token decryption
builder.Services.AddSingleton<DashboardTokenService>();

// Register Dashboard DB service for fetching roles and roleSettings
var dashboardConnectionString = builder.Configuration.GetConnectionString("Dashboard");

if (string.IsNullOrEmpty(dashboardConnectionString))
{
    throw new InvalidOperationException(
        "Dashboard connection string is required. DevHost needs Dashboard DB to fetch user roles.\n" +
        "Configure 'ConnectionStrings:Dashboard' in appsettings.json or appsettings.Development.json");
}

builder.Services.AddSingleton<IDashboardUserService>(new DashboardUserService(dashboardConnectionString));
Console.WriteLine("[DevHost] Dashboard DB configured - will fetch roles from database");

// Register BizuitUserContext that will be populated from Claims + RoleSettings
builder.Services.AddScoped<BizuitUserContext>(serviceProvider =>
{
    var httpContext = serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;
    if (httpContext?.User?.Identity?.IsAuthenticated == true)
    {
        var username = httpContext.User.FindFirst(ClaimTypes.Name)?.Value ?? "anonymous";
        var roles = httpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        // Parse RoleSettings from custom claim (JSON)
        var roleSettings = new List<UserRoleSetting>();
        var roleSettingsClaim = httpContext.User.FindFirst("RoleSettings")?.Value;
        if (!string.IsNullOrEmpty(roleSettingsClaim))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<RoleSettingDto>>(roleSettingsClaim);
                if (parsed != null)
                {
                    roleSettings = parsed.Select(s => new UserRoleSetting(s.Role, s.Name, s.Value)).ToList();
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        // Parse ExpiresAt from claim
        DateTime? expiresAt = null;
        var expiresAtClaim = httpContext.User.FindFirst("ExpiresAt")?.Value;
        if (!string.IsNullOrEmpty(expiresAtClaim) && DateTime.TryParse(expiresAtClaim, out var parsed2))
        {
            expiresAt = parsed2;
        }

        // Get raw token from claim
        var rawToken = httpContext.User.FindFirst("RawToken")?.Value;

        return new BizuitUserContext
        {
            Username = username,
            TenantId = "dev-tenant",
            IsAuthenticated = true,
            TokenType = rawToken != null ? "encrypted-dashboard" : "plain-username",
            Roles = roles,
            RoleSettings = roleSettings,
            ExpiresAt = expiresAt,
            RawToken = rawToken
        };
    }

    return new BizuitUserContext
    {
        Username = "anonymous",
        TenantId = "dev-tenant",
        IsAuthenticated = false,
        TokenType = "none",
        Roles = new List<string>(),
        RoleSettings = new List<UserRoleSetting>()
    };
});

builder.Services.AddHttpContextAccessor();

// Load and configure the plugin
var plugin = new MyPlugin.MyPluginPlugin();
plugin.ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

// Exception handling middleware (MUST be first to catch all exceptions)
app.UseMiddleware<ExceptionMiddleware>();

// Enable Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Enable automatic transactions (matches Backend Host behavior)
app.UseMiddleware<AutoTransactionMiddleware>();

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyPlugin DevHost v1");
    c.RoutePrefix = string.Empty; // Swagger at root
});

// Register debug endpoints (only in Development)
app.MapDebugEndpoints(app.Environment);

// Create endpoint builder adapter
var endpointBuilder = new DevHostEndpointBuilder(app);
plugin.ConfigureEndpoints(endpointBuilder);

// Finalize endpoints (apply authorization from plugin configuration)
endpointBuilder.FinalizeEndpoints();

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║       MyPlugin DevHost - Development Server                  ║");
Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
Console.WriteLine("║  Swagger UI:  http://localhost:5001                          ║");
Console.WriteLine("║  Debug Info:  http://localhost:5001/api/_debug               ║");
Console.WriteLine($"║  Plugin:      {plugin.Info.Name,-35}        ║");
Console.WriteLine($"║  Version:     {plugin.Info.Version,-35}        ║");
Console.WriteLine($"║  SQL Logging: {(enableSqlLogging ? "Enabled" : "Disabled"),-35}        ║");
Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
Console.WriteLine("║  Authentication:                                             ║");
Console.WriteLine("║    Pass encrypted Dashboard token as Bearer token            ║");
Console.WriteLine("║    OR pass plain username as Bearer token                    ║");
Console.WriteLine("║    Roles and RoleSettings loaded from Dashboard DB           ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();

app.Run();

/// <summary>
/// Adapter that implements IPluginEndpointBuilder for the DevHost.
/// Maps plugin endpoints directly to the WebApplication with proper authorization support.
/// </summary>
public class DevHostEndpointBuilder : IPluginEndpointBuilder
{
    private readonly WebApplication _app;
    private readonly List<(IEndpointConventionBuilder Native, IPluginEndpointConventionBuilder Plugin)> _endpoints = new();

    public DevHostEndpointBuilder(WebApplication app)
    {
        _app = app;
    }

    public IEndpointConventionBuilder MapGet(string pattern, Delegate handler)
        => RegisterEndpoint(_app.MapGet($"/api/{pattern}", handler));

    public IEndpointConventionBuilder MapPost(string pattern, Delegate handler)
        => RegisterEndpoint(_app.MapPost($"/api/{pattern}", handler));

    public IEndpointConventionBuilder MapPut(string pattern, Delegate handler)
        => RegisterEndpoint(_app.MapPut($"/api/{pattern}", handler));

    public IEndpointConventionBuilder MapDelete(string pattern, Delegate handler)
        => RegisterEndpoint(_app.MapDelete($"/api/{pattern}", handler));

    public IEndpointConventionBuilder MapPatch(string pattern, Delegate handler)
        => RegisterEndpoint(_app.MapPatch($"/api/{pattern}", handler));

    private IEndpointConventionBuilder RegisterEndpoint(IEndpointConventionBuilder nativeBuilder)
    {
        var pluginBuilder = new PluginEndpointConventionBuilder(null);
        _endpoints.Add((nativeBuilder, pluginBuilder));
        return pluginBuilder;
    }

    public void FinalizeEndpoints()
    {
        foreach (var (native, plugin) in _endpoints)
        {
            if (plugin.RequiresAuthorization)
            {
                if (!string.IsNullOrEmpty(plugin.RequiredRoles))
                {
                    // Apply role-based authorization (ANY role)
                    var roleArray = plugin.RequiredRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    native.RequireAuthorization(policy =>
                    {
                        policy.RequireAuthenticatedUser();
                        policy.RequireRole(roleArray);
                    });
                }
                else
                {
                    // Just require authentication
                    native.RequireAuthorization();
                }
            }
        }
    }
}

/// <summary>
/// Authentication handler for DevHost.
/// Supports both encrypted Dashboard tokens and plain usernames.
/// - If token is encrypted (Dashboard API format): Decrypt and extract username
/// - If token is plain username: Use directly
/// Then fetches roles from Dashboard DB.
/// </summary>
public class DevHostBearerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IDashboardUserService _dashboardService;
    private readonly DashboardTokenService _tokenService;

    public DevHostBearerAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        IDashboardUserService dashboardService,
        DashboardTokenService tokenService)
        : base(options, logger, encoder)
    {
        _dashboardService = dashboardService;
        _tokenService = tokenService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return AuthenticateResult.NoResult();
        }

        var authHeader = Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("Invalid Authorization header");
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        // Try to decrypt token first (if it's an encrypted Dashboard token)
        var tokenData = _tokenService.TryDecryptTokenFull(token);
        string username;
        DateTime? expiresAt = null;
        string? rawToken = null;

        if (tokenData != null)
        {
            username = tokenData.Username;
            expiresAt = tokenData.ExpiresAt;
            rawToken = tokenData.RawToken;
            Logger.LogDebug("[Auth] Decrypted token for user '{Username}', expires: {ExpiresAt}", username, expiresAt);
        }
        else
        {
            // If decryption failed, treat token as plain username
            username = token;
            Logger.LogDebug("[Auth] Using token as plain username: '{Username}'", username);
        }

        // Get user data from Dashboard DB
        var userData = await _dashboardService.GetUserDataAsync(username);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userData.UserId),
            new Claim(ClaimTypes.Name, userData.Username)
        };

        // Add roles
        foreach (var role in userData.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add RoleSettings as JSON claim
        if (userData.RoleSettings.Any())
        {
            var roleSettingsJson = JsonSerializer.Serialize(userData.RoleSettings);
            claims.Add(new Claim("RoleSettings", roleSettingsJson));
        }

        // Add expiration time claim
        if (expiresAt.HasValue)
        {
            claims.Add(new Claim("ExpiresAt", expiresAt.Value.ToString("O")));
        }

        // Add raw token claim
        if (!string.IsNullOrEmpty(rawToken))
        {
            claims.Add(new Claim("RawToken", rawToken));
        }

        var identity = new ClaimsIdentity(claims, "DevHostBearer");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "DevHostBearer");

        return AuthenticateResult.Success(ticket);
    }
}

// DTO for role settings claim
public record RoleSettingDto(string Role, string Name, string Value);

// User data returned by dashboard service
public record DashboardUserData(
    string UserId,
    string Username,
    List<string> Roles,
    List<RoleSettingDto> RoleSettings);

// Interface for dashboard user service
public interface IDashboardUserService
{
    Task<DashboardUserData> GetUserDataAsync(string username);
}

/// <summary>
/// Real implementation that queries Dashboard DB for roles and roleSettings.
/// </summary>
public class DashboardUserService : IDashboardUserService
{
    private readonly string _connectionString;

    public DashboardUserService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<DashboardUserData> GetUserDataAsync(string username)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Get user ID
        var userId = await connection.QueryFirstOrDefaultAsync<int?>(
            "SELECT UserId FROM Users WHERE UserName = @Username",
            new { Username = username });

        if (!userId.HasValue)
        {
            // User not found - return empty data
            return new DashboardUserData(
                $"unknown-{username}",
                username,
                new List<string>(),
                new List<RoleSettingDto>());
        }

        // Get roles
        var roles = await connection.QueryAsync<string>(@"
            SELECT r.RoleName
            FROM UserRoles ur
            INNER JOIN Roles r ON ur.RoleId = r.RoleId
            WHERE ur.UserId = @UserId",
            new { UserId = userId.Value });

        // Get role settings
        var settings = await connection.QueryAsync<RoleSettingDto>(@"
            SELECT r.RoleName as Role, d.SettingName as Name, s.SettingValue as Value
            FROM UserRoles ur
            INNER JOIN Roles r ON ur.RoleId = r.RoleId
            INNER JOIN UserRoleSettings s ON ur.UserRoleId = s.UserRoleId
            INNER JOIN UserRoleSettingsDefinition d ON s.UserRoleSettingDefinitionId = d.UserRoleSettingDefinitionId
            WHERE ur.UserId = @UserId
              AND s.SettingValue IS NOT NULL AND s.SettingValue <> ''",
            new { UserId = userId.Value });

        return new DashboardUserData(
            userId.Value.ToString(),
            username,
            roles.ToList(),
            settings.ToList());
    }
}

/// <summary>
/// Result of token decryption containing all token data.
/// </summary>
public record DecryptedTokenData(
    string Username,
    DateTime? ExpiresAt,
    string RawToken,
    Dictionary<string, JsonElement>? AllFields
);

/// <summary>
/// Token service that handles Dashboard token decryption.
/// Supports both encrypted Dashboard tokens and plain usernames.
/// </summary>
public class DashboardTokenService
{
    // Dashboard API uses this hardcoded key for DES encryption
    private const string DASHBOARD_CRYPTO_KEY = "12345678";
    private readonly ILogger<DashboardTokenService> _logger;

    public DashboardTokenService(ILogger<DashboardTokenService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Attempts to decrypt a Dashboard token.
    /// Returns the username from the decrypted token, or null if not a valid encrypted token.
    /// </summary>
    public string? TryDecryptToken(string token)
    {
        var result = TryDecryptTokenFull(token);
        return result?.Username;
    }

    /// <summary>
    /// Attempts to decrypt a Dashboard token and returns full token data.
    /// Returns all fields from the decrypted token, or null if not a valid encrypted token.
    /// </summary>
    public DecryptedTokenData? TryDecryptTokenFull(string token)
    {
        try
        {
            // URL decode first (tokens from Dashboard API are URL-encoded)
            var decodedToken = System.Web.HttpUtility.UrlDecode(token);

            _logger.LogDebug("[TokenService] Attempting to decrypt token (length: {Length})", decodedToken.Length);

            var keyBytes = Encoding.UTF8.GetBytes(DASHBOARD_CRYPTO_KEY.Substring(0, 8));
            var encryptedBytes = Convert.FromBase64String(decodedToken);

            using var des = DES.Create();
            des.Key = keyBytes;
            des.IV = keyBytes;
            des.Mode = CipherMode.CBC;
            des.Padding = PaddingMode.PKCS7;

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(encryptedBytes, 0, encryptedBytes.Length);
                cs.FlushFinalBlock();
            }

            var decryptedJson = Encoding.UTF8.GetString(ms.ToArray());
            _logger.LogDebug("[TokenService] Token decrypted successfully: {Json}", decryptedJson);

            // Parse JSON to extract all fields
            var tokenData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(decryptedJson);
            if (tokenData == null)
            {
                _logger.LogWarning("[TokenService] Decrypted token has invalid JSON format");
                return null;
            }

            // Extract username from token
            string? username = null;
            if (tokenData.TryGetValue("UserName", out var usernameValue))
            {
                username = usernameValue.GetString();
                _logger.LogInformation("[TokenService] Extracted username '{Username}' from encrypted token", username);
            }

            if (username == null)
            {
                _logger.LogWarning("[TokenService] Decrypted token missing 'UserName' field");
                return null;
            }

            // Extract expiration date (Dashboard uses "ExpirationDate" field)
            DateTime? expiresAt = null;
            if (tokenData.TryGetValue("ExpirationDate", out var expirationValue))
            {
                var expirationStr = expirationValue.GetString();
                if (DateTime.TryParse(expirationStr, out var parsed))
                {
                    expiresAt = parsed;
                    _logger.LogDebug("[TokenService] Token expires at: {ExpiresAt}", expiresAt);
                }
            }

            return new DecryptedTokenData(username, expiresAt, token, tokenData);
        }
        catch (FormatException)
        {
            // Not a valid Base64 string - probably a plain username
            _logger.LogDebug("[TokenService] Token is not Base64 encoded (likely a plain username)");
            return null;
        }
        catch (CryptographicException ex)
        {
            // Decryption failed - probably not an encrypted token
            _logger.LogDebug("[TokenService] Token decryption failed: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TokenService] Unexpected error during token decryption");
            return null;
        }
    }
}
