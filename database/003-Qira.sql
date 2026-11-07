BEGIN TRANSACTION;
CREATE TABLE [QiraDefinitions] (
    [Id] bigint NOT NULL IDENTITY,
    [Code] nvarchar(80) NOT NULL,
    [SourceUrl] nvarchar(600) NOT NULL,
    [Hash] nvarchar(64) NOT NULL,
    [DocumentJson] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_QiraDefinitions] PRIMARY KEY ([Id])
);

CREATE TABLE [QiraNationalPolicies] (
    [Id] bigint NOT NULL IDENTITY,
    [Version] int NOT NULL,
    [Name] nvarchar(160) NOT NULL,
    [Reference] nvarchar(2000) NOT NULL,
    [HazardsJson] nvarchar(max) NOT NULL,
    [ActionsJson] nvarchar(max) NOT NULL,
    [ApprovedBy] nvarchar(450) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_QiraNationalPolicies] PRIMARY KEY ([Id])
);

CREATE TABLE [QiraAssessments] (
    [Id] bigint NOT NULL IDENTITY,
    [EventId] bigint NOT NULL,
    [DefinitionId] bigint NOT NULL,
    [PolicyId] bigint NULL,
    [PreviousId] bigint NULL,
    [Revision] int NOT NULL,
    [State] int NOT NULL,
    [AssessedBy] nvarchar(450) NOT NULL,
    [AssessorName] nvarchar(160) NOT NULL,
    [Scope] nvarchar(2000) NOT NULL,
    [Population] nvarchar(1000) NOT NULL,
    [TimeWindow] nvarchar(500) NOT NULL,
    [Hazard] nvarchar(160) NOT NULL,
    [NovelHazard] bit NOT NULL,
    [Hospitalizations] int NULL,
    [IcuAdmissions] int NULL,
    [Risk] int NULL,
    [Confidence] int NULL,
    [ConfidenceReason] nvarchar(2000) NOT NULL,
    [Uncertainties] nvarchar(4000) NOT NULL,
    [ResultJson] nvarchar(max) NOT NULL,
    [CompletedAt] datetime2 NULL,
    [ReviewedBy] nvarchar(450) NULL,
    [ReviewedAt] datetime2 NULL,
    [ReviewNote] nvarchar(2000) NOT NULL,
    [ApprovedBy] nvarchar(450) NULL,
    [ApprovedAt] datetime2 NULL,
    [ApprovalNote] nvarchar(2000) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_QiraAssessments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QiraAssessments_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QiraAssessments_QiraAssessments_PreviousId] FOREIGN KEY ([PreviousId]) REFERENCES [QiraAssessments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QiraAssessments_QiraDefinitions_DefinitionId] FOREIGN KEY ([DefinitionId]) REFERENCES [QiraDefinitions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QiraAssessments_QiraNationalPolicies_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [QiraNationalPolicies] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [QiraActions] (
    [Id] bigint NOT NULL IDENTITY,
    [AssessmentId] bigint NOT NULL,
    [Title] nvarchar(1000) NOT NULL,
    [Organization] nvarchar(450) NOT NULL,
    [Priority] int NOT NULL,
    [DueAt] datetime2 NOT NULL,
    [TaskId] bigint NULL,
    [Notes] nvarchar(2000) NOT NULL,
    [SharedBy] nvarchar(450) NULL,
    [SharedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_QiraActions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QiraActions_QiraAssessments_AssessmentId] FOREIGN KEY ([AssessmentId]) REFERENCES [QiraAssessments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QiraActions_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [QiraAnswers] (
    [Id] bigint NOT NULL IDENTITY,
    [AssessmentId] bigint NOT NULL,
    [Node] nvarchar(20) NOT NULL,
    [Choice] int NOT NULL,
    [Active] bit NOT NULL,
    [Notes] nvarchar(2000) NOT NULL,
    [Source] nvarchar(2000) NOT NULL,
    [Evidence] nvarchar(4000) NOT NULL,
    [AttachmentId] bigint NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_QiraAnswers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QiraAnswers_EventAttachments_AttachmentId] FOREIGN KEY ([AttachmentId]) REFERENCES [EventAttachments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QiraAnswers_QiraAssessments_AssessmentId] FOREIGN KEY ([AssessmentId]) REFERENCES [QiraAssessments] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [QiraHistories] (
    [Id] bigint NOT NULL IDENTITY,
    [AssessmentId] bigint NOT NULL,
    [Actor] nvarchar(450) NOT NULL,
    [Action] nvarchar(80) NOT NULL,
    [Details] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_QiraHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QiraHistories_QiraAssessments_AssessmentId] FOREIGN KEY ([AssessmentId]) REFERENCES [QiraAssessments] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_QiraActions_AssessmentId] ON [QiraActions] ([AssessmentId]);

CREATE INDEX [IX_QiraActions_TaskId] ON [QiraActions] ([TaskId]);

CREATE UNIQUE INDEX [IX_QiraAnswers_AssessmentId_Node] ON [QiraAnswers] ([AssessmentId], [Node]);

CREATE INDEX [IX_QiraAnswers_AttachmentId] ON [QiraAnswers] ([AttachmentId]);

CREATE INDEX [IX_QiraAssessments_DefinitionId] ON [QiraAssessments] ([DefinitionId]);

CREATE UNIQUE INDEX [IX_QiraAssessments_EventId_Revision] ON [QiraAssessments] ([EventId], [Revision]);

CREATE INDEX [IX_QiraAssessments_PolicyId] ON [QiraAssessments] ([PolicyId]);

CREATE INDEX [IX_QiraAssessments_PreviousId] ON [QiraAssessments] ([PreviousId]);

CREATE UNIQUE INDEX [IX_QiraDefinitions_Code] ON [QiraDefinitions] ([Code]);

CREATE INDEX [IX_QiraHistories_AssessmentId] ON [QiraHistories] ([AssessmentId]);

CREATE UNIQUE INDEX [IX_QiraNationalPolicies_Version] ON [QiraNationalPolicies] ([Version]);

IF DATABASE_PRINCIPAL_ID(N'pheoc_runtime') IS NOT NULL
BEGIN
  DENY UPDATE, DELETE ON OBJECT::dbo.QiraDefinitions TO pheoc_runtime;
  DENY UPDATE, DELETE ON OBJECT::dbo.QiraNationalPolicies TO pheoc_runtime;
  DENY UPDATE, DELETE ON OBJECT::dbo.QiraHistories TO pheoc_runtime;
END;
INSERT INTO dbo.AspNetRoleClaims(RoleId,ClaimType,ClaimValue)
SELECT r.Id, N'permission', p.Value
FROM dbo.AspNetRoles r
CROSS JOIN (VALUES(N'QIRA.Review'),(N'QIRA.Reassess')) p(Value)
WHERE r.NormalizedName=N'SYSTEM ADMINISTRATOR'
AND NOT EXISTS(SELECT 1 FROM dbo.AspNetRoleClaims c WHERE c.RoleId=r.Id AND c.ClaimType=N'permission' AND c.ClaimValue=p.Value);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261106211504_QiraRiskAssessment', N'10.0.9');

COMMIT;
GO

