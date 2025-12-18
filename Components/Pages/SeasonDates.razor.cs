using RRCServices.Season;

namespace RRCApp.Components.Pages
{
    public partial class SeasonDates
    {
        private SeasonSettings _model = new();
        private bool _loading = true;
        private string? _message;
        private string? _error;

        protected override async Task OnInitializedAsync()
        {
            _model = await SeasonSettingsService.GetAsync();
            _loading = false;
        }

        private async Task SaveAsync()
        {
            _message = null;
            _error = null;

            try
            {
                await SeasonSettingsService.UpdateAsync(_model);
                _message = "Saved successfully.";
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
        }
    }
}