# Season work – Batch 5 (final): admin season editor, and removal of the old code

1. The admin page `/admin/season-settings` becomes a **season list**. You can add a season (for example 2028) or edit one, with no SQL. Saving rejects overlapping seasons and duplicate names.
2. The old JSON season settings code is **removed**, along with the duplicate `SystemClock` classes. All three server apps now use the single UK-time clock in RRCServices.

Do the three parts in order (A → B → C), then rebuild. Part B removes code that the old versions of the Part A files still use, so doing B first won't compile.

## A. Replace (7 files)

| File (`C:\Users\u0000770PC\source\repos\…`) | What changes |
|---|---|
| `RRCApp\Components\Pages\SeasonDates.razor` | Rewritten: season table, Add/Edit form, error list. Interactive Server. |
| `RRCApp\Components\Pages\SeasonDates.razor.cs` | Rewritten for `ISeasonService` |
| `RRCApp\Components\_Imports.razor` | `@using Season` → `@using RRCServices.Season`. The old line only resolved to the `RRCApp\Season` folder that Part B deletes. |
| `RRCApp\Program.cs` | Old season settings registrations removed (`Configure<SeasonSettings>` ×2, JSON store, `ISeasonSettingsService`); `using RRCApp.Season;` removed |
| `PublicApp\Program.cs` | Old JSON store and `ISeasonSettingsService` registrations removed |
| `rrcapi\Program.cs` | Its own `SystemClock` class at the bottom removed |
| `RRCServices\Clock\IClock.cs` | Comment updated only. This `SystemClock` is now the one every app uses. |

## B. Delete (9 files, then 2 empty folders)

In Visual Studio Solution Explorer, right-click → Delete.

| Delete | Why it's safe |
|---|---|
| `RRCServices\Season\ISeasonSettingsService.cs` | Replaced by `ISeasonService` |
| `RRCServices\Season\ISeasonSettingsStore.cs` | Only used by the JSON stores |
| `RRCServices\Season\SeasonSettingsService.cs` | Also contains the old `SeasonSettings` class. Nothing uses either after Part A. |
| `RRCApp\Season\JsonSeasonSettingsStore.cs` | Old JSON store. Then delete the empty `RRCApp\Season` folder. |
| `RRCApp\App_Data\seasonSettings.json` | The old settings file. Then delete the empty `RRCApp\App_Data` folder. |
| `PublicApp\Class.cs` | PublicApp's copy of the JSON store, never used |
| `RRCApp\Class.cs` | Contains only RRCApp's copy of `SystemClock` |
| `PublicApp\clock.cs` | Contains only PublicApp's copy of `SystemClock` |
| `rrcapi\Class.cs` | rrcapi's unused copy of `SystemClock` |

**Keep** `RRCServices\Season\` itself. It still holds `ISeasonService`, `SeasonService`, `SeasonRules`, `SeasonWindow` and `SeasonInfo`.

## C. Edit by hand (2 files)

I'm not sending these two files, because they contain your database password.

In **`RRCApp\appsettings.json`** and **`PublicApp\appsettings.json`**, delete the whole `"SeasonSettings"` block (the block itself, plus the comma after it):

```json
  "SeasonSettings": {
    "SeasonStartDate": "2025-12-01",
    "SeasonEndDate": "2026-11-30",
    "SelectionStartDate": "2025-03-01",
    "SelectionEndDate": "2025-09-30"
  },
```

Optionally, in `RRCApp\Components\Layout\NavMenu.razor`, rename the link text "Season Date Settings" to "Seasons". The address `/admin/season-settings` stays the same.

## Check

1. **Rebuild Solution**: no errors. If one mentions `SeasonSettings`, `JsonSeasonSettingsStore` or `SystemClock`, a Part A file is still the old version.
2. Run RRCApp and open **Season Date Settings**. You should see:
   - 2027, 2026 and 2025 listed, with 2026 marked **current** (we're between seasons, so it's the one that has just ended).
   - **Add season** pre-fills 2028 (01 Jan 2028 – 31 Oct 2028, no-target rule on). Save, and it appears in the list.
   - Edit 2028 so its start date is 31 Oct 2027: Save is refused with an "overlap" message. Cancel.
   - Add another season called "2027": refused with "already exists".
   - (Remove the 2028 test row in SQL if you don't want it yet: `DELETE FROM config.Season WHERE Name = N'2028';`)
3. PublicApp's League and My Details pages and rrcapi's Swagger still behave as after Batches 2–4.

## Deploying to Azure

Nothing new is needed on the database side. On Azure, the old `App_Data\seasonSettings.json` may stay in RRCApp's site folder after publishing, because publishing doesn't delete files by default. Nothing reads it any more, so it's harmless. You can delete it in Kudu, or tick "Remove additional files at destination" in the publish settings.
