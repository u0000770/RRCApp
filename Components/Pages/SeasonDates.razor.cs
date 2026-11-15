using RRCServices.Season;

namespace RRCApp.Components.Pages
{
    // =========================================================================
    // SeasonDates — admin season list (/admin/season-settings)
    // =========================================================================
    // SEASON WORK (batch 5): rewritten for ISeasonService / config.Season.
    // The old version edited the four dates of the JSON settings file through
    // ISeasonSettingsService, which no page uses any more and which this batch
    // deletes.
    //
    // Add: pre-fills the year after the newest season (1 Jan – 31 Oct), copying
    //      that season's no-target setting.
    // Edit: loads the season into the same form.
    // Save: ISeasonService.SaveAsync validates (SeasonRules.Validate): name
    //       required and unique, end on or after start, no overlap with another
    //       season. Problems are listed under the form.
    //
    // There is deliberately no Delete: a season with results would lose its
    // league and predictions. Correct its dates instead.
    // =========================================================================
    public partial class SeasonDates
    {
        private IReadOnlyList<SeasonInfo> _seasons = Array.Empty<SeasonInfo>();
        private SeasonInfo? _current;

        private SeasonEdit? _edit;                      // null = form hidden
        private IReadOnlyList<string> _errors = Array.Empty<string>();

        private bool _loading = true;
        private bool _saving;
        private string? _message;
        private string? _loadError;

        protected override async Task OnInitializedAsync()
        {
            await LoadAsync();
            _loading = false;
        }

        private async Task LoadAsync()
        {
            try
            {
                _seasons = await SeasonService.ListAsync();          // newest first
                _current = await SeasonService.GetCurrentAsync();
                _loadError = null;
            }
            catch (Exception ex)
            {
                _loadError = $"Could not load seasons: {ex.Message}";
            }
        }

        private void StartAdd()
        {
            var newest = _seasons.FirstOrDefault();
            var year = newest is null ? DateTime.Today.Year : newest.StartDate.Year + 1;

            _edit = new SeasonEdit
            {
                SeasonId = 0,
                Name = year.ToString(),
                StartDate = new DateOnly(year, 1, 1),
                EndDate = new DateOnly(year, 10, 31),
                AllowNoTarget = newest?.AllowNoTarget ?? true
            };

            _errors = Array.Empty<string>();
            _message = null;
        }

        private void StartEdit(SeasonInfo season)
        {
            _edit = SeasonEdit.From(season);
            _errors = Array.Empty<string>();
            _message = null;
        }

        private void CancelEdit()
        {
            _edit = null;
            _errors = Array.Empty<string>();
        }

        private async Task SaveAsync()
        {
            if (_edit is null) return;

            _saving = true;
            _errors = Array.Empty<string>();
            _message = null;

            try
            {
                var saved = await SeasonService.SaveAsync(_edit);
                _message = $"Season {saved.Name} saved: {saved.Window}.";
                _edit = null;
                await LoadAsync();
            }
            catch (SeasonValidationException ex)
            {
                _errors = ex.Errors;
            }
            catch (Exception ex)
            {
                _errors = new[] { $"Save failed: {ex.Message}" };
            }
            finally
            {
                _saving = false;
            }
        }
    }
}
