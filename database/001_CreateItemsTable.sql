-- ============================================
-- Items Table (Public CRUD - No authentication)
-- ============================================
-- This table is used by the Items feature which demonstrates
-- basic public CRUD operations without authentication.
-- ============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Items')
BEGIN
    CREATE TABLE Items (
        ItemId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500),
        Price DECIMAL(18,2) NOT NULL,
        Quantity INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL
    );

    -- Index for common searches
    CREATE INDEX IX_Items_Name ON Items(Name);
    CREATE INDEX IX_Items_IsActive ON Items(IsActive);
    CREATE INDEX IX_Items_CreatedAt ON Items(CreatedAt DESC);

    PRINT 'Table Items created successfully';
END
ELSE
BEGIN
    PRINT 'Table Items already exists';
END
GO

-- Insert sample data
IF NOT EXISTS (SELECT * FROM Items WHERE Name = 'Sample Item 1')
BEGIN
    INSERT INTO Items (Name, Description, Price, Quantity, IsActive)
    VALUES
        ('Sample Item 1', 'This is a sample item for testing', 10.00, 100, 1),
        ('Sample Item 2', 'Another sample item', 25.50, 50, 1),
        ('Sample Item 3', 'Third sample item', 99.99, 25, 1);

    PRINT 'Sample data inserted into Items';
END
GO
