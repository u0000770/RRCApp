/* =============================================================================
   001_create_config_season.sql
   -----------------------------------------------------------------------------
   Creates the 'config' schema and the config.Season table in the RRCAzure
   database, then seeds seasons 2025, 2026 and 2027.

   * Touches NOTHING in the existing (dbo) legacy tables.
   * Safe to re-run: every step checks whether it has already been done.
   * Contains no GO separators, so it runs in the Azure portal Query editor,
     SSMS, Azure Data Studio, or VS SQL Server Object Explorer.

   Season rules (agreed 15 Nov 2026):
     - A season runs 01 Jan .. 31 Oct inclusive.
     - 01 Nov .. 31 Dec is between seasons (the special November race is here).
     - AllowNoTarget: the "no target time -> flat 6 points" rule.
         2025 = off (old behaviour), 2026 = on, 2027 = on.
   ============================================================================= */

-- 1. Schema ------------------------------------------------------------------
IF SCHEMA_ID(N'config') IS NULL
    EXEC (N'CREATE SCHEMA config AUTHORIZATION dbo;');

-- 2. Table (dynamic SQL so the batch compiles before the schema exists) ------
IF OBJECT_ID(N'config.Season', N'U') IS NULL
    EXEC (N'
CREATE TABLE config.Season
(
    SeasonId      int            IDENTITY(1,1) NOT NULL
                  CONSTRAINT PK_config_Season PRIMARY KEY,
    Name          nvarchar(20)   NOT NULL,
    StartDate     date           NOT NULL,
    EndDate       date           NOT NULL,
    AllowNoTarget bit            NOT NULL
                  CONSTRAINT DF_config_Season_AllowNoTarget DEFAULT (1),
    CreatedUtc    datetime2(0)   NOT NULL
                  CONSTRAINT DF_config_Season_CreatedUtc DEFAULT (sysutcdatetime()),
    UpdatedUtc    datetime2(0)   NOT NULL
                  CONSTRAINT DF_config_Season_UpdatedUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT UQ_config_Season_Name  UNIQUE (Name),
    CONSTRAINT CK_config_Season_Dates CHECK  (StartDate <= EndDate)
);');

-- 3. Seed (only inserts seasons that are not already there) -----------------
IF NOT EXISTS (SELECT 1 FROM config.Season WHERE Name = N'2025')
    INSERT config.Season (Name, StartDate, EndDate, AllowNoTarget)
    VALUES (N'2025', '2025-01-01', '2025-10-31', 0);

IF NOT EXISTS (SELECT 1 FROM config.Season WHERE Name = N'2026')
    INSERT config.Season (Name, StartDate, EndDate, AllowNoTarget)
    VALUES (N'2026', '2026-01-01', '2026-10-31', 1);

IF NOT EXISTS (SELECT 1 FROM config.Season WHERE Name = N'2027')
    INSERT config.Season (Name, StartDate, EndDate, AllowNoTarget)
    VALUES (N'2027', '2027-01-01', '2027-10-31', 1);

-- 4. Check -------------------------------------------------------------------
SELECT SeasonId, Name, StartDate, EndDate, AllowNoTarget, CreatedUtc, UpdatedUtc
FROM   config.Season
ORDER  BY StartDate;
