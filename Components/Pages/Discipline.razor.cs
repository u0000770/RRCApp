using RRCServices;

namespace RRCApp.Components.Pages
{
    public partial class Discipline
    {
        private List<DisciplineDTO>? _items;

        private DisciplineDTO _newModel = new();
        private DisciplineDTO _editModel = new();
        private int? _editingId;

        private string? _error;

        protected override async Task OnInitializedAsync()
            => await ReloadAsync();

        private async Task ReloadAsync()
        {
            _error = null;
            _items = await Service.GetAllActiveAsync();
        }

        private async Task AddAsync()
        {
            _error = null;

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

            await Service.AddAsync(_newModel);
            _newModel = new DisciplineDTO();
            await ReloadAsync();
        }

        private void StartEdit(DisciplineDTO d)
        {
            _error = null;
            _editingId = d.id;

            _editModel = new DisciplineDTO
            {
                id = d.id,
                Code = d.Code,
                Name = d.Name
            };
        }

        private void CancelEdit()
        {
            _editingId = null;
            _editModel = new DisciplineDTO();
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

            await Service.UpdateAsync(_editModel);
            CancelEdit();
            await ReloadAsync();
        }

        private async Task SoftDeleteAsync(int id)
        {
            _error = null;
            await Service.SoftDeleteAsync(id);

            if (_editingId == id)
                CancelEdit();

            await ReloadAsync();
        }

        private async Task HardDeleteAsync(int id)
        {
            _error = null;

            var deleted = await Service.HardDeleteAsync(id);
            if (!deleted)
            {
                _error = $"Discipline {id} not found.";
                return;
            }

            if (_editingId == id)
                CancelEdit();

            await ReloadAsync();
        }
    }
}