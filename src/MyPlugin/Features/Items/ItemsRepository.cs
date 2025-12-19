using System.Data;
using Bizuit.Backend.Core.Database;
using MyPlugin.Features.Items.Models;

namespace MyPlugin.Features.Items;

/// <summary>
/// Repository for Items using SafeQueryBuilder.
/// SQL Injection is IMPOSSIBLE.
/// </summary>
public class ItemsRepository : SafeRepository<Item>
{
    protected override string TableName => "Items";

    public ItemsRepository(IConnectionFactory connectionFactory)
        : base(connectionFactory.CreateConnection())
    {
    }

    /// <summary>
    /// Search items with optional filters.
    /// </summary>
    public async Task<IEnumerable<Item>> SearchAsync(string? name, decimal? minPrice, bool? isActive)
    {
        var query = Query();

        if (!string.IsNullOrEmpty(name))
        {
            query.WhereLike("Name", name);
        }

        if (minPrice.HasValue)
        {
            query.WhereGreaterOrEqual("Price", minPrice.Value);
        }

        if (isActive.HasValue)
        {
            query.WhereEquals("IsActive", isActive.Value);
        }

        query.OrderByDescending("CreatedAt");

        return await ExecuteAsync(query);
    }

    /// <summary>
    /// Create a new item.
    /// </summary>
    public async Task<int> CreateAsync(CreateItemRequest request)
    {
        var insert = Insert()
            .Set("Name", request.Name)
            .Set("Description", request.Description)
            .Set("Price", request.Price)
            .Set("Quantity", request.Quantity)
            .Set("IsActive", true)
            .Set("CreatedAt", DateTime.UtcNow);

        return await ExecuteWithIdentityAsync(insert);
    }

    /// <summary>
    /// Update an item.
    /// </summary>
    public async Task<bool> UpdateAsync(int itemId, UpdateItemRequest request)
    {
        var update = Update()
            .Set("Name", request.Name)
            .Set("Description", request.Description)
            .Set("Price", request.Price)
            .Set("Quantity", request.Quantity)
            .Set("IsActive", request.IsActive)
            .Set("UpdatedAt", DateTime.UtcNow)
            .WhereEquals("ItemId", itemId);

        var rows = await ExecuteAsync(update);
        return rows > 0;
    }

    /// <summary>
    /// Get item by ID.
    /// </summary>
    public async Task<Item?> GetByIdAsync(int itemId)
    {
        return await ExecuteSingleAsync(
            Query().WhereEquals("ItemId", itemId));
    }

    /// <summary>
    /// Delete item by ID.
    /// </summary>
    public async Task<bool> DeleteAsync(int itemId)
    {
        var rows = await ExecuteAsync(
            Delete().WhereEquals("ItemId", itemId));
        return rows > 0;
    }
}
