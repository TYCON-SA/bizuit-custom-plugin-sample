-- ============================================
-- MyPlugin Database Setup Script
-- ============================================
--
-- This script creates all tables required by the plugin.
-- Run this script against your SQL Server database before
-- uploading and activating the plugin.
--
-- Usage:
--   sqlcmd -S <server> -d <database> -U <user> -P <password> -i setup-database.sql
--
-- Or execute in SQL Server Management Studio (SSMS)
-- ============================================

PRINT '============================================';
PRINT 'MyPlugin Database Setup';
PRINT '============================================';
PRINT '';

-- ============================================
-- 1. Items Table (Public CRUD)
-- ============================================
PRINT 'Creating Items table...';

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

    CREATE INDEX IX_Items_Name ON Items(Name);
    CREATE INDEX IX_Items_IsActive ON Items(IsActive);
    CREATE INDEX IX_Items_CreatedAt ON Items(CreatedAt DESC);

    PRINT '  [OK] Table Items created';
END
ELSE
BEGIN
    PRINT '  [SKIP] Table Items already exists';
END

-- ============================================
-- 2. Products Table (Protected CRUD)
-- ============================================
PRINT 'Creating Products table...';

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

    CREATE UNIQUE INDEX UX_Products_SKU ON Products(SKU);
    CREATE INDEX IX_Products_Name ON Products(Name);
    CREATE INDEX IX_Products_Category ON Products(Category);
    CREATE INDEX IX_Products_Price ON Products(Price);
    CREATE INDEX IX_Products_Stock ON Products(Stock);
    CREATE INDEX IX_Products_CreatedAt ON Products(CreatedAt DESC);

    PRINT '  [OK] Table Products created';
END
ELSE
BEGIN
    PRINT '  [SKIP] Table Products already exists';
END

-- ============================================
-- 3. AuditLogs Table (NoTransaction)
-- ============================================
PRINT 'Creating AuditLogs table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE AuditLogs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Action NVARCHAR(50) NOT NULL,
        EntityType NVARCHAR(50),
        EntityId NVARCHAR(50),
        Username NVARCHAR(100),
        Details NVARCHAR(MAX),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_AuditLogs_Action ON AuditLogs(Action);
    CREATE INDEX IX_AuditLogs_EntityType ON AuditLogs(EntityType);
    CREATE INDEX IX_AuditLogs_Username ON AuditLogs(Username);
    CREATE INDEX IX_AuditLogs_CreatedAt ON AuditLogs(CreatedAt DESC);

    PRINT '  [OK] Table AuditLogs created';
END
ELSE
BEGIN
    PRINT '  [SKIP] Table AuditLogs already exists';
END

-- ============================================
-- Insert Sample Data (Optional)
-- ============================================
PRINT '';
PRINT 'Inserting sample data...';

-- Items sample data
IF NOT EXISTS (SELECT * FROM Items WHERE Name = 'Sample Item 1')
BEGIN
    INSERT INTO Items (Name, Description, Price, Quantity, IsActive)
    VALUES
        ('Sample Item 1', 'This is a sample item for testing', 10.00, 100, 1),
        ('Sample Item 2', 'Another sample item', 25.50, 50, 1),
        ('Sample Item 3', 'Third sample item', 99.99, 25, 1);
    PRINT '  [OK] Sample Items inserted';
END

-- Products sample data
IF NOT EXISTS (SELECT * FROM Products WHERE SKU = 'PROD-001')
BEGIN
    INSERT INTO Products (Name, SKU, Description, Price, Stock, Category, CreatedBy)
    VALUES
        ('Laptop Pro 15', 'PROD-001', 'High-performance laptop', 1299.99, 10, 'Electronics', 'admin'),
        ('Wireless Mouse', 'PROD-002', 'Ergonomic wireless mouse', 29.99, 100, 'Electronics', 'admin'),
        ('Office Chair', 'PROD-003', 'Comfortable office chair', 399.99, 25, 'Furniture', 'admin');
    PRINT '  [OK] Sample Products inserted';
END

-- AuditLogs sample data
IF NOT EXISTS (SELECT * FROM AuditLogs WHERE Action = 'PLUGIN_INSTALLED')
BEGIN
    INSERT INTO AuditLogs (Action, EntityType, EntityId, Username, Details)
    VALUES ('PLUGIN_INSTALLED', 'Plugin', 'myplugin', 'admin', 'Plugin MyPlugin v1.0.0 installed');
    PRINT '  [OK] Sample AuditLogs inserted';
END

-- ============================================
-- Summary
-- ============================================
PRINT '';
PRINT '============================================';
PRINT 'Setup Complete!';
PRINT '============================================';
PRINT '';
PRINT 'Tables created:';
PRINT '  - Items (public CRUD)';
PRINT '  - Products (protected CRUD)';
PRINT '  - AuditLogs (no transaction)';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Build and package the plugin: npm run package';
PRINT '2. Upload the ZIP file via admin panel';
PRINT '3. Configure the connection string';
PRINT '4. Activate the plugin';
PRINT '';
GO
