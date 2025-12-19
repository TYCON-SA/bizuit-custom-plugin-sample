using Bizuit.Backend.Abstractions;
using Bizuit.Backend.Core.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyPlugin.Features.Items.Models;

namespace MyPlugin.Features.Items;

/// <summary>
/// Minimal API endpoints for Items.
/// Routes will be prefixed with /api/plugins/{name}/{version}/
///
/// This example demonstrates how to access user information including:
/// - Username, TenantId, Roles
/// - RoleSettings (business properties per role from UserRoleSettings table)
/// </summary>
public static class ItemsEndpoints
{
    public static void Map(IPluginEndpointBuilder endpoints)
    {
        endpoints.MapGet("items", GetAll);
        endpoints.MapGet("items/search", Search);
        endpoints.MapGet("items/{id:int}", GetById);
        endpoints.MapPost("items", Create);
        endpoints.MapPut("items/{id:int}", Update);
        endpoints.MapDelete("items/{id:int}", Delete);

        // Example endpoints demonstrating user context access
        endpoints.MapGet("items/my-info", GetMyInfo);
        endpoints.MapGet("items/by-product", GetItemsByUserProduct);
    }

    private static async Task<IResult> GetAll(ItemsService service)
    {
        var items = await service.GetAllAsync();
        return Results.Ok(items);
    }

    private static async Task<IResult> Search(
        ItemsService service,
        [FromQuery] string? name = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] bool? isActive = null)
    {
        var items = await service.SearchAsync(name, minPrice, isActive);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetById(int id, ItemsService service)
    {
        var item = await service.GetByIdAsync(id);
        if (item == null)
        {
            return Results.NotFound(new { error = "Item not found" });
        }
        return Results.Ok(item);
    }

    private static async Task<IResult> Create(CreateItemRequest request, ItemsService service)
    {
        try
        {
            var itemId = await service.CreateAsync(request);
            return Results.Created($"/items/{itemId}", new { itemId });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Update(int id, UpdateItemRequest request, ItemsService service)
    {
        try
        {
            var updated = await service.UpdateAsync(id, request);
            if (!updated)
            {
                return Results.NotFound(new { error = "Item not found" });
            }
            return Results.Ok(new { message = "Item updated" });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Delete(int id, ItemsService service)
    {
        var deleted = await service.DeleteAsync(id);
        if (!deleted)
        {
            return Results.NotFound(new { error = "Item not found" });
        }
        return Results.Ok(new { message = "Item deleted" });
    }

    // =========================================================================
    // EXAMPLE: Accessing User Context (Roles and RoleSettings)
    // =========================================================================

    /// <summary>
    /// Example endpoint showing how to access the authenticated user's information.
    /// Returns username, tenant, roles, and role settings.
    ///
    /// BizuitUserContext is automatically injected when you add it as a parameter.
    ///
    /// IMPORTANT: To use RoleSettings, update Bizuit.Backend.Core to version 1.0.3+
    /// </summary>
    private static Task<IResult> GetMyInfo(BizuitUserContext user)
    {
        // BizuitUserContext provides:
        // - user.Username         : The authenticated username
        // - user.TenantId         : Tenant identifier for multi-tenant scenarios
        // - user.IsAuthenticated  : Whether the user is authenticated
        // - user.Roles            : List<string> of role names
        // - user.ExpiresAt        : Token expiration time
        //
        // With Bizuit.Backend.Core 1.0.3+:
        // - user.RoleSettings     : List<RoleSetting> with business properties per role
        // - user.GetSettingValues("SettingName") : Get all values for a setting
        // - user.HasSettingValue("SettingName", "Value") : Check if has specific value
        // - user.GetSettingValue("RoleName", "SettingName") : Get value for specific role

        var info = new
        {
            username = user.Username,
            tenantId = user.TenantId,
            isAuthenticated = user.IsAuthenticated,
            expiresAt = user.ExpiresAt,

            // All roles the user has
            roles = user.Roles,

            // Helper methods for role checking:
            hasAdminRole = user.HasRole("Administrators"),
            hasAnyGestorRole = user.HasAnyRole("Gestores", "Supervisores"),
            hasAllRoles = user.HasAllRoles("Administrators", "BizuitAdmins"),

            // RoleSettings - Business properties per role from UserRoleSettings table:
            roleSettings = user.RoleSettings.Select(s => new { s.Role, s.Name, s.Value }),
            allProductos = user.GetSettingValues("Producto"),
            hasCocacola = user.HasSettingValue("Producto", "COCACOLA")
        };

        return Task.FromResult(Results.Ok(info));
    }

    /// <summary>
    /// Example endpoint showing how to filter data based on user's roles.
    ///
    /// This pattern is useful for role-based data filtering.
    /// For more advanced filtering using RoleSettings, update to Bizuit.Backend.Core 1.0.3+
    /// </summary>
    private static async Task<IResult> GetItemsByUserProduct(
        ItemsService service,
        BizuitUserContext user)
    {
        // Check user roles for authorization
        if (!user.HasAnyRole("Administrators", "Gestores"))
        {
            return Results.Forbid();
        }

        // Get all items first
        var items = await service.GetAllAsync();

        // Filter items based on user's RoleSettings (Producto property)
        var allowedProducts = user.GetSettingValues("Producto").ToList();

        // If user has specific product restrictions, filter by them
        IEnumerable<Item> filteredItems = items;
        if (allowedProducts.Any())
        {
            filteredItems = items.Where(i =>
                allowedProducts.Contains(i.Category ?? "", StringComparer.OrdinalIgnoreCase));
        }

        return Results.Ok(new
        {
            message = $"Items for user {user.Username} with roles: {string.Join(", ", user.Roles)}",
            allowedProducts = allowedProducts,
            totalItems = items.Count(),
            filteredCount = filteredItems.Count(),
            items = filteredItems
        });
    }
}
