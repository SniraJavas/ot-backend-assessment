
USE master;
GO

IF DB_ID(N'OT_Assessment_DB') IS NULL
BEGIN
    CREATE DATABASE OT_Assessment_DB;
END;
GO

USE OT_Assessment_DB;
GO

/*=========================================================
 1. PLAYERS
=========================================================*/

IF OBJECT_ID('dbo.Players', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Players
    (
        AccountId UNIQUEIDENTIFIER NOT NULL,
        Username NVARCHAR(100) NOT NULL,

        CONSTRAINT PK_Players
            PRIMARY KEY CLUSTERED (AccountId)
    );
END;
GO

/*=========================================================
 2. PROVIDERS
=========================================================*/

IF OBJECT_ID('dbo.Providers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Providers
    (
        ProviderId INT IDENTITY(1,1) NOT NULL,
        ProviderName NVARCHAR(200) NOT NULL,

        CONSTRAINT PK_Providers
            PRIMARY KEY CLUSTERED (ProviderId),

        CONSTRAINT UQ_Providers_ProviderName
            UNIQUE (ProviderName)
    );
END;
GO

/*=========================================================
 3. GAMES
=========================================================*/

IF OBJECT_ID('dbo.Games', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Games
    (
        GameId INT IDENTITY(1,1) NOT NULL,
        ProviderId INT NOT NULL,
        GameName NVARCHAR(200) NOT NULL,
        Theme NVARCHAR(100) NOT NULL,

        CONSTRAINT PK_Games
            PRIMARY KEY CLUSTERED (GameId),

        CONSTRAINT FK_Games_Providers
            FOREIGN KEY (ProviderId)
            REFERENCES dbo.Providers(ProviderId),

        CONSTRAINT UQ_Games_ProviderId_GameName
            UNIQUE (ProviderId, GameName)
    );
END;
GO

/*=========================================================
 4. CASINO WAGERS
=========================================================*/

IF OBJECT_ID('dbo.CasinoWagers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CasinoWagers
    (
        WagerId UNIQUEIDENTIFIER NOT NULL,
        TransactionId UNIQUEIDENTIFIER NOT NULL,
        BrandId UNIQUEIDENTIFIER NOT NULL,
        AccountId UNIQUEIDENTIFIER NOT NULL,

        ExternalReferenceId UNIQUEIDENTIFIER NOT NULL,
        TransactionTypeId UNIQUEIDENTIFIER NOT NULL,

        GameId INT NOT NULL,

        Amount DECIMAL(19,8) NOT NULL,

        CreatedDateTime DATETIMEOFFSET(7) NOT NULL,

        NumberOfBets INT NOT NULL,

        CountryCode CHAR(2) NOT NULL,

        Duration BIGINT NOT NULL,

        ReceivedAtUtc DATETIME2(7) NOT NULL
            CONSTRAINT DF_CasinoWagers_ReceivedAtUtc
            DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_CasinoWagers
            PRIMARY KEY CLUSTERED (WagerId),

        CONSTRAINT FK_CasinoWagers_Players
            FOREIGN KEY (AccountId)
            REFERENCES dbo.Players(AccountId),

        CONSTRAINT FK_CasinoWagers_Games
            FOREIGN KEY (GameId)
            REFERENCES dbo.Games(GameId),

        CONSTRAINT CK_CasinoWagers_Amount
            CHECK (Amount >= 0),

        CONSTRAINT CK_CasinoWagers_NumberOfBets
            CHECK (NumberOfBets > 0),

        CONSTRAINT CK_CasinoWagers_Duration
            CHECK (Duration >= 0)
    );
END;
GO

/*=========================================================
 5. INDEXES
=========================================================*/

/*
 Supports:
 GET api/player/{playerId}/casino

 Filter:
 AccountId

 Sort:
 CreatedDateTime DESC
*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_CasinoWagers_AccountId_CreatedDateTime'
      AND object_id = OBJECT_ID('dbo.CasinoWagers')
)
BEGIN
    CREATE NONCLUSTERED INDEX 
    IX_CasinoWagers_AccountId_CreatedDateTime
    ON dbo.CasinoWagers
    (
        AccountId,
        CreatedDateTime DESC,
        WagerId DESC
    )
    INCLUDE (GameId, Amount);
END;
GO

/*=========================================================
 6. INSERT CASINO WAGER
=========================================================*/

/*
 Responsibilities:

 - Insert or update player.
 - Insert provider if it does not exist.
 - Insert game if it does not exist.
 - Insert casino wager.
 - Prevent duplicate wagers.
 - Ensure operations execute in one transaction.

 WasInserted:
 1 = New wager inserted.
 0 = Wager already exists.
*/

CREATE OR ALTER PROCEDURE dbo.usp_CasinoWager_Insert

    @WagerId UNIQUEIDENTIFIER,
    @Theme NVARCHAR(100),
    @ProviderName NVARCHAR(200),
    @GameName NVARCHAR(200),

    @TransactionId UNIQUEIDENTIFIER,
    @BrandId UNIQUEIDENTIFIER,
    @AccountId UNIQUEIDENTIFIER,
    @Username NVARCHAR(100),

    @ExternalReferenceId UNIQUEIDENTIFIER,
    @TransactionTypeId UNIQUEIDENTIFIER,

    @Amount DECIMAL(19,8),
    @CreatedDateTime DATETIMEOFFSET(7),

    @NumberOfBets INT,
    @CountryCode CHAR(2),
    @Duration BIGINT

AS
BEGIN

    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /* Validate required fields. */

    IF @WagerId IS NULL
       OR @TransactionId IS NULL
       OR @BrandId IS NULL
       OR @AccountId IS NULL
       OR @ExternalReferenceId IS NULL
       OR @TransactionTypeId IS NULL
       OR @Theme IS NULL
       OR @ProviderName IS NULL
       OR @GameName IS NULL
       OR @Username IS NULL
       OR @CreatedDateTime IS NULL
       OR @CountryCode IS NULL
    BEGIN
        ;THROW 50001,
            'Required wager fields cannot be NULL.',
            1;
    END;

    IF @Amount < 0
       OR @NumberOfBets <= 0
       OR @Duration < 0
    BEGIN
        ;THROW 50002,
            'Wager amount, number of bets, or duration is invalid.',
            1;
    END;

    BEGIN TRY

        BEGIN TRANSACTION;

        /*
         Check whether wager already exists.

         UPDLOCK and HOLDLOCK help protect against
         concurrent duplicate deliveries.
        */

        IF EXISTS
        (
            SELECT 1
            FROM dbo.CasinoWagers WITH (UPDLOCK, HOLDLOCK)
            WHERE WagerId = @WagerId
        )
        BEGIN

            COMMIT TRANSACTION;

            SELECT CAST(0 AS BIT) AS WasInserted;

            RETURN;

        END;

        /* Insert or update player. */

        IF EXISTS
        (
            SELECT 1
            FROM dbo.Players WITH (UPDLOCK, HOLDLOCK)
            WHERE AccountId = @AccountId
        )
        BEGIN

            UPDATE dbo.Players
            SET Username = @Username
            WHERE AccountId = @AccountId
              AND Username <> @Username;

        END
        ELSE
        BEGIN

            INSERT INTO dbo.Players
            (
                AccountId,
                Username
            )
            VALUES
            (
                @AccountId,
                @Username
            );

        END;

        /* Find or create provider. */

        DECLARE @ProviderId INT;

        SELECT @ProviderId = ProviderId
        FROM dbo.Providers WITH (UPDLOCK, HOLDLOCK)
        WHERE ProviderName = @ProviderName;

        IF @ProviderId IS NULL
        BEGIN

            INSERT INTO dbo.Providers
            (
                ProviderName
            )
            VALUES
            (
                @ProviderName
            );

            SET @ProviderId = CONVERT(INT, SCOPE_IDENTITY());

        END;

        /* Find or create game. */

        DECLARE @GameId INT;

        SELECT @GameId = GameId
        FROM dbo.Games WITH (UPDLOCK, HOLDLOCK)
        WHERE ProviderId = @ProviderId
          AND GameName = @GameName;

        IF @GameId IS NULL
        BEGIN

            INSERT INTO dbo.Games
            (
                ProviderId,
                GameName,
                Theme
            )
            VALUES
            (
                @ProviderId,
                @GameName,
                @Theme
            );

            SET @GameId = CONVERT(INT, SCOPE_IDENTITY());

        END
        ELSE IF EXISTS
        (
            SELECT 1
            FROM dbo.Games
            WHERE GameId = @GameId
              AND Theme <> @Theme
        )
        BEGIN

            UPDATE dbo.Games
            SET Theme = @Theme
            WHERE GameId = @GameId;

        END;

        /* Insert wager. */

        INSERT INTO dbo.CasinoWagers
        (
            WagerId,
            TransactionId,
            BrandId,
            AccountId,
            ExternalReferenceId,
            TransactionTypeId,
            GameId,
            Amount,
            CreatedDateTime,
            NumberOfBets,
            CountryCode,
            Duration
        )
        VALUES
        (
            @WagerId,
            @TransactionId,
            @BrandId,
            @AccountId,
            @ExternalReferenceId,
            @TransactionTypeId,
            @GameId,
            @Amount,
            @CreatedDateTime,
            @NumberOfBets,
            @CountryCode,
            @Duration
        );

        COMMIT TRANSACTION;

        SELECT CAST(1 AS BIT) AS WasInserted;

    END TRY
    BEGIN CATCH

        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        THROW;

    END CATCH;

END;
GO

/*=========================================================
 7. GET PAGINATED PLAYER CASINO WAGERS
=========================================================*/

/*
 Returns two result sets.

 Result set 1:
 Wager records for requested page.

 Result set 2:
 Total number of wagers.

 Suitable for Dapper QueryMultipleAsync.
*/

CREATE OR ALTER PROCEDURE dbo.usp_PlayerCasinoWagers_GetPaged

    @AccountId UNIQUEIDENTIFIER,
    @Page INT = 1,
    @PageSize INT = 10

AS
BEGIN

    SET NOCOUNT ON;

    IF @AccountId IS NULL
        THROW 50010, 'AccountId is required.', 1;

    IF @Page < 1
        THROW 50011, 'Page must be greater than zero.', 1;

    IF @PageSize < 1 OR @PageSize > 500
        THROW 50012,
            'PageSize must be between 1 and 500.',
            1;

    DECLARE @Offset BIGINT =
        (CONVERT(BIGINT, @Page) - 1)
        * CONVERT(BIGINT, @PageSize);

    /* Result set 1: wager records. */

    SELECT

        w.WagerId,

        g.GameName AS Game,

        p.ProviderName AS Provider,

        w.Amount,

        w.CreatedDateTime AS CreatedDate

    FROM dbo.CasinoWagers AS w

    INNER JOIN dbo.Games AS g
        ON g.GameId = w.GameId

    INNER JOIN dbo.Providers AS p
        ON p.ProviderId = g.ProviderId

    WHERE w.AccountId = @AccountId

    ORDER BY
        w.CreatedDateTime DESC,
        w.WagerId DESC

    OFFSET @Offset ROWS

    FETCH NEXT @PageSize ROWS ONLY;

    /* Result set 2: total records. */

    SELECT COUNT_BIG(*) AS Total

    FROM dbo.CasinoWagers

    WHERE AccountId = @AccountId;

END;
GO

/*=========================================================
 8. GET TOP SPENDERS
=========================================================*/

/*
 Returns players ordered by total wager amount.

 Highest spending player appears first.
*/

CREATE OR ALTER PROCEDURE dbo.usp_PlayerTopSpenders_Get

    @Count INT = 10

AS
BEGIN

    SET NOCOUNT ON;

    IF @Count < 1 OR @Count > 1000
        THROW 50020,
            'Count must be between 1 and 1000.',
            1;

    SELECT TOP (@Count)

        p.AccountId,

        p.Username,

        SUM(w.Amount) AS TotalAmountSpend

    FROM dbo.Players AS p

    INNER JOIN dbo.CasinoWagers AS w
        ON w.AccountId = p.AccountId

    GROUP BY
        p.AccountId,
        p.Username

    ORDER BY
        SUM(w.Amount) DESC,
        p.AccountId ASC;

END;
GO

/*=========================================================
 9. VERIFY DATABASE OBJECTS
=========================================================*/

SELECT
    name AS TableName
FROM sys.tables
WHERE schema_id = SCHEMA_ID('dbo')
ORDER BY name;

SELECT
    name AS StoredProcedureName
FROM sys.procedures
WHERE schema_id = SCHEMA_ID('dbo')
ORDER BY name;

GO