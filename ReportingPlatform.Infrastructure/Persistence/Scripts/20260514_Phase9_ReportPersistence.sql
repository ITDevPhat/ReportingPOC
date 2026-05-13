IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'rpt')
BEGIN
    EXEC('CREATE SCHEMA rpt');
END
GO

IF OBJECT_ID('rpt.ReportExecutions', 'U') IS NULL
BEGIN
    CREATE TABLE rpt.ReportExecutions
    (
        ExecutionId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ReportExecutions PRIMARY KEY,
        TemplateId UNIQUEIDENTIFIER NULL,
        Status NVARCHAR(50) NOT NULL,
        QueryJson NVARCHAR(MAX) NOT NULL,
        GeneratedSql NVARCHAR(MAX) NULL,
        ParametersJson NVARCHAR(MAX) NULL,
        ResultJson NVARCHAR(MAX) NULL,
        [RowCount] INT NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        StartedAt DATETIME2(3) NULL,
        CompletedAt DATETIME2(3) NULL,
        CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ReportExecutions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(100) NULL
    );
END
GO

IF OBJECT_ID('rpt.ReportTemplates', 'U') IS NULL
BEGIN
    CREATE TABLE rpt.ReportTemplates
    (
        TemplateId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ReportTemplates PRIMARY KEY,
        TemplateKey NVARCHAR(150) NOT NULL,
        Name NVARCHAR(255) NOT NULL,
        Description NVARCHAR(1000) NULL,
        BaseEntityKey NVARCHAR(100) NOT NULL,
        QueryJson NVARCHAR(MAX) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ReportTemplates_IsActive DEFAULT 1,
        CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ReportTemplates_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(100) NULL,
        UpdatedAt DATETIME2(3) NULL,
        UpdatedBy NVARCHAR(100) NULL
    );
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_ReportExecutions_ReportTemplates_TemplateId'
)
BEGIN
    ALTER TABLE rpt.ReportExecutions
        ADD CONSTRAINT FK_ReportExecutions_ReportTemplates_TemplateId
        FOREIGN KEY (TemplateId) REFERENCES rpt.ReportTemplates(TemplateId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ReportTemplates_TemplateKey' AND object_id = OBJECT_ID('rpt.ReportTemplates'))
BEGIN
    CREATE UNIQUE INDEX IX_ReportTemplates_TemplateKey ON rpt.ReportTemplates(TemplateKey);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ReportTemplates_IsActive' AND object_id = OBJECT_ID('rpt.ReportTemplates'))
BEGIN
    CREATE INDEX IX_ReportTemplates_IsActive ON rpt.ReportTemplates(IsActive);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ReportExecutions_TemplateId' AND object_id = OBJECT_ID('rpt.ReportExecutions'))
BEGIN
    CREATE INDEX IX_ReportExecutions_TemplateId ON rpt.ReportExecutions(TemplateId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ReportExecutions_Status' AND object_id = OBJECT_ID('rpt.ReportExecutions'))
BEGIN
    CREATE INDEX IX_ReportExecutions_Status ON rpt.ReportExecutions(Status);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ReportExecutions_CreatedAt' AND object_id = OBJECT_ID('rpt.ReportExecutions'))
BEGIN
    CREATE INDEX IX_ReportExecutions_CreatedAt ON rpt.ReportExecutions(CreatedAt);
END
GO
