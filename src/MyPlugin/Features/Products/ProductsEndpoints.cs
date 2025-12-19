using Bizuit.Backend.Abstractions;
using Bizuit.Backend.Core.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyPlugin.Features.Products.Models;

namespace MyPlugin.Features.Products;

/// <summary>
/// Minimal API endpoints for Products.
///
/// AUTHENTICATION & AUTHORIZATION:
/// - GET endpoints: Public (no auth required)
/// - POST/PUT: Requires authentication (.RequireAuthorization())
/// - DELETE: Requires admin role (.RequireAuthorization("admin"))
///
/// Role Configuration:
/// - Developer specifies default roles using .RequireAuthorization("role1,role2")
/// - Backend Host auto-populates BackendPluginEndpointRoles table on plugin install
/// - Administrators can modify roles via admin panel without code changes
///
/// ROLESETTINGS PATTERN:
/// This file demonstrates how to use RoleSettings to:
/// - Filter data based on user's business properties (e.g., allowed categories)
/// - Require specific RoleSettings for certain operations (e.g., VendorId)
/// - Extract required values from user context with proper error handling
///
/// Routes are prefixed with /api/plugins/{name}/{version}/
/// Example: GET /api/plugins/myplugin/v1/products
/// </summary>
public static class ProductsEndpoints
{
    public static void Map(IPluginEndpointBuilder endpoints)
    {
        // ============================================
        // PUBLIC ENDPOINTS (no authentication required)
        // ============================================

        // GET /products - List all products
        endpoints.MapGet("products", GetAll);

        // GET /products/search - Search with filters
        endpoints.MapGet("products/search", Search);

        // GET /products/{id} - Get single product
        endpoints.MapGet("products/{id:int}", GetById);

        // GET /products/sku/{sku} - Get product by SKU
        endpoints.MapGet("products/sku/{sku}", GetBySKU);

        // ============================================
        // PROTECTED ENDPOINTS (authentication required)
        // ============================================

        // POST /products - Create product (requires auth)
        endpoints.MapPost("products", Create)
            .RequireAuthorization();

        // PUT /products/{id} - Update product (requires auth)
        endpoints.MapPut("products/{id:int}", Update)
            .RequireAuthorization();

        // ============================================
        // ADMIN-ONLY ENDPOINTS (requires admin role)
        // ============================================

        // DELETE /products/{id} - Delete product (admin only)
        endpoints.MapDelete("products/{id:int}", Delete)
            .RequireAuthorization("admin");

        // ============================================
        // ROLESETTINGS EXAMPLES
        // ============================================

        // GET /products/my-allowed - Get products filtered by user's allowed categories from RoleSettings
        endpoints.MapGet("products/my-allowed", GetMyAllowedProducts)
            .RequireAuthorization();

        // GET /products/my-info - Get current user info (roles, roleSettings) - EXAMPLE ENDPOINT
        endpoints.MapGet("products/my-info", GetMyInfo)
            .RequireAuthorization();
    }

    // --- Public Endpoints ---

    private static async Task<IResult> GetAll(ProductsService service)
    {
        var products = await service.GetAllAsync();
        return Results.Ok(products);
    }

    private static async Task<IResult> Search(
        ProductsService service,
        [FromQuery] string? name = null,
        [FromQuery] string? category = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int? minStock = null)
    {
        var products = await service.SearchAsync(name, category, minPrice, maxPrice, minStock);
        return Results.Ok(products);
    }

    private static async Task<IResult> GetById(int id, ProductsService service)
    {
        var product = await service.GetByIdAsync(id);
        if (product == null)
        {
            return Results.NotFound(new { error = "Product not found" });
        }
        return Results.Ok(product);
    }

    private static async Task<IResult> GetBySKU(string sku, ProductsService service)
    {
        var product = await service.GetBySKUAsync(sku);
        if (product == null)
        {
            return Results.NotFound(new { error = "Product not found" });
        }
        return Results.Ok(product);
    }

    // --- Protected Endpoints (require authentication) ---

