namespace MyPlugin.Features.Items.Models;

/// <summary>
/// Item entity model.
/// </summary>
public class Item
{
    public int ItemId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request to create an item.
/// </summary>
public class CreateItemRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
/// Request to update an item.
/// </summary>
public class UpdateItemRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public bool IsActive { get; set; }
}
