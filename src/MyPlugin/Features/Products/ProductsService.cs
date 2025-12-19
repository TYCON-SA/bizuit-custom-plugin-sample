using MyPlugin.Features.Products.Models;

namespace MyPlugin.Features.Products;

/// <summary>
/// Business logic service for Products.
/// Demonstrates validation and business rules.
/// </summary>
public class ProductsService
{
    private readonly ProductsRepository _repository;

    public ProductsService(ProductsRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<IEnumerable<Product>> SearchAsync(
        string? name,
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        int? minStock)
    {
        return await _repository.SearchAsync(name, category, minPrice, maxPrice, minStock);
    }

    public async Task<IEnumerable<Product>> GetByCategoriesAsync(IEnumerable<string> categories)
    {
        return await _repository.GetByCategoriesAsync(categories);
    }

    public async Task<Product?> GetByIdAsync(int productId)
    {
        return await _repository.GetByIdAsync(productId);
    }

    public async Task<Product?> GetBySKUAsync(string sku)
    {
        return await _repository.GetBySKUAsync(sku);
    }

    public async Task<int> CreateAsync(CreateProductRequest request, string createdBy)
    {
        // Business validation
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required");
        }

        if (string.IsNullOrWhiteSpace(request.SKU))
        {
            throw new ArgumentException("SKU is required");
        }

        if (request.Price < 0)
        {
            throw new ArgumentException("Price cannot be negative");
        }

        if (request.Stock < 0)
        {
            throw new ArgumentException("Stock cannot be negative");
        }

        // Check SKU uniqueness
        var existing = await _repository.GetBySKUAsync(request.SKU);
        if (existing != null)
        {
            throw new ArgumentException($"Product with SKU '{request.SKU}' already exists");
        }

        return await _repository.CreateAsync(request, createdBy);
    }

    public async Task<bool> UpdateAsync(int productId, UpdateProductRequest request, string? currentSKU = null)
    {
        // Verify product exists
        var existing = await _repository.GetByIdAsync(productId);
        if (existing == null)
        {
            return false;
        }

        // Business validation
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required");
        }

        if (string.IsNullOrWhiteSpace(request.SKU))
        {
            throw new ArgumentException("SKU is required");
        }

        if (request.Price < 0)
        {
            throw new ArgumentException("Price cannot be negative");
        }

        // Check SKU uniqueness (if changed)
        if (request.SKU != existing.SKU)
        {
            var withSameSKU = await _repository.GetBySKUAsync(request.SKU);
            if (withSameSKU != null)
            {
                throw new ArgumentException($"Product with SKU '{request.SKU}' already exists");
            }
        }

        return await _repository.UpdateAsync(productId, request);
    }

    public async Task<bool> DeleteAsync(int productId)
    {
        return await _repository.DeleteAsync(productId);
    }
}
