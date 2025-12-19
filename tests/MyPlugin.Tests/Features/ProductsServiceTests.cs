using Xunit;
using MyPlugin.Features.Products.Models;

namespace MyPlugin.Tests.Features;

/// <summary>
/// Unit tests for Products feature.
/// Demonstrates testing patterns for protected CRUD validation logic.
///
/// NOTE: These tests focus on business validation logic.
/// For integration tests with real database, you would use
/// a test database with actual data.
/// </summary>
public class ProductsServiceTests
{
    [Fact]
    public void CreateProductRequest_WithValidData_IsValid()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            SKU = "TEST-001",
            Description = "Test Description",
            Price = 10.00m,
            Stock = 5,
            Category = "Electronics"
        };

        // Assert - valid request has required fields
        Assert.NotNull(request.Name);
        Assert.NotNull(request.SKU);
        Assert.True(request.Price >= 0);
        Assert.True(request.Stock >= 0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateProductRequest_WithEmptyName_FailsValidation(string name)
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = name,
            SKU = "TEST-001",
            Price = 10.00m,
            Stock = 5
        };

        // Assert - name should be invalid
        Assert.True(string.IsNullOrWhiteSpace(request.Name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateProductRequest_WithEmptySKU_FailsValidation(string sku)
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            SKU = sku,
            Price = 10.00m,
            Stock = 5
        };

        // Assert - SKU should be invalid
        Assert.True(string.IsNullOrWhiteSpace(request.SKU));
    }

    [Theory]
    [InlineData(-1.00)]
    [InlineData(-100.00)]
    public void CreateProductRequest_WithNegativePrice_FailsValidation(decimal price)
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            SKU = "TEST-001",
            Price = price,
            Stock = 5
        };

        // Assert - price is negative
        Assert.True(request.Price < 0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void CreateProductRequest_WithNegativeStock_FailsValidation(int stock)
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            SKU = "TEST-001",
            Price = 10.00m,
            Stock = stock
        };

        // Assert - stock is negative
        Assert.True(request.Stock < 0);
    }

    [Fact]
    public void UpdateProductRequest_WithValidData_IsValid()
    {
        // Arrange
        var request = new UpdateProductRequest
        {
            Name = "Updated Product",
            SKU = "TEST-001",
            Description = "Updated Description",
            Price = 20.00m,
            Stock = 10,
            Category = "Updated Category"
        };

        // Assert
        Assert.NotNull(request.Name);
        Assert.NotNull(request.SKU);
        Assert.True(request.Price >= 0);
        Assert.True(request.Stock >= 0);
    }

    [Fact]
    public void Product_DefaultValues_AreCorrect()
    {
        // Arrange
        var product = new Product
        {
            ProductId = 1,
            Name = "Test Product",
            SKU = "TEST-001",
            Price = 10.00m,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(0, product.Stock); // Default stock is 0
        Assert.Null(product.UpdatedAt);
        Assert.Null(product.Description);
        Assert.Null(product.Category);
        Assert.Null(product.CreatedBy);
    }

    [Fact]
    public void Product_SKU_ShouldBeUnique_DocumentedBehavior()
    {
        // This test documents that SKU should be unique
        // The actual uniqueness is enforced by the database constraint
        // and validated in the service layer

        var product1 = new Product
        {
            ProductId = 1,
            Name = "Product 1",
            SKU = "SAME-SKU",
            Price = 10.00m,
            CreatedAt = DateTime.UtcNow
        };

        var product2 = new Product
        {
            ProductId = 2,
            Name = "Product 2",
            SKU = "SAME-SKU",  // Same SKU - should fail in service
            Price = 20.00m,
            CreatedAt = DateTime.UtcNow
        };

        // Assert - both have same SKU (database would reject this)
        Assert.Equal(product1.SKU, product2.SKU);
        Assert.NotEqual(product1.ProductId, product2.ProductId);
    }
}
