using RRCServices;

namespace RRCApp.Components.Pages
{
    public partial class Competition
    {
        // ----------------------------
        // STATE (what drives the UI)
        // ----------------------------

        // The list shown in the table. Null means "still loading".
        private List<Competitions>? _items;

        // DTO used for the "Add new" form.
        private Competitions _newModel = new();

        // DTO used for the "Edit row" inputs.
        // We copy row values into this so editing doesn't change the list item until Save.
        private Competitions _editModel = new();

        // Which row is currently being edited (null means "no row is in edit mode")
        private int? _editingId;

        // A simple error message for the page
        private string? _error;

        // ----------------------------
        // LIFECYCLE
        // ----------------------------

        // Runs once when the component first loads.
        // We load the table data here.
        protected override async Task OnInitializedAsync()
        {
            await ReloadAsync();
        }

        // Reloads the list from the DB via the service.
        // Because the service returns only Active records, soft-deleted items disappear after this.
        private async Task ReloadAsync()
        {
            _error = null;
            _items = await Service.GetAllActiveAsync();
        }

        // ----------------------------
        // CREATE
        // ----------------------------

        private async Task AddAsync()
        {
            _error = null;

            // Manual validation (works even if you haven't added [Required] attributes)
            if (string.IsNullOrWhiteSpace(_newModel.Title))
            {
                _error = "Title is required.";
                return;
            }

            // Code is optional in your DTO (string?), but you can enforce if you want:
            // if (string.IsNullOrWhiteSpace(_newModel.Code)) { _error = "Code is required."; return; }

            await Service.AddAsync(_newModel);

            // Reset the add form so inputs clear
            _newModel = new Competitions();

            await ReloadAsync();
        }

        // ----------------------------
        // EDIT MODE (client-side only)
        // ----------------------------

        private void StartEdit(Competitions c)
        {
            _error = null;

            // This is the "switch" that makes the row render inputs instead of text
            _editingId = c.Id;

            // Copy row values into edit model
            _editModel = new Competitions
            {
                Id = c.Id,
                Title = c.Title,
                Code = c.Code
            };
        }

        private void CancelEdit()
        {
            _editingId = null;
            _editModel = new Competitions();
        }

        // ----------------------------
        // UPDATE
        // ----------------------------

        private async Task SaveEditAsync()
        {
            _error = null;

            if (_editingId is null)
                return;

            if (string.IsNullOrWhiteSpace(_editModel.Title))
            {
                _error = "Title is required.";
                return;
            }

            await Service.UpdateAsync(_editModel);

            // Exit edit mode and refresh list
            CancelEdit();
            await ReloadAsync();
        }

        // ----------------------------
        // DELETE (soft + hard)
        // ----------------------------

        private async Task SoftDeleteAsync(int id)
        {
            _error = null;

            // Soft delete sets Active=false; record will vanish after ReloadAsync()
            await Service.SoftDeleteAsync(id);

            // If we were editing this row, exit edit mode
            if (_editingId == id)
                CancelEdit();

            await ReloadAsync();
        }

        private async Task HardDeleteAsync(int id)
        {
            _error = null;

            // Hard delete permanently removes the record
            var deleted = await Service.HardDeleteAsync(id);
            if (!deleted)
            {
                _error = $"Competition {id} not found.";
                return;
            }

            if (_editingId == id)
                CancelEdit();

            await ReloadAsync();
        }
    }
}