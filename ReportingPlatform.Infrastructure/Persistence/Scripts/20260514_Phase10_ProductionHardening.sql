IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'rpt')
BEGIN
    EXEC('CREATE SCHEMA rpt');
END
GO

IF OBJECT_ID('rpt.ReportAuditLogs', 'U') IS NULL
BEGIN
    CREATE TABLE rpt.ReportAuditLogs
    (
        AuditLogId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ReportAuditLogs PRIMARY KEY,
        EventType NVARCHAR(100) NOT NULL,
        EntityType NVARCHAR(100) NULL,
        EntityId NVARCHAR(100) NULL,
        UserId NVARCHAR(100) NULL,
        RequestPath NVARCHAR(500) NULL,
        RequestMethod NVARCHAR(20) NULL,
        QueryHash NVARCHAR(128) NULL,
        ExecutionId UNIQUEIDENTIFIER NULL,
        TemplateId UNIQUEIDENTIFIER NULL,
        Status NVARCHAR(50) NULL,
        DurationMs BIGINT NULL,
        MetadataJson NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ReportAuditLogs_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ReportAuditLogs_EventType' AND object_id = OBJECT_ID('rpt.ReportAuditLogs'))
BEGIN
    CREATE INDEX IX_ReportAuditLogs_EventType ON rpt.ReportAuditLogs(EventType);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ReportAuditLogs_UserId' AND object_id = OBJECT_ID('rpt.ReportAuditLogs'))
BEGIN
    CREATE INDEX IX_ReportAuditLogs_UserId ON rpt.ReportAuditLogs(UserId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ReportAuditLogs_ExecutionId' AND object_id = OBJECT_ID('rpt.ReportAuditLogs'))
BEGIN
    CREATE INDEX IX_ReportAuditLogs_ExecutionId ON rpt.ReportAuditLogs(ExecutionId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ReportAuditLogs_TemplateId' AND object_id = OBJECT_ID('rpt.ReportAuditLogs'))
BEGIN
    CREATE INDEX IX_ReportAuditLogs_TemplateId ON rpt.ReportAuditLogs(TemplateId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ReportAuditLogs_CreatedAt' AND object_id = OBJECT_ID('rpt.ReportAuditLogs'))
BEGIN
    CREATE INDEX IX_ReportAuditLogs_CreatedAt ON rpt.ReportAuditLogs(CreatedAt);
END
GO
