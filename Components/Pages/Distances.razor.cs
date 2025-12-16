using RRCServices;

namespace RRCApp.Components.Pages
{
    public partial class Distances
    {
        // Holds all rows for display.
        private List<DistanceDTO>? _items;

        // Model for the "Add new" form.
        private DistanceDTO _newModel = new();

        // Model for the currently edited row.
        private DistanceDTO _editModel = new();

        // Which row is being edited (by primary key).
        private int? _editingId;

        // Error message shown in the UI.
        private string? _error;

        protected override async Task OnInitializedAsync()
        {
            await ReloadAsync();
        }

        private async Task ReloadAsync()
        {
            _error = null;
            _items = await Service.GetAllAsync();
        }

        private async Task AddAsync()
        {
            _error = null;

            // Basic manual validation
            if (string.IsNullOrWhiteSpace(_newModel.Code))
            {
                _error = "Code is required.";
                return;
            }
            if (string.IsNullOrWhiteSpace(_newModel.Name))
            {
                _error = "Name is required.";
                return;
            }
            if (_newModel.Meters <= 0)
            {
                _error = "Meters must be greater than 0.";
                return;
            }

            await Service.AddAsync(_newModel);

            // Reset add form
            _newModel = new DistanceDTO();

            await ReloadAsync();
        }

        private void StartEdit(DistanceDTO d)
        {
            _error = null;

            // Toggle edit mode for this row
            _editingId = d.EFKey;

            // Copy values into edit model (do not edit list item directly)
            _editModel = new DistanceDTO
            {
                EFKey = d.EFKey,
                Code = d.Code,
                Name = d.Name,
                Meters = d.Meters
            };
        }

        private void CancelEdit()
        {
            _editingId = null;
            _editModel = new DistanceDTO();
        }

        private async Task SaveEditAsync()
        {
            _error = null;

            if (_editingId is null) return;

            if (string.IsNullOrWhiteSpace(_editModel.Code))
            {
                _error = "Code is required.";
                return;
            }
            if (string.IsNullOrWhiteSpace(_editModel.Name))
            {
                _error = "Name is required.";
                return;
            }
            if (_editModel.Meters <= 0)
            {
                _error = "Meters must be greater than 0.";
                return;
            }

            await Service.UpdateAsync(_editModel);

            CancelEdit();
            await ReloadAsync();
        }

        private async Task DeleteAsync(int id)
        {
            _error = null;

            await Service.DeleteAsync(id);

            if (_editingId == id)
                CancelEdit();

            await ReloadAsync();
        }
    }
}