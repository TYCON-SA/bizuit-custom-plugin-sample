-- ============================================
-- AuditLogs Table (NoTransaction - Fire-and-forget logging)
-- ============================================
-- This table is used by the AuditLogs feature which demonstrates
-- endpoints that opt-out of automatic transactions using .NoTransaction()
--
-- Use cases:
-- - Fire-and-forget logging operations
-- - Audit entries that should persist even if main operation fails
-- - High-performance logging without transaction overhead
-- ============================================

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

    -- Indexes for common searches
    CREATE INDEX IX_AuditLogs_Action ON AuditLogs(Action);
    CREATE INDEX IX_AuditLogs_EntityType ON AuditLogs(EntityType);
    CREATE INDEX IX_AuditLogs_Username ON AuditLogs(Username);
    CREATE INDEX IX_AuditLogs_CreatedAt ON AuditLogs(CreatedAt DESC);

    PRINT 'Table AuditLogs created successfully';
END
ELSE
BEGIN
    PRINT 'Table AuditLogs already exists';
END
GO

-- Insert sample data
IF NOT EXISTS (SELECT * FROM AuditLogs WHERE Action = 'PLUGIN_INSTALLED')
BEGIN
    INSERT INTO AuditLogs (Action, EntityType, EntityId, Username, Details)
    VALUES
        ('PLUGIN_INSTALLED', 'Plugin', 'myplugin', 'admin', 'Plugin MyPlugin v1.0.0 installed'),
        ('USER_LOGIN', 'User', 'admin', 'admin', 'User logged in successfully'),
        ('PRODUCT_CREATED', 'Product', 'PROD-001', 'admin', 'Created product: Laptop Pro 15');

    PRINT 'Sample data inserted into AuditLogs';
END
GO
