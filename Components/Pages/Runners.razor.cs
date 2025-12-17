using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RRCServices.Runner;

namespace RRCApp.Components.Pages
{
    public partial class Runners
    {
        private string? SearchText;
        private bool IncludeInactive;

        private bool IsBusy;
        private string? Error;

        private List<RunnerListItemDto> Items = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            Error = null;
            IsBusy = true;

            try
            {
                var results = await RunnerService.GetRunnersAsync(SearchText, IncludeInactive);
                Items = results.ToList();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ClearAsync()
        {
            SearchText = null;
            IncludeInactive = false;
            await LoadAsync();
        }

        private void GoDetails(int runnerId)
            => Nav.NavigateTo($"/runners/{runnerId}");

        private void GoCreate()
            => Nav.NavigateTo("/runners/create");

        private async Task HandleSearchKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await LoadAsync();
            }
        }

        private async Task ToggleActiveAsync(RunnerListItemDto r, ChangeEventArgs e)
        {
            Error = null;

            var newValue = e.Value is bool b
                ? b
                : bool.TryParse(e.Value?.ToString(), out var parsed) && parsed;

            IsBusy = true;
            try
            {
                // optimistic UI update
                r.Active = newValue;

                var ok = await RunnerService.SetRunnerActiveAsync(r.Id, newValue);
                if (!ok)
                {
                    Error = "Failed to update runner status.";
                }

                // optional: if not including inactive, remove from list immediately
                if (!IncludeInactive && newValue == false)
                {
                    Items.Remove(r);
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                // reload to restore correct state if update failed
                await LoadAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static string RowClass(RunnerListItemDto r)
            => r.Active == true ? "" : "table-secondary";
    }
}