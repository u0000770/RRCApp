using RRCServices.Runner;

namespace RRCApp.Components.Pages
{
    public partial class Runners
    {
        private List<RunnerListItemDto>? _runners;

        private string? _search;
        private bool _includeInactive;

        private string _sortColumn = "Name";
        private bool _sortAscending = true;

        protected override async Task OnInitializedAsync()
        {
            await ReloadAsync();
        }

        private async Task ReloadAsync()
        {
            _runners = (await RunnerService.GetRunnersAsync(
                _search,
                includeInactive: _includeInactive))
                .ToList();

            ApplySort();
        }

        private void SortBy(string column)
        {
            if (_sortColumn == column)
                _sortAscending = !_sortAscending;
            else
            {
                _sortColumn = column;
                _sortAscending = true;
            }

            ApplySort();
        }

        private void ApplySort()
        {
            if (_runners is null) return;

            _runners = (_sortColumn, _sortAscending) switch
            {
                ("Name", true) => _runners.OrderBy(r => r.Name).ToList(),
                ("Name", false) => _runners.OrderByDescending(r => r.Name).ToList(),

                ("Active", true) => _runners.OrderBy(r => r.Active).ThenBy(r => r.Name).ToList(),
                ("Active", false) => _runners.OrderByDescending(r => r.Active).ThenBy(r => r.Name).ToList(),

                _ => _runners
            };
        }

        private string SortIndicator(string column)
        {
            if (_sortColumn != column) return "";
            return _sortAscending ? "▲" : "▼";
        }
    }
}