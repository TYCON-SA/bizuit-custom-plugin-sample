using Bizuit.Backend.Core.Database;
using MyPlugin.Features.Products.Models;

namespace MyPlugin.Features.Products;

/// <summary>
/// Repository for Products using SafeQueryBuilder.
/// SQL Injection is IMPOSSIBLE.
///
/// Demonstrates advanced SafeQueryBuilder usage:
/// - Multiple WHERE conditions
/// - LIKE searches
/// - Comparison operators (>=, <=)
/// - WhereIn for category filtering
/// </summary>
public class ProductsRepository : SafeRepository<Product>
{
    protected override string TableName => "Products";

    public ProductsRepository(IConnectionFactory connectionFactory)
        : base(connectionFactory.CreateConnection())
    {
    }

    /// <summary>
    /// Search products with multiple filters.
    /// Demonstrates: WhereLike, WhereGreaterOrEqual, WhereLessOrEqual, WhereIn
    /// </summary>
    public async Task<IEnumerable<Product>> SearchAsync(
        string? name,
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        int? minStock)
    {
        var query = Query();

        // WhereLike - partial text search (adds % wildcards automatically)
        if (!string.IsNullOrEmpty(name))
        {
            query.WhereLike("Name", name);
        }

        // WhereEquals - exact match
        if (!string.IsNullOrEmpty(category))
        {
            query.WhereEquals("Category", category);
        }

        // WhereGreaterOrEqual - minimum value filter
        if (minPrice.HasValue)
        {
            query.WhereGreaterOrEqual("Price", minPrice.Value);
        }

        // WhereLessOrEqual - maximum value filter
        if (maxPrice.HasValue)
        {
            query.WhereLessOrEqual("Price", maxPrice.Value);
        }

        // WhereGreaterOrEqual for stock
        if (minStock.HasValue)
        {
            query.WhereGreaterOrEqual("Stock", minStock.Value);
        }

        query.OrderByDescending("CreatedAt");

        return await ExecuteAsync(query);
    }

    /// <summary>
    /// Get products by categories.
    /// Demonstrates: WhereIn for filtering by multiple values
    /// </summary>
    public async Task<IEnumerable<Product>> GetByCategoriesAsync(IEnumerable<string> categories)
    {
        var query = Query()
            .WhereIn("Category", categories.Cast<object>())
            .OrderBy("Category")
            .OrderBy("Name");

        return await ExecuteAsync(query);
    }

    /// <summary>
    /// Get product by SKU.
    /// </summary>
    public async Task<Product?> GetBySKUAsync(string sku)
    {
        return await ExecuteSingleAsync(
            Query().WhereEquals("SKU", sku));
    }

    /// <summary>
    /// Get product by ID.
    /// </summary>
    public async Task<Product?> GetByIdAsync(int productId)
    {
        return await ExecuteSingleAsync(
            Query().WhereEquals("ProductId", productId));
    }

    /// <summary>
    /// Create a new product.
    /// </summary>
    public async Task<int> CreateAsync(CreateProductRequest request, string createdBy)
    {
        var insert = Insert()
            .Set("Name", request.Name)
            .Set("SKU", request.SKU)
            .Set("Description", request.Description)
            .Set("Price", request.Price)
            .Set("Stock", request.Stock)
            .Set("Category", request.Category)
            .Set("CreatedBy", createdBy)
            .Set("CreatedAt", DateTime.UtcNow);

        return await ExecuteWithIdentityAsync(insert);
    }

    /// <summary>
    /// Update a product.
    /// </summary>
    public async Task<bool> UpdateAsync(int productId, UpdateProductRequest request)
    {
        var update = Update()
            .Set("Name", request.Name)
            .Set("SKU", request.SKU)
            .Set("Description", request.Description)
            .Set("Price", request.Price)
            .Set("Stock", request.Stock)
            .Set("Category", request.Category)
            .Set("UpdatedAt", DateTime.UtcNow)
            .WhereEquals("ProductId", productId);

        var rows = await ExecuteAsync(update);
        return rows > 0;
    }

    /// <summary>
    /// Delete a product.
    /// </summary>
    public async Task<bool> DeleteAsync(int productId)
    {
        var rows = await ExecuteAsync(
            Delete().WhereEquals("ProductId", productId));
        return rows > 0;
    }
}
