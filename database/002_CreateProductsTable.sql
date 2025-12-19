-- ============================================
-- Products Table (Protected CRUD - Requires authentication)
-- ============================================
-- This table is used by the Products feature which demonstrates
-- protected CRUD operations with authentication and authorization.
--
-- - GET endpoints: Public
-- - POST/PUT: Requires authentication
-- - DELETE: Requires admin role
-- ============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
BEGIN
    CREATE TABLE Products (
        ProductId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        SKU NVARCHAR(50) NOT NULL,
        Description NVARCHAR(500),
        Price DECIMAL(18,2) NOT NULL,
        Stock INT NOT NULL DEFAULT 0,
        Category NVARCHAR(50),
        CreatedBy NVARCHAR(100),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL
    );

    -- Unique constraint on SKU
    CREATE UNIQUE INDEX UX_Products_SKU ON Products(SKU);

    -- Indexes for common searches
    CREATE INDEX IX_Products_Name ON Products(Name);
    CREATE INDEX IX_Products_Category ON Products(Category);
    CREATE INDEX IX_Products_Price ON Products(Price);
    CREATE INDEX IX_Products_Stock ON Products(Stock);
    CREATE INDEX IX_Products_CreatedAt ON Products(CreatedAt DESC);

    PRINT 'Table Products created successfully';
END
ELSE
BEGIN
    PRINT 'Table Products already exists';
END
GO

-- Insert sample data
IF NOT EXISTS (SELECT * FROM Products WHERE SKU = 'PROD-001')
BEGIN
    INSERT INTO Products (Name, SKU, Description, Price, Stock, Category, CreatedBy)
    VALUES
        ('Laptop Pro 15', 'PROD-001', 'High-performance laptop for professionals', 1299.99, 10, 'Electronics', 'admin'),
        ('Wireless Mouse', 'PROD-002', 'Ergonomic wireless mouse', 29.99, 100, 'Electronics', 'admin'),
        ('Office Chair', 'PROD-003', 'Comfortable ergonomic office chair', 399.99, 25, 'Furniture', 'admin'),
        ('Standing Desk', 'PROD-004', 'Adjustable height standing desk', 599.99, 15, 'Furniture', 'admin'),
        ('USB-C Hub', 'PROD-005', '7-in-1 USB-C hub with HDMI', 49.99, 50, 'Electronics', 'admin');

    PRINT 'Sample data inserted into Products';
END
GO
