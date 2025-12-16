using RRCServices;

namespace RRCApp.Components.Pages
{
    public partial class Event
    {
        private List<EventDTO>? _items;

        // Drop-down data
        private List<DisciplineDTO> _disciplines = new();
        private List<DistanceDTO> _distances = new();

        // Add + edit models
        private EventDTO _newModel = new();
        private EventDTO _editModel = new();

        // Because InputCheckbox binds to bool, keep a backing bool for nullable Active
        private bool _newActive = true;
        private bool _editActive = true;

        private int? _editingId;
        private string? _error;


        private string _sortColumn = "Title";
        private bool _sortAscending = true;

        private void SortBy(string column)
        {
            // If user clicks same column, toggle direction; otherwise set new column ascending.
            if (_sortColumn == column)
                _sortAscending = !_sortAscending;
            else
            {
                _sortColumn = column;
                _sortAscending = true;
            }

            // Apply sort to the already-loaded list (_items)
            if (_items is null) return;

            _items = (_sortColumn, _sortAscending) switch
            {
                ("Title", true) => _items.OrderBy(e => e.Title).ToList(),
                ("Title", false) => _items.OrderByDescending(e => e.Title).ToList(),

                ("Venue", true) => _items.OrderBy(e => e.Venue).ToList(),
                ("Venue", false) => _items.OrderByDescending(e => e.Venue).ToList(),

                ("Discipline", true) => _items.OrderBy(e => e.Discipline).ToList(),
                ("Discipline", false) => _items.OrderByDescending(e => e.Discipline).ToList(),

                ("Distance", true) => _items.OrderBy(e => e.DistanceCode).ToList(),
                ("Distance", false) => _items.OrderByDescending(e => e.DistanceCode).ToList(),

                ("Active", true) => _items.OrderBy(e => e.Active == true).ToList(),
                ("Active", false) => _items.OrderByDescending(e => e.Active == true).ToList(),

                _ => _items
            };
        }


        protected override async Task OnInitializedAsync()
        {
            await LoadLookupsAsync();
            await ReloadAsync();
        }

        private async Task LoadLookupsAsync()
        {
            // Distances are not active-filtered in your DistanceService, so we just load all.
            _distances = await DistanceService.GetAllAsync();

            // Disciplines only active
            _disciplines = await DisciplineService.GetAllActiveAsync();
        }

        private async Task ReloadAsync()
        {
            _error = null;
            _items = await EventsService.GetAllAsync();
        }

        private async Task AddAsync()
        {
            _error = null;

            // Simple validation
            if (string.IsNullOrWhiteSpace(_newModel.Title))
            {
                _error = "Title is required.";
                return;
            }

            // Set nullable Active based on checkbox
            _newModel.Active = _newActive;

            await EventsService.AddAsync(_newModel);

            _newModel = new EventDTO();
            _newActive = true;

            await ReloadAsync();
        }

        private void StartEdit(EventDTO e)
        {
            _error = null;
            _editingId = e.Id;

            _editModel = new EventDTO
            {
                Id = e.Id,
                Title = e.Title,
                Venue = e.Venue,
                Discipline = e.Discipline,
                DistanceCode = e.DistanceCode,
                Active = e.Active
            };

            _editActive = (e.Active == true);
        }

        private void CancelEdit()
        {
            _editingId = null;
            _editModel = new EventDTO();
            _editActive = true;
        }

        private async Task SaveEditAsync()
        {
            _error = null;

            if (_editingId is null) return;

            if (string.IsNullOrWhiteSpace(_editModel.Title))
            {
                _error = "Title is required.";
                return;
            }

            _editModel.Active = _editActive;

            await EventsService.UpdateAsync(_editModel);

            CancelEdit();
            await ReloadAsync();
        }

        private async Task HardDeleteAsync(int id)
        {
            _error = null;

            var deleted = await EventsService.HardDeleteAsync(id);
            if (!deleted)
            {
                _error = $"Event {id} not found.";
                return;
            }

            if (_editingId == id)
                CancelEdit();

            await ReloadAsync();
        }
    }
}