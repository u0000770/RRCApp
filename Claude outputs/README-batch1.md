# Season work – Batch 1: foundation (no behaviour change)

This batch **adds** the season model, the service and tests. Nothing is wired into the apps yet, so every page behaves exactly as it does today. The old `ISeasonSettingsService` and JSON file are left in place and are removed in a later batch.

I couldn't compile this here (I have no .NET SDK), so the first build is yours. I checked every new type name against the solution: none clash.

## Files

| File in this batch | Goes to (`C:\Users\u0000770PC\source\repos\…`) | New / Replace |
|---|---|---|
| `RRCConfig/RRCConfig.csproj` | `RRCConfig\RRCConfig.csproj` | **New project** |
| `RRCConfig/Models/SeasonRecord.cs` | `RRCConfig\Models\SeasonRecord.cs` | New |
| `RRCConfig/Data/SeasonContext.cs` | `RRCConfig\Data\SeasonContext.cs` | New |
| `RRCConfig/Sql/001_create_config_season.sql` | `RRCConfig\Sql\001_create_config_season.sql` | New |
| `RRCServices/RRCServices.csproj` | `RRCServices\RRCServices.csproj` | **Replace** (adds 1 project reference) |
| `RRCServices/Clock/IClock.cs` | `RRCServices\Clock\IClock.cs` | **Replace** (adds `Today`; UK time) |
| `RRCServices/Season/SeasonWindow.cs` | `RRCServices\Season\SeasonWindow.cs` | New |
| `RRCServices/Season/SeasonInfo.cs` | `RRCServices\Season\SeasonInfo.cs` | New |
| `RRCServices/Season/SeasonRules.cs` | `RRCServices\Season\SeasonRules.cs` | New |
| `RRCServices/Season/ISeasonService.cs` | `RRCServices\Season\ISeasonService.cs` | New |
| `RRCServices/Season/SeasonService.cs` | `RRCServices\Season\SeasonService.cs` | New |
| `RRCServiceTest/Seasons/SeasonWindowTests.cs` | `RRCServiceTest\Seasons\SeasonWindowTests.cs` | New |
| `RRCServiceTest/Seasons/SeasonRulesTests.cs` | `RRCServiceTest\Seasons\SeasonRulesTests.cs` | New |

## Steps

1. **Database.** Open `001_create_config_season.sql` in the Azure portal Query editor (or SSMS) against the RRCAzure database and run it. The final `SELECT` should show three rows:

   | Name | StartDate | EndDate | AllowNoTarget |
   |---|---|---|---|
   | 2025 | 2025-01-01 | 2025-10-31 | 0 |
   | 2026 | 2026-01-01 | 2026-10-31 | 1 |
   | 2027 | 2027-01-01 | 2027-10-31 | 1 |

   If it fails with a permission error on `CREATE SCHEMA`, run it signed in as the server admin. The apps' login only needs read and write access to the table; `db_datareader`/`db_datawriter` cover every schema.

2. **New project.** Create the folder `repos\RRCConfig`, copy in the 4 RRCConfig files, then in VS: right-click the solution → **Add → Existing Project…** → `RRCConfig.csproj`.

3. **RRCServices.** Replace `RRCServices.csproj` and `Clock\IClock.cs`, and copy the 5 new files into `Season\`. SDK-style projects pick up new files on their own.

4. **Tests.** Copy the 2 test files into a new `RRCServiceTest\Seasons\` folder.

5. **Build and test.** Rebuild the solution, then run all tests in Test Explorer. You should see:
   - The 2 new test classes, 42 cases, all passing.
   - All existing tests unchanged.

## Why `IClock.cs` changes without breaking anything

- `Today` is a *default interface member*, so `FixedClock` and the three host copies of `SystemClock` (RRCApp, PublicApp, rrcapi) compile unchanged.
- The UK-time change only affects `RRCServices.Clock.SystemClock`. No host uses it yet because each host's own copy shadows it, so runtime behaviour doesn't change. A later batch deletes the host copies.

## What's next (Batch 2, for you to review first)

- Register `SeasonContext` and `ISeasonService` in RRCApp, PublicApp and rrcapi.
- **Predictions** (`IRacePredictionService`, RunnerCalculator, PublicApp MyDetails): use the effective season of the race date. This stops December 2025 being included, and the special November race is predicted from the 2026 races.
- **League pages and services**: default to the current season, and only count races inside it. The special race is never scored.
- Replace `Year == 2026` with `season.AllowNoTarget`.
