# Season Dates – Analysis & Recommendations (v2)

_v2: 15 Nov 2026, updated with the agreed decisions · Scope: RRCApp.sln (all 10 projects)_

## 0. Agreed decisions

| # | Decision |
|---|---|
| D1 | A season runs **01 Jan → 31 Oct inclusive**. **01 Nov → 31 Dec is between seasons.** Seasons are named by year ("2026"). |
| D2 | There is one **special race in November**. It needs predicted times but is **not part of the season**, so it is never scored in the league. |
| D3 | Predictions **reset on 01 Jan**. A race is predicted from the races in its own season. In the Nov–Dec gap (the special race), it uses the season that has just ended. |
| D4 | The **no-target rule** (a race with no target time still scores a flat 6 points) **continues in 2027**. It becomes a per-season setting (`AllowNoTarget`): 2025 off, 2026 on, 2027 on. |
| D5 | The **selection window** is unused. It is **dropped**. Dropdowns keep their own rolling ranges. |
| D6 | Seasons are stored in a **new `config.Season` table** in the existing RRCAzure database, accessed through a separate small `SeasonContext` (project `RRCConfig`). The **legacy `RRCContext` model and its migrations are not touched.** |
| D7 | Admins set season start and end dates, and the change must reach **every app, current and future**. That's why it lives in the shared database rather than in per-app files. |

SQLite was considered and rejected. RRCApp, PublicApp and rrcapi are three separate App Services, each with its own disk, so a SQLite file has the same problem as today's JSON file: an admin save only reaches one app.

## 1. Existing mechanism (as found)

- **Design:** the admin page `/admin/season-settings` → `SeasonSettingsService` → `JsonSeasonSettingsStore` → `App_Data/seasonSettings.json`, with defaults in `appsettings.json`. It holds four dates: season start and end, and selection start and end.
- **Reality:**
  - Only **RRCApp** has the file and the defaults.
  - **PublicApp** registers the service but has neither, so `GetAsync()` returns **0001-01-01** for every date. This is why the service calls were commented out and replaced with literals.
  - **rrcapi** and **MobApp** have no season source at all.
  - The file is per-server, so an admin save could never reach the other apps anyway.
- **Three sources disagree:**
  - The JSON file: 2025-01-01 → 2026-12-31.
  - `appsettings.json`: 2025-12-01 → 2026-11-30, with a selection window that falls outside the season.
  - The hard-coded literals: a range of other values.
- **The season was moved by a month.** Most of the hard-coded values assume **1 Dec → 30 Nov**, which contradicts D1.

### Hard-coded call sites

| # | File : line | Current | Fix (per decisions) |
|---|---|---|---|
| 1 | `RRCServices/Calculator/IRacePredictionService.cs:261,264` | 2025-11-30 → 2026-11-30 | Effective season of the **race event date** (D3) |
| 2 | `RRCApp/Components/Pages/RunnerCalculator.razor.cs:465` | start 2025-12-01 | Effective season (D3) |
| 3 | `PublicApp/Components/Pages/MyDetails.razor.cs:652` | start 2025-12-01 | Current season (D3) |
| 4 | `MobApp/Pages/MyDetails.razor:200` | start 2025-12-01 (compiled into client) | Read from new API endpoint |
| 5 | `PublicApp/Components/Pages/League.razor.cs:10-11` | 2026-01-01 → 2026-11-30 | Default to current season |
| 6 | `PublicApp/Components/Pages/League.razor.cs:45-46` | Reset → **2025** | Reset → current season |
| 7 | `RRCApp/Components/Pages/LeagueSummary.razor:158-159` | 1 Jan → **31 Dec** (would score a December race) | Default to current season |
| 8 | `RRCServices/League/LeagueTableService.cs:203` | `Year == 2026` | `season.AllowNoTarget` (D4) |
| 9 | `RRCServices/League/LeagueTableService.cs:492` | `Year == 2026` | `season.AllowNoTarget` (D4) |
| 10 | `RRCServices/League/LeagueSummaryService.cs:117` | `Year == 2026` | `season.AllowNoTarget` (D4) |

### Other defects

1. **Races on the last day can be dropped.** The end date is passed as midnight and queried with `<=`, so a race on the last day with a time attached isn't counted. Fixed by `SeasonWindow.ToExclusive` with `<`.
2. **Publishing overwrites admin edits.** The Web SDK publishes `App_Data/seasonSettings.json`, so every deploy replaces what the admin saved with your local copy. Goes away with the database table.
3. **The cache never refreshes.** Saved dates only reach the app after a restart. The new service doesn't cache.
4. **There are five `SystemClock` copies**, and `DateTime.Today` is used directly in many places.
   - Azure runs in UTC, so "today" is wrong between 00:00 and 01:00 during BST.
   - Fixed by `IClock.Today` in UK time, then removing the copies.

## 2. Target design (built in Batch 1)

```
RRCConfig (new)                          RRCServices
  Models/SeasonRecord  ── config.Season    Season/SeasonWindow   inclusive range, FromInclusive / ToExclusive
  Data/SeasonContext   (no migrations)     Season/SeasonInfo     read model (+ SeasonEdit, SeasonValidationException)
  Sql/001_create_config_season.sql         Season/SeasonRules    pure rules: FindContaining / FindEffective / Validate
                                           Season/ISeasonService ListAsync, GetByIdAsync, GetContainingAsync,
                                                                 GetEffectiveAsync, GetCurrentAsync, SaveAsync
                                           Season/SeasonService  EF over SeasonContext, no cache
                                           Clock/IClock          + Today (UK time)
```

Which method to use where:

| Need | Call |
|---|---|
| League tables and scoring | `GetContainingAsync(date)`, or the season the user picks |
| Predictions for a race | `GetEffectiveAsync(raceDate)` |
| Default season for a page | `GetCurrentAsync()` (in Nov–Dec this returns the season just ended) |
| Admin page | `ListAsync()`, `SaveAsync(edit)` (validates no overlaps and unique names) |

## 3. Plan

| Batch | Content | Behaviour change |
|---|---|---|
| **1** ✅ issued | `RRCConfig` project + SQL script + season service + `IClock.Today` + tests | None |
| 2 | Register in RRCApp, PublicApp and rrcapi. Fix call sites #1–#3 (predictions) and #5–#7 (league defaults). | **Yes.** Predictions exclude December 2025; the special race is predicted from 2026; the league stops at 31 Oct. |
| 3 | Fix #8–#10: `AllowNoTarget` replaces `Year == 2026`. Merge the duplicated league query. | 2027 keeps the rule |
| 4 | rrcapi `GET /api/v1/seasons/current` + `rrc.contracts` `SeasonDto`. MobApp (#4) reads it. | Mobile matches the web apps |
| 5 | Admin page becomes a season list editor. Remove the selection window, `ISeasonSettingsService`, both JSON stores, the `App_Data` file, the appsettings sections and the host `SystemClock` copies. | Admin edits reach every app |

**Timing:**
- If the special race's **predictions haven't been run yet**, Batch 2 should go in first. Today's code includes December 2025 races (D3).
- Batch 3 must be in **before the first 2027 league race**.
