-- ============================================
-- Rollback: 002_CreateProductsTable
-- Drops the Products table and its indexes
-- ============================================

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
BEGIN
    DROP TABLE Products;
    PRINT 'Table Products dropped';
END
GO
