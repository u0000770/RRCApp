# Season work – Batch 2+3 (combined): connect the apps, fix predictions, league defaults and the no-target rule

This one package does three things:
- **(2)** connects RRCApp, PublicApp and rrcapi to the season service from Batch 1 and removes every hard-coded season date from predictions and the league pages;
- **(3)** replaces the hard-coded `Year == 2026` no-target rule with each season's `AllowNoTarget` setting;
- fixes rrcapi's league endpoint, which was missing a service registration.

**Behaviour changes in this batch.** Test it against your local database first.

Every changed line is marked `SEASON WORK (batch 2…)` or `FIX (batch 2+3)`. Your `#region old` blocks are untouched. Line endings and BOMs match your originals. `batch2-changes.diff` shows every changed line.

## Files

| File (`C:\Users\u0000770PC\source\repos\…`) | New / Replace | What changes |
|---|---|---|
| `RRCServices\Season\SeasonRules.cs` | Replace (Batch 1 file) | + `AllowsNoTarget(seasons, date)`, rule 6 |
| `RRCServices\Calculator\IRacePredictionService.cs` | Replace | Season comes from the **race date** (effective season). The hard-coded 2025-11-30 → 2026-11-30 is gone. The season's last day is now counted in full. |
| `RRCServices\League\LeagueTableService.cs` | Replace | `Year == 2026` → new `ApplyNoTargetRule` using the season flags. `ScoreRunner` gives 6 to any no-target race it receives. The constructor takes `ISeasonService`. |
| `RRCServices\League\LeagueSummaryService.cs` | Replace | Same `ApplyNoTargetRule` call, so admin and public scores can't differ. The constructor takes `ISeasonService`. |
| `RRCApp\Program.cs` | Replace | Registers `SeasonContext` and `ISeasonService` |
| `RRCApp\Components\Pages\RunnerCalculator.razor` | Replace | `@inject ISeasonService SeasonService` |
| `RRCApp\Components\Pages\RunnerCalculator.razor.cs` | Replace | Season comes from the **selected race**; messages name the season |
| `RRCApp\Components\Pages\LeagueSummary.razor` | Replace | Defaults to the current season, not the calendar year; includes the full last day |
| `PublicApp\Program.cs` | Replace | Registers `SeasonContext` and `ISeasonService` |
| `PublicApp\Components\Pages\MyDetails.razor` | Replace | `@inject ISeasonService SeasonService` |
| `PublicApp\Components\Pages\MyDetails.razor.cs` | Replace | Season comes from the **selected race** (`ApplySeasonForSelectedRace`) |
| `PublicApp\Components\Pages\League.razor` | Replace | Injects `ISeasonService` and shows "Season 2026: 01 Jan 2026 – 31 Oct 2026" |
| `PublicApp\Components\Pages\League.razor.cs` | Replace | Uses the current season; Reset goes to the current season (was 2025); includes the full last day |
| `rrcapi\Program.cs` | Replace | Registers `ISeasonService` and `SeasonContext`. **FIX:** registers `ITrophyCalculator`, which was missing, so `/api/v1/league/trophy` returned an error. **FIX:** the end date now stops at 23:59:59. |
| `RRCServiceTest\Seasons\NoTargetRuleTests.cs` | **New** | 9 tests for the season-driven no-target rule, including the 1 Jan 2027 race |

## Expected results (testing locally, today = 15 Nov 2026)

| Where | Expected |
|---|---|
| **Test Explorer** | `NoTargetRuleTests` passes, and the Batch 1 season tests still pass. Your existing `ScoreRunnerTests` give the same results as before this batch. |
| Public **League** | Shows "Season 2026: 01 Jan 2026 – 31 Oct 2026". Click Load: nothing after 31 Oct is scored. No-target 2026 races still score 6, as before. |
| Admin **League Summary** | 01/01/2026 – 31/10/2026, with the same points as the public League page. Set the dates to cover 2025: no-target 2025 races are still excluded, as before. |
| Admin **Race Calculator** | For the November special race, the message ends "…from season 2026". |
| Admin predicted times for a race event | For the November race, only 2026 results are used. December 2025 is no longer used. |
| Public **My Details** | For the November race, it predicts from 2026 races. For a January 2027 race, it says "please run at least one race this season (from 01 Jan 2027)". |
| rrcapi Swagger: `GET /api/v1/league/trophy?start=2026-01-01&end=2026-10-31` | Returns the tables instead of an error. |

**On 1 Jan 2027:** there are no targets, and the 2027 season has `AllowNoTarget = 1`, so everyone with a recorded time scores 6 points. `NoTargetRuleTests` covers exactly this case.

## Not in this package

- MobApp still hard-codes 2025-12-01, and rrcapi has no seasons endpoint yet. This is Batch 4.
- The admin Season Settings page and the JSON file are still there but no longer used. They're removed in Batch 5.

## Deploying to Azure (later)

1. Run `RRCConfig\Sql\001_create_config_season.sql` against the **Azure** database **first**. All three apps now read `config.Season`.
2. Then publish RRCApp, PublicApp and rrcapi.
