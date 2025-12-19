namespace MyPlugin.Features.Products.Models;

/// <summary>
/// Product entity model.
/// </summary>
public class Product
{
    public int ProductId { get; set; }
    public required string Name { get; set; }
    public required string SKU { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? Category { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request to create a product.
/// </summary>
public class CreateProductRequest
{
    public required string Name { get; set; }
    public required string SKU { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? Category { get; set; }
}

/// <summary>
/// Request to update a product.
/// </summary>
public class UpdateProductRequest
{
    public required string Name { get; set; }
    public required string SKU { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? Category { get; set; }
}
