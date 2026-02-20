-- ============================================
-- Rollback: 003_CreateAuditLogsTable
-- Drops the AuditLogs table and its indexes
-- ============================================

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    DROP TABLE AuditLogs;
    PRINT 'Table AuditLogs dropped';
END
GO
