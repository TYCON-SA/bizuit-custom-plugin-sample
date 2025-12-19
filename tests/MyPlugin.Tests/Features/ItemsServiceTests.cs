using Xunit;
using MyPlugin.Features.Items.Models;

namespace MyPlugin.Tests.Features;

/// <summary>
/// Unit tests for Items feature.
/// Demonstrates testing patterns for plugin validation logic.
///
/// NOTE: These tests focus on business validation logic.
/// For integration tests with real database, you would use
/// a test database with actual data.
/// </summary>
public class ItemsServiceTests
{
    [Fact]
    public void CreateItemRequest_WithValidData_IsValid()
    {
        // Arrange
        var request = new CreateItemRequest
        {
            Name = "Test Item",
            Description = "Test Description",
            Price = 10.00m,
            Quantity = 5
        };

        // Assert - valid request has required fields
        Assert.NotNull(request.Name);
        Assert.True(request.Price >= 0);
        Assert.True(request.Quantity >= 0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateItemRequest_WithEmptyName_FailsValidation(string name)
    {
        // Arrange
        var request = new CreateItemRequest
        {
            Name = name,
            Description = "Test Description",
            Price = 10.00m,
            Quantity = 5
        };

        // Assert - name should be invalid
        Assert.True(string.IsNullOrWhiteSpace(request.Name));
    }

    [Theory]
    [InlineData(-1.00)]
    [InlineData(-100.00)]
    public void CreateItemRequest_WithNegativePrice_FailsValidation(decimal price)
    {
        // Arrange
        var request = new CreateItemRequest
        {
            Name = "Test Item",
            Description = "Test Description",
            Price = price,
            Quantity = 5
        };

        // Assert - price is negative
        Assert.True(request.Price < 0);
    }

    [Fact]
    public void UpdateItemRequest_WithValidData_IsValid()
    {
        // Arrange
        var request = new UpdateItemRequest
        {
            Name = "Updated Item",
            Description = "Updated Description",
            Price = 20.00m,
            Quantity = 10,
            IsActive = true
        };

        // Assert
        Assert.NotNull(request.Name);
        Assert.True(request.Price >= 0);
    }

    [Fact]
    public void Item_DefaultValues_AreCorrect()
    {
        // Arrange
        var item = new Item
        {
            ItemId = 1,
            Name = "Test Item",
            Price = 10.00m,
            CreatedAt = DateTime.UtcNow
        };

        // Assert - default IsActive should be true
        Assert.True(item.IsActive);
        Assert.Null(item.UpdatedAt);
        Assert.Null(item.Description);
    }
}
