using Bizuit.Backend.Abstractions;
using Bizuit.Backend.Core.Auth;
using Microsoft.AspNetCore.Http;

namespace MyPlugin.Features.Me;

/// <summary>
/// Debug endpoint that returns ALL information about the authenticated user.
/// Useful for debugging authentication and authorization.
/// </summary>
public static class MeEndpoints
{
    public static void Map(IPluginEndpointBuilder endpoints)
    {
        endpoints.MapGet("me", GetMe)
            .RequireAuthorization("Administrators,BizuitAdmins,Gestores,Registered Users");
    }

    /// <summary>
    /// Returns ALL information about the authenticated user.
    /// </summary>
    private static Task<IResult> GetMe(BizuitUserContext user, HttpContext httpContext)
    {
        var info = new
        {
            // === Basic User Info ===
            username = user.Username,
            tenantId = user.TenantId,
            isAuthenticated = user.IsAuthenticated,
            tokenType = user.TokenType,
            expiresAt = user.ExpiresAt,
            rawToken = user.RawToken,

            // === Roles ===
            roles = user.Roles,
            rolesCount = user.Roles.Count,

            // === Role Helper Methods ===
            roleChecks = new
            {
                hasAdministrators = user.HasRole("Administrators"),
                hasBizuitAdmins = user.HasRole("BizuitAdmins"),
                hasGestores = user.HasRole("Gestores"),
                hasSupervisores = user.HasRole("Supervisores"),
                hasAnyAdmin = user.HasAnyRole("Administrators", "BizuitAdmins"),
                hasAllAdmins = user.HasAllRoles("Administrators", "BizuitAdmins")
            },

            // === Role Settings (from UserRoleSettings table) ===
            roleSettings = user.RoleSettings.Select(s => new
            {
                role = s.Role,
                name = s.Name,
                value = s.Value
            }),
            roleSettingsCount = user.RoleSettings.Count,

            // === Role Settings by Role ===
            settingsByRole = user.Roles.Select(role => new
            {
                role,
                settings = user.GetRoleSettings(role).Select(s => new { s.Name, s.Value })
            }),

            // === Common Setting Values ===
            commonSettings = new
            {
                productos = user.GetSettingValues("Producto").ToList(),
                idGestor = user.GetSettingValues("IdGestor").ToList(),
            },

            // === Claims from Token ===
            claims = user.Claims,
            claimsCount = user.Claims.Count,

            // === Principal Claims (if available) ===
            principalClaims = user.Principal?.Claims?.Select(c => new
            {
                type = c.Type,
                value = c.Value,
                issuer = c.Issuer
            }),

            // === HTTP Context Info ===
            httpInfo = new
            {
                method = httpContext.Request.Method,
                path = httpContext.Request.Path.Value,
                queryString = httpContext.Request.QueryString.Value,
                scheme = httpContext.Request.Scheme,
                host = httpContext.Request.Host.Value,
                contentType = httpContext.Request.ContentType,
                userAgent = httpContext.Request.Headers.UserAgent.ToString(),
                authorization = httpContext.Request.Headers.Authorization.ToString(),
                allHeaders = httpContext.Request.Headers
                    .Where(h => !h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(h => h.Key, h => h.Value.ToString())
            },

            // === Timestamp ===
            timestamp = DateTime.UtcNow,
            timestampLocal = DateTime.Now
        };

        return Task.FromResult(Results.Ok(info));
    }
}