    private static async Task<IResult> Create(
        CreateProductRequest request,
        ProductsService service,
        BizuitUserContext user)  // Injected automatically when authenticated
    {
        try
        {
            // Use authenticated user's username
            var productId = await service.CreateAsync(request, user.Username ?? "anonymous");
            return Results.Created($"/products/{productId}", new
            {
                productId,
                createdBy = user.Username
            });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Update(
        int id,
        UpdateProductRequest request,
        ProductsService service,
        BizuitUserContext user)  // Injected automatically when authenticated
    {
        try
        {
            var updated = await service.UpdateAsync(id, request);
            if (!updated)
            {
                return Results.NotFound(new { error = "Product not found" });
            }
            return Results.Ok(new
            {
                message = "Product updated",
                updatedBy = user.Username
            });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    // --- Admin-Only Endpoints ---

    private static async Task<IResult> Delete(
        int id,
        ProductsService service,
        BizuitUserContext user)  // Injected automatically when authenticated
    {
        var deleted = await service.DeleteAsync(id);
        if (!deleted)
        {
            return Results.NotFound(new { error = "Product not found" });
        }
        return Results.Ok(new
        {
            message = "Product deleted",
            deletedBy = user.Username
        });
    }

    // =========================================================================
    // ROLESETTINGS EXAMPLES - How to use RoleSettings in your plugins
    // =========================================================================

    /// <summary>
    /// Example: Get products filtered by user's allowed categories from RoleSettings.
    ///
    /// This demonstrates how to use RoleSettings to filter data based on user's
    /// business properties. The user's allowed categories come from their
    /// RoleSettings (e.g., a Vendor role might have Category=Electronics,Furniture).
    ///
    /// If the user has no Category settings, returns all products (admin behavior).
    /// </summary>
    private static async Task<IResult> GetMyAllowedProducts(
        ProductsService service,
        BizuitUserContext user)
    {
        // Get all "Category" values from user's role settings (across all roles)
        var allowedCategories = user.GetSettingValues("Category").ToList();

        if (!allowedCategories.Any())
        {
            // No category restrictions - return all products (admin/unrestricted user)
            var allProducts = await service.GetAllAsync();
            return Results.Ok(new
            {
                message = "No category restrictions - returning all products",
                username = user.Username,
                totalProducts = allProducts.Count(),
                products = allProducts
            });
        }

        // Filter products by user's allowed categories
        var products = await service.GetByCategoriesAsync(allowedCategories);

        return Results.Ok(new
        {
            message = $"Filtered by categories: {string.Join(", ", allowedCategories)}",
            username = user.Username,
            allowedCategories,
            totalProducts = products.Count(),
            products
        });
    }

    /// <summary>
    /// Example endpoint showing how to access the authenticated user's information.
    /// Returns username, tenant, roles, and role settings.
    ///
    /// BizuitUserContext is automatically injected when you add it as a parameter.
    ///
    /// IMPORTANT: Requires Bizuit.Backend.Core version 1.0.3+ for RoleSettings support.
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
            hasAnyVendorRole = user.HasAnyRole("Vendors", "Sellers"),
            hasAllRoles = user.HasAllRoles("Administrators", "BizuitAdmins"),

            // RoleSettings - Business properties per role from UserRoleSettings table:
            roleSettings = user.RoleSettings.Select(s => new { s.Role, s.Name, s.Value }),

            // Example: Get all values for a specific setting across all roles
            // Useful for filtering data based on user permissions
            allCategories = user.GetSettingValues("Category"),
            allVendorIds = user.GetSettingValues("VendorId"),

            // Example: Check if user has a specific setting value
            hasElectronics = user.HasSettingValue("Category", "Electronics")
        };

        return Task.FromResult(Results.Ok(info));
    }

    // =========================================================================
    // HELPER PATTERN: Get Required Value from RoleSettings
    // =========================================================================

    /// <summary>
    /// Example helper: Gets a required VendorId from the user's RoleSettings.
    /// Use this pattern when an endpoint REQUIRES a specific RoleSetting value.
    ///
    /// Returns an error result if the user doesn't have the VendorId configured.
    ///
    /// Usage in endpoint:
    ///   var vendorIdResult = GetVendorIdFromUser(user);
    ///   if (vendorIdResult.Error != null)
    ///   {
    ///       return vendorIdResult.Error;
    ///   }
    ///   // Use vendorIdResult.VendorId
    /// </summary>
    private static (int VendorId, IResult? Error) GetVendorIdFromUser(BizuitUserContext user)
    {
        // Try to get VendorId from Vendors role first
        var vendorIdValue = user.GetSettingValue("Vendors", "VendorId");

        // If not found in Vendors, try to get from any role that has VendorId
        if (string.IsNullOrEmpty(vendorIdValue))
        {
            vendorIdValue = user.GetSettingValues("VendorId").FirstOrDefault();
        }

        if (string.IsNullOrEmpty(vendorIdValue))
        {
            return (0, Results.BadRequest(new
            {
                error = "User does not have VendorId configured in RoleSettings",
                detail = "The user must have the 'VendorId' setting configured for the 'Vendors' role or any other role",
                username = user.Username,
                roles = user.Roles,
                roleSettings = user.RoleSettings.Select(s => new { s.Role, s.Name, s.Value })
            }));
        }

        if (!int.TryParse(vendorIdValue, out var vendorId))
        {
            return (0, Results.BadRequest(new
            {
                error = "VendorId value is not a valid integer",
                detail = $"The VendorId setting value '{vendorIdValue}' cannot be converted to an integer",
                username = user.Username
            }));
        }

        return (vendorId, null);
    }
}
