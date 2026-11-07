IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [GovernorateId] int NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [Actor] nvarchar(450) NOT NULL,
        [Action] nvarchar(80) NOT NULL,
        [Entity] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(100) NOT NULL,
        [RecordedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [Governorates] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Governorates] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [InventoryItems] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(160) NOT NULL,
        [Unit] nvarchar(40) NOT NULL,
        [Minimum] decimal(18,3) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_InventoryItems] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [RiskPolicies] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(120) NOT NULL,
        [Version] int NOT NULL,
        [ModerateFrom] int NOT NULL,
        [HighFrom] int NOT NULL,
        [CriticalFrom] int NOT NULL,
        [ApprovedBy] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RiskPolicies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [Events] (
        [Id] bigint NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [EventType] nvarchar(80) NOT NULL,
        [ThreatType] nvarchar(80) NOT NULL,
        [GovernorateId] int NOT NULL,
        [Location] nvarchar(500) NOT NULL,
        [Description] nvarchar(4000) NOT NULL,
        [Latitude] decimal(9,6) NULL,
        [Longitude] decimal(9,6) NULL,
        [CasesCount] int NULL,
        [DeathsCount] int NULL,
        [Status] int NOT NULL,
        [OccurredAt] datetime2 NULL,
        [DetectedAt] datetime2 NULL,
        [NotifiedAt] datetime2 NULL,
        [VerifiedAt] datetime2 NULL,
        [ConfirmedAt] datetime2 NULL,
        [AcknowledgedAt] datetime2 NULL,
        [FirstResponseAt] datetime2 NULL,
        [ClosedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Events] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Events_Coordinates] CHECK (([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180)),
        CONSTRAINT [CK_Events_Counts] CHECK (([CasesCount] IS NULL OR [CasesCount]>=0) AND ([DeathsCount] IS NULL OR [DeathsCount]>=0)),
        CONSTRAINT [FK_Events_Governorates_GovernorateId] FOREIGN KEY ([GovernorateId]) REFERENCES [Governorates] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [Personnel] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(160) NOT NULL,
        [EmployeeNumber] nvarchar(80) NOT NULL,
        [Organization] nvarchar(160) NOT NULL,
        [Specialty] nvarchar(120) NOT NULL,
        [Skills] nvarchar(120) NOT NULL,
        [Phone] nvarchar(80) NOT NULL,
        [GovernorateId] int NOT NULL,
        [IsAvailable] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Personnel] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Personnel_Governorates_GovernorateId] FOREIGN KEY ([GovernorateId]) REFERENCES [Governorates] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [Teams] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(160) NOT NULL,
        [GovernorateId] int NOT NULL,
        [State] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Teams] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Teams_Governorates_GovernorateId] FOREIGN KEY ([GovernorateId]) REFERENCES [Governorates] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [Warehouses] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(160) NOT NULL,
        [GovernorateId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouses_Governorates_GovernorateId] FOREIGN KEY ([GovernorateId]) REFERENCES [Governorates] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [AARReviews] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [Expected] nvarchar(4000) NOT NULL,
        [Actual] nvarchar(4000) NOT NULL,
        [WentWell] nvarchar(4000) NOT NULL,
        [Improvements] nvarchar(4000) NOT NULL,
        [RecordedBy] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AARReviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AARReviews_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [ActivationDecisions] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [Level] int NOT NULL,
        [Rationale] nvarchar(2000) NOT NULL,
        [ApprovedBy] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ActivationDecisions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ActivationDecisions_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [ClosureRequests] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [Rationale] nvarchar(2000) NOT NULL,
        [RequestedBy] nvarchar(450) NOT NULL,
        [ApprovedBy] nvarchar(450) NULL,
        [ResourcesSettled] bit NOT NULL,
        [EpidemiologyReviewed] bit NOT NULL,
        [FieldReviewed] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ClosureRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ClosureRequests_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [Contacts] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [Code] nvarchar(80) NOT NULL,
        [LastExposureAt] datetime2 NOT NULL,
        [FollowUpUntil] datetime2 NOT NULL,
        [RiskLevel] nvarchar(40) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Contacts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Contacts_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [EventTimeline] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [Action] nvarchar(80) NOT NULL,
        [Summary] nvarchar(2000) NOT NULL,
        [Actor] nvarchar(450) NOT NULL,
        [RecordedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EventTimeline] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EventTimeline_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [InboxMessages] (
        [Id] bigint NOT NULL IDENTITY,
        [Source] nvarchar(64) NOT NULL,
        [ExternalEventId] nvarchar(128) NOT NULL,
        [IdempotencyKey] nvarchar(128) NOT NULL,
        [PayloadHash] nvarchar(64) NOT NULL,
        [ProtectedPayload] nvarchar(max) NOT NULL,
        [EventId] bigint NOT NULL,
        [ReceivedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_InboxMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InboxMessages_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [IncidentAssignments] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [Position] nvarchar(120) NOT NULL,
        [PersonName] nvarchar(160) NOT NULL,
        [Organization] nvarchar(160) NOT NULL,
        [EndedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_IncidentAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_IncidentAssignments_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [IncidentDecisions] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [Text] nvarchar(4000) NOT NULL,
        [DecidedBy] nvarchar(450) NOT NULL,
        [CorrectsDecisionId] bigint NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_IncidentDecisions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_IncidentDecisions_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_IncidentDecisions_IncidentDecisions_CorrectsDecisionId] FOREIGN KEY ([CorrectsDecisionId]) REFERENCES [IncidentDecisions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [OutboxMessages] (
        [Id] bigint NOT NULL IDENTITY,
        [MessageId] uniqueidentifier NOT NULL,
        [EventId] bigint NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [Status] nvarchar(40) NOT NULL,
        [Attempts] int NOT NULL,
        [NextAttemptAt] datetime2 NOT NULL,
        [SentAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OutboxMessages_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [RiskAssessments] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [PolicyId] bigint NOT NULL,
        [Likelihood] int NOT NULL,
        [Impact] int NOT NULL,
        [Score] int NOT NULL,
        [Level] nvarchar(40) NOT NULL,
        [Rationale] nvarchar(2000) NOT NULL,
        [AssessedBy] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RiskAssessments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RiskAssessments_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RiskAssessments_RiskPolicies_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [RiskPolicies] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [Samples] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [Code] nvarchar(80) NOT NULL,
        [Type] nvarchar(100) NOT NULL,
        [CollectedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Samples] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Samples_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [SitRepReports] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [Version] int NOT NULL,
        [Summary] nvarchar(4000) NOT NULL,
        [Challenges] nvarchar(4000) NOT NULL,
        [NextActions] nvarchar(4000) NOT NULL,
        [Snapshot] nvarchar(max) NOT NULL,
        [PreparedBy] nvarchar(450) NOT NULL,
        [ApprovedBy] nvarchar(450) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SitRepReports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SitRepReports_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [Tasks] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [AssignedTo] nvarchar(450) NOT NULL,
        [DueAt] datetime2 NOT NULL,
        [State] int NOT NULL,
        [Progress] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Tasks] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Tasks_Progress] CHECK ([Progress] BETWEEN 0 AND 100),
        CONSTRAINT [FK_Tasks_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [Deployments] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [TeamId] bigint NOT NULL,
        [Mission] nvarchar(2000) NOT NULL,
        [MembersSnapshot] nvarchar(max) NOT NULL,
        [ApprovedBy] nvarchar(450) NOT NULL,
        [ArrivedAt] datetime2 NULL,
        [ReturnedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Deployments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Deployments_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Deployments_Teams_TeamId] FOREIGN KEY ([TeamId]) REFERENCES [Teams] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [TeamMembers] (
        [TeamId] bigint NOT NULL,
        [PersonId] bigint NOT NULL,
        [IsLeader] bit NOT NULL,
        CONSTRAINT [PK_TeamMembers] PRIMARY KEY ([TeamId], [PersonId]),
        CONSTRAINT [FK_TeamMembers_Personnel_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [Personnel] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TeamMembers_Teams_TeamId] FOREIGN KEY ([TeamId]) REFERENCES [Teams] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [InventoryBatches] (
        [Id] bigint NOT NULL IDENTITY,
        [WarehouseId] bigint NOT NULL,
        [ItemId] bigint NOT NULL,
        [BatchNumber] nvarchar(80) NOT NULL,
        [ExpiresAt] datetime2 NULL,
        [Balance] decimal(18,3) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_InventoryBatches] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Inventory_Balance] CHECK ([Balance]>=0),
        CONSTRAINT [FK_InventoryBatches_InventoryItems_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [InventoryItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InventoryBatches_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [ImprovementActions] (
        [Id] bigint NOT NULL IDENTITY,
        [ReviewId] bigint NOT NULL,
        [Action] nvarchar(2000) NOT NULL,
        [Owner] nvarchar(450) NOT NULL,
        [DueAt] datetime2 NOT NULL,
        [Progress] int NOT NULL,
        [Evidence] nvarchar(2000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ImprovementActions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ImprovementActions_AARReviews_ReviewId] FOREIGN KEY ([ReviewId]) REFERENCES [AARReviews] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [ContactFollowUps] (
        [Id] bigint NOT NULL IDENTITY,
        [ContactId] bigint NOT NULL,
        [Outcome] nvarchar(1000) NOT NULL,
        [RecordedBy] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ContactFollowUps] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContactFollowUps_Contacts_ContactId] FOREIGN KEY ([ContactId]) REFERENCES [Contacts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [LaboratoryResults] (
        [Id] bigint NOT NULL IDENTITY,
        [SampleId] bigint NOT NULL,
        [Result] nvarchar(2000) NOT NULL,
        [RecordedBy] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_LaboratoryResults] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LaboratoryResults_Samples_SampleId] FOREIGN KEY ([SampleId]) REFERENCES [Samples] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [SampleTransfers] (
        [Id] bigint NOT NULL IDENTITY,
        [SampleId] bigint NOT NULL,
        [From] nvarchar(160) NOT NULL,
        [To] nvarchar(160) NOT NULL,
        [RecordedBy] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SampleTransfers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SampleTransfers_Samples_SampleId] FOREIGN KEY ([SampleId]) REFERENCES [Samples] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [FieldReports] (
        [Id] bigint NOT NULL IDENTITY,
        [DeploymentId] bigint NOT NULL,
        [Activities] nvarchar(4000) NOT NULL,
        [Needs] nvarchar(2000) NOT NULL,
        [Latitude] decimal(9,6) NULL,
        [Longitude] decimal(9,6) NULL,
        [ReportedBy] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_FieldReports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FieldReports_Deployments_DeploymentId] FOREIGN KEY ([DeploymentId]) REFERENCES [Deployments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE TABLE [InventoryTransactions] (
        [Id] bigint NOT NULL IDENTITY,
        [BatchId] bigint NOT NULL,
        [EventId] bigint NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [ApprovedBy] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_InventoryTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryTransactions_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InventoryTransactions_InventoryBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [InventoryBatches] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AARReviews_EventId] ON [AARReviews] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ActivationDecisions_EventId] ON [ActivationDecisions] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ClosureRequests_EventId] ON [ClosureRequests] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ContactFollowUps_ContactId] ON [ContactFollowUps] ([ContactId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Contacts_EventId_Code] ON [Contacts] ([EventId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Deployments_EventId] ON [Deployments] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Deployments_TeamId] ON [Deployments] ([TeamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Events_GovernorateId_Status_CreatedAt] ON [Events] ([GovernorateId], [Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Events_PublicId] ON [Events] ([PublicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_EventTimeline_EventId_RecordedAt] ON [EventTimeline] ([EventId], [RecordedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_FieldReports_DeploymentId] ON [FieldReports] ([DeploymentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ImprovementActions_ReviewId] ON [ImprovementActions] ([ReviewId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_InboxMessages_EventId] ON [InboxMessages] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InboxMessages_Source_ExternalEventId] ON [InboxMessages] ([Source], [ExternalEventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InboxMessages_Source_IdempotencyKey] ON [InboxMessages] ([Source], [IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_IncidentAssignments_EventId] ON [IncidentAssignments] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_IncidentDecisions_CorrectsDecisionId] ON [IncidentDecisions] ([CorrectsDecisionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_IncidentDecisions_EventId] ON [IncidentDecisions] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_InventoryBatches_ItemId] ON [InventoryBatches] ([ItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InventoryBatches_WarehouseId_ItemId_BatchNumber] ON [InventoryBatches] ([WarehouseId], [ItemId], [BatchNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_BatchId] ON [InventoryTransactions] ([BatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_EventId] ON [InventoryTransactions] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_LaboratoryResults_SampleId] ON [LaboratoryResults] ([SampleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_OutboxMessages_EventId] ON [OutboxMessages] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OutboxMessages_MessageId] ON [OutboxMessages] ([MessageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_OutboxMessages_Status_NextAttemptAt] ON [OutboxMessages] ([Status], [NextAttemptAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Personnel_EmployeeNumber] ON [Personnel] ([EmployeeNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Personnel_GovernorateId] ON [Personnel] ([GovernorateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_RiskAssessments_EventId] ON [RiskAssessments] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_RiskAssessments_PolicyId] ON [RiskAssessments] ([PolicyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RiskPolicies_Version] ON [RiskPolicies] ([Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Samples_Code] ON [Samples] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Samples_EventId] ON [Samples] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_SampleTransfers_SampleId] ON [SampleTransfers] ([SampleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SitRepReports_EventId_Version] ON [SitRepReports] ([EventId], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Tasks_EventId] ON [Tasks] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_TeamMembers_PersonId] ON [TeamMembers] ([PersonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Teams_GovernorateId] ON [Teams] ([GovernorateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Warehouses_GovernorateId] ON [Warehouses] ([GovernorateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105210957_InitialSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105210957_InitialSchema', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [Warehouses] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [Teams] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [Tasks] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [SitRepReports] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [SampleTransfers] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [Samples] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [RiskPolicies] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [RiskAssessments] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [Personnel] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [OutboxMessages] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [LaboratoryResults] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [InventoryTransactions] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [InventoryItems] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [InventoryBatches] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [IncidentDecisions] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [IncidentAssignments] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [ImprovementActions] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [FieldReports] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [Events] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [Deployments] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [Contacts] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [ContactFollowUps] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [ClosureRequests] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [ActivationDecisions] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    ALTER TABLE [AARReviews] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211302_EventConcurrencyStamp'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105211302_EventConcurrencyStamp', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211452_TrainingDataSeparation'
)
BEGIN
    ALTER TABLE [Events] ADD [IsTraining] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105211452_TrainingDataSeparation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105211452_TrainingDataSeparation', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105212401_CoordinationAndSla'
)
BEGIN
    CREATE TABLE [EventAttachments] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [OriginalName] nvarchar(240) NOT NULL,
        [StorageKey] nvarchar(100) NOT NULL,
        [ContentType] nvarchar(80) NOT NULL,
        [SizeBytes] bigint NOT NULL,
        [Sha256] nvarchar(64) NOT NULL,
        [UploadedBy] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EventAttachments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EventAttachments_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105212401_CoordinationAndSla'
)
BEGIN
    CREATE TABLE [EventMessages] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [Text] nvarchar(2000) NOT NULL,
        [Sender] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EventMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EventMessages_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105212401_CoordinationAndSla'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [Text] nvarchar(1000) NOT NULL,
        [Severity] nvarchar(40) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105212401_CoordinationAndSla'
)
BEGIN
    CREATE TABLE [SlaDefinitions] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Stage] nvarchar(40) NOT NULL,
        [Version] int NOT NULL,
        [TargetMinutes] int NOT NULL,
        [EscalateEveryMinutes] int NOT NULL,
        [ApprovedBy] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SlaDefinitions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105212401_CoordinationAndSla'
)
BEGIN
    CREATE TABLE [NotificationReads] (
        [NotificationId] bigint NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [ReadAt] datetime2 NOT NULL,
        CONSTRAINT [PK_NotificationReads] PRIMARY KEY ([NotificationId], [UserId]),
        CONSTRAINT [FK_NotificationReads_Notifications_NotificationId] FOREIGN KEY ([NotificationId]) REFERENCES [Notifications] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105212401_CoordinationAndSla'
)
BEGIN
    CREATE TABLE [EventSlas] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [DefinitionId] bigint NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NULL,
        [Compliant] bit NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EventSlas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EventSlas_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EventSlas_SlaDefinitions_DefinitionId] FOREIGN KEY ([DefinitionId]) REFERENCES [SlaDefinitions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105212401_CoordinationAndSla'
)
BEGIN
    CREATE TABLE [SlaEscalations] (
        [Id] bigint NOT NULL IDENTITY,
        [EventSlaId] bigint NOT NULL,
        [Level] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SlaEscalations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SlaEscalations_EventSlas_EventSlaId] FOREIGN KEY ([EventSlaId]) REFERENCES [EventSlas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105212401_CoordinationAndSla'
)
BEGIN
    CREATE INDEX [IX_EventAttachments_EventId] ON [EventAttachments] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105212401_CoordinationAndSla'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EventAttachments_StorageKey] ON [EventAttachments] ([StorageKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105212401_CoordinationAndSla'
)
BEGIN
    CREATE INDEX [IX_EventMessages_EventId] ON [EventMessages] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105212401_CoordinationAndSla'
)
BEGIN
    CREATE INDEX [IX_EventSlas_DefinitionId] ON [EventSlas] ([DefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105212401_CoordinationAndSla'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EventSlas_EventId_DefinitionId] ON [EventSlas] ([EventId], [DefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105212401_CoordinationAndSla'
)
BEGIN
    CREATE INDEX [IX_Notifications_EventId] ON [Notifications] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105212401_CoordinationAndSla'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SlaDefinitions_Stage_Version] ON [SlaDefinitions] ([Stage], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105212401_CoordinationAndSla'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SlaEscalations_EventSlaId_Level] ON [SlaEscalations] ([EventSlaId], [Level]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105212401_CoordinationAndSla'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105212401_CoordinationAndSla', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    ALTER TABLE [FieldReports] ADD [ClientRequestId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    ALTER TABLE [ClosureRequests] ADD [Exceptional] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    ALTER TABLE [ClosureRequests] ADD [PolicyId] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    CREATE TABLE [ClosurePolicies] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(120) NOT NULL,
        [Version] int NOT NULL,
        [RequireCompletedTasks] bit NOT NULL,
        [RequireApprovedReport] bit NOT NULL,
        [RequireResourcesSettled] bit NOT NULL,
        [RequireEpidemiologyReview] bit NOT NULL,
        [RequireFieldReview] bit NOT NULL,
        [AllowExceptional] bit NOT NULL,
        [ApprovedBy] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ClosurePolicies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    CREATE TABLE [EarlyResponseMilestones] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [Action] int NOT NULL,
        [Date] date NULL,
        [NotApplicable] bit NOT NULL,
        [Evidence] nvarchar(2000) NOT NULL,
        [RecordedBy] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EarlyResponseMilestones] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EarlyResponseMilestones_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    CREATE TABLE [KpiDateRecords] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [Emergence] date NULL,
        [Detection] date NULL,
        [Notification] date NULL,
        [Excluded] bit NOT NULL,
        [Evidence] nvarchar(2000) NOT NULL,
        [RecordedBy] nvarchar(450) NOT NULL,
        [MethodologyVersion] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_KpiDateRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KpiDateRecords_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    CREATE TABLE [SupplyRequests] (
        [Id] bigint NOT NULL IDENTITY,
        [EventId] bigint NOT NULL,
        [ItemId] bigint NOT NULL,
        [Quantity] decimal(18,3) NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [RequestedBy] nvarchar(450) NOT NULL,
        [ApprovedBy] nvarchar(450) NULL,
        [BatchId] bigint NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SupplyRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SupplyRequests_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupplyRequests_InventoryBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [InventoryBatches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupplyRequests_InventoryItems_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [InventoryItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    CREATE TABLE [Vehicles] (
        [Id] bigint NOT NULL IDENTITY,
        [PlateNumber] nvarchar(80) NOT NULL,
        [Type] nvarchar(120) NOT NULL,
        [GovernorateId] int NOT NULL,
        [Available] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Vehicles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Vehicles_Governorates_GovernorateId] FOREIGN KEY ([GovernorateId]) REFERENCES [Governorates] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    CREATE TABLE [VehicleAssignments] (
        [Id] bigint NOT NULL IDENTITY,
        [VehicleId] bigint NOT NULL,
        [DeploymentId] bigint NOT NULL,
        [AssignedBy] nvarchar(450) NOT NULL,
        [ReturnedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_VehicleAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VehicleAssignments_Deployments_DeploymentId] FOREIGN KEY ([DeploymentId]) REFERENCES [Deployments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VehicleAssignments_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_FieldReports_ClientRequestId] ON [FieldReports] ([ClientRequestId]) WHERE [ClientRequestId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    CREATE INDEX [IX_ClosureRequests_PolicyId] ON [ClosureRequests] ([PolicyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    CREATE INDEX [IX_EarlyResponseMilestones_EventId_Action_Id] ON [EarlyResponseMilestones] ([EventId], [Action], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    CREATE INDEX [IX_KpiDateRecords_EventId_Id] ON [KpiDateRecords] ([EventId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    CREATE INDEX [IX_SupplyRequests_BatchId] ON [SupplyRequests] ([BatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    CREATE INDEX [IX_SupplyRequests_EventId] ON [SupplyRequests] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    CREATE INDEX [IX_SupplyRequests_ItemId] ON [SupplyRequests] ([ItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    CREATE INDEX [IX_VehicleAssignments_DeploymentId] ON [VehicleAssignments] ([DeploymentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    CREATE INDEX [IX_VehicleAssignments_VehicleId] ON [VehicleAssignments] ([VehicleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    CREATE INDEX [IX_Vehicles_GovernorateId] ON [Vehicles] ([GovernorateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Vehicles_PlateNumber] ON [Vehicles] ([PlateNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    ALTER TABLE [ClosureRequests] ADD CONSTRAINT [FK_ClosureRequests_ClosurePolicies_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [ClosurePolicies] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213155_AnalyticsLogisticsAndPolicies'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105213155_AnalyticsLogisticsAndPolicies', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213554_ReliableSyncAndSlaScheduling'
)
BEGIN
    ALTER TABLE [EventSlas] ADD [LastEvaluatedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213554_ReliableSyncAndSlaScheduling'
)
BEGIN
    CREATE TABLE [EbsSyncStates] (
        [Id] bigint NOT NULL IDENTITY,
        [ConnectorCode] nvarchar(40) NOT NULL,
        [Enabled] bit NOT NULL,
        [CursorLocal] datetime2 NOT NULL,
        [CursorVerificationId] int NOT NULL,
        [LastSuccessAt] datetime2 NULL,
        [LastError] nvarchar(200) NOT NULL,
        [EnabledBy] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EbsSyncStates] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213554_ReliableSyncAndSlaScheduling'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EbsSyncStates_ConnectorCode] ON [EbsSyncStates] ([ConnectorCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105213554_ReliableSyncAndSlaScheduling'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105213554_ReliableSyncAndSlaScheduling', N'10.0.9');
END;

COMMIT;
GO

