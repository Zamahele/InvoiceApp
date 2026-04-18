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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260412080505_InitialCreate'
)
BEGIN
    IF SCHEMA_ID(N'blacktech') IS NULL EXEC(N'CREATE SCHEMA [blacktech];');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260412080505_InitialCreate'
)
BEGIN
    CREATE TABLE [blacktech].[BankingDetails] (
        [Id] int NOT NULL IDENTITY,
        [BankName] nvarchar(max) NOT NULL,
        [AccountType] nvarchar(max) NULL,
        [AccountNumber] nvarchar(max) NULL,
        [BranchCode] nvarchar(max) NULL,
        CONSTRAINT [PK_BankingDetails] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260412080505_InitialCreate'
)
BEGIN
    CREATE TABLE [blacktech].[CompanySettings] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [AddressLine1] nvarchar(max) NULL,
        [AddressLine2] nvarchar(max) NULL,
        [City] nvarchar(max) NULL,
        [PostalCode] nvarchar(max) NULL,
        [Phone] nvarchar(max) NULL,
        CONSTRAINT [PK_CompanySettings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260412080505_InitialCreate'
)
BEGIN
    CREATE TABLE [blacktech].[Invoices] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceNumber] nvarchar(max) NOT NULL,
        [InvoiceDate] datetime2 NOT NULL,
        [IsReissue] bit NOT NULL,
        [ReissueDate] datetime2 NULL,
        [ClientName] nvarchar(max) NOT NULL,
        [ClientAddressLine1] nvarchar(max) NULL,
        [ClientAddressLine2] nvarchar(max) NULL,
        [ClientCity] nvarchar(max) NULL,
        [ClientPostalCode] nvarchar(max) NULL,
        [SubTotal] decimal(18,2) NOT NULL,
        [VATEnabled] bit NOT NULL,
        [VATRate] decimal(5,2) NOT NULL,
        [VATAmount] decimal(18,2) NOT NULL,
        [RetentionEnabled] bit NOT NULL,
        [RetentionType] nvarchar(max) NOT NULL,
        [RetentionPercentage] decimal(5,2) NOT NULL,
        [RetentionAmount] decimal(18,2) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260412080505_InitialCreate'
)
BEGIN
    CREATE TABLE [blacktech].[SavedRates] (
        [Id] int NOT NULL IDENTITY,
        [Description] nvarchar(max) NOT NULL,
        [Unit] nvarchar(max) NOT NULL,
        [Rate] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_SavedRates] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260412080505_InitialCreate'
)
BEGIN
    CREATE TABLE [blacktech].[LineItems] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceId] int NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Unit] nvarchar(max) NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [Rate] decimal(18,2) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_LineItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LineItems_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [blacktech].[Invoices] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260412080505_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LineItems_InvoiceId] ON [blacktech].[LineItems] ([InvoiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260412080505_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260412080505_InitialCreate', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418155417_AddRentTracking'
)
BEGIN
    CREATE TABLE [blacktech].[Rooms] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [TenantName] nvarchar(max) NOT NULL,
        [TenantPhone] nvarchar(max) NOT NULL,
        [RentAmount] decimal(18,2) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Rooms] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418155417_AddRentTracking'
)
BEGIN
    CREATE TABLE [blacktech].[RentPayments] (
        [Id] int NOT NULL IDENTITY,
        [RoomId] int NOT NULL,
        [Month] int NOT NULL,
        [Year] int NOT NULL,
        [AmountDue] decimal(18,2) NOT NULL,
        [AmountPaid] decimal(18,2) NULL,
        [PaidDate] datetime2 NULL,
        [IsPaid] bit NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_RentPayments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RentPayments_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [blacktech].[Rooms] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418155417_AddRentTracking'
)
BEGIN
    CREATE INDEX [IX_RentPayments_RoomId] ON [blacktech].[RentPayments] ([RoomId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418155417_AddRentTracking'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260418155417_AddRentTracking', N'8.0.0');
END;
GO

COMMIT;
GO

