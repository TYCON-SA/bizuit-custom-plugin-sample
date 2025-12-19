using MyPlugin.Features.Items.Models;

namespace MyPlugin.Features.Items;

/// <summary>
/// Business logic service for Items.
/// </summary>
public class ItemsService
{
    private readonly ItemsRepository _repository;

    public ItemsService(ItemsRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Item>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<IEnumerable<Item>> SearchAsync(string? name, decimal? minPrice, bool? isActive)
    {
        return await _repository.SearchAsync(name, minPrice, isActive);
    }

    public async Task<Item?> GetByIdAsync(int itemId)
    {
        return await _repository.GetByIdAsync(itemId);
    }

    public async Task<int> CreateAsync(CreateItemRequest request)
    {
        // Add business validation here
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required");
        }

        if (request.Price < 0)
        {
            throw new ArgumentException("Price cannot be negative");
        }

        return await _repository.CreateAsync(request);
    }

    public async Task<bool> UpdateAsync(int itemId, UpdateItemRequest request)
    {
        // Verify item exists
        var existing = await _repository.GetByIdAsync(itemId);
        if (existing == null)
        {
            return false;
        }

        // Add business validation here
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required");
        }

        return await _repository.UpdateAsync(itemId, request);
    }

    public async Task<bool> DeleteAsync(int itemId)
    {
        return await _repository.DeleteAsync(itemId);
    }
}
