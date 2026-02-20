-- ============================================
-- Rollback: 001_CreateItemsTable
-- Drops the Items table and its indexes
-- ============================================

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Items')
BEGIN
    DROP TABLE Items;
    PRINT 'Table Items dropped';
END
GO
