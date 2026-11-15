# Season work – Batch 4: seasons endpoint in rrcapi; MobApp stops hard-coding 2025-12-01

rrcapi gets two read-only season endpoints. MobApp's My Details page now asks the API for the season of the **selected race**, with the same rule as the web apps: the containing season, or between seasons (the special November race) the season that has just ended.

All 4 files are **replacements**, with changes marked `SEASON WORK (batch 4)`. Line endings and BOMs match your originals. `batch4-changes.diff` shows every changed line.

## Files

| File (`C:\Users\u0000770PC\source\repos\…`) | What changes |
|---|---|
| `rrcapi\Program.cs` | Your Batch 2+3 version plus `GET /api/v1/seasons` and `GET /api/v1/seasons/effective?date=yyyy-MM-dd`, and a `SeasonDto` record |
| `rrc.contracts\DTOs.cs` | + `SeasonDto` (seasonId, name, startDate, endDate, allowNoTarget) |
| `MobApp\ApiClient\RaceApi.cs` | + `GetEffectiveSeasonAsync(date)`. Returns `null` when the API answers 404 (no season covers the date). |
| `MobApp\Pages\MyDetails.razor` | `_seasonStart` is no longer `new(2025, 12, 1)`. The new `ApplySeasonForSelectedRaceAsync()` fetches the season for the selected race (cached per date) and sets `_seasonStart` and `_hasSeasonRaces`. `OnSelectedRaceChanged` is now `async Task`. |

## Testing locally

MobApp's API address is hard-coded to the **Azure** rrcapi in `MobApp\Program.cs`. Azure won't have `/seasons` until you deploy, so MobApp run as-is will report "no season has been set up". To test locally:

1. **Temporarily** change the `BaseAddress` in `MobApp\Program.cs` to `https://localhost:7001` (your local rrcapi). rrcapi's CORS already allows MobApp's `https://localhost:7142`.
2. Solution → **Configure Startup Projects → Multiple startup projects** → set rrcapi and MobApp to Start, using the `https` profile for both.
3. Check rrcapi in Swagger:
   - `GET /api/v1/seasons` should return 2027, 2026 and 2025.
   - `GET /api/v1/seasons/effective?date=2026-11-20` should return **2026** (the special November race).
   - `GET /api/v1/seasons/effective?date=2027-01-01` should return **2027**.
   - `GET /api/v1/seasons/effective?date=2024-06-01` should return **404**.
4. In MobApp, open My Details for a runner:
   - With the November race selected, "this season" should mean from 01 Jan 2026.
   - With a 2027 race selected (if one is in the next 3 months), you should see "please run at least one race this season (from 01 Jan 2027)".
5. **Put the `BaseAddress` back to the Azure address** before publishing MobApp.

## Known gap (not part of this batch)

MobApp's `Calculate()` never actually makes a prediction. After its checks, it stops without calling the prediction formula, so the mobile My Details page never shows a predicted time. That was already the case before this batch.

## Deploying to Azure (later)

1. Make sure the `config.Season` script has been run on Azure. This was already needed for Batch 2+3.
2. Publish **rrcapi** before **MobApp**, so the new endpoints exist when the new MobApp calls them.
