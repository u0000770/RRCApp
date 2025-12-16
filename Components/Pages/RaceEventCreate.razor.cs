using Microsoft.AspNetCore.Components;
using RRCServices;

namespace RRCApp.Components.Pages
{
    public partial class RaceEventCreate
    {
        // ----------------------------
        // Page state
        // ----------------------------
        private bool _loading = true;
        private bool _saving = false;

        private string? _error;
        private string? _success;
        private bool _duplicateWarning;

        // Distance selection (string code). Separate from DTO.
        private string? _selectedDistanceCode;

        // Lookup lists for create page
        private RaceEventCreateLookupsDTO _lookups = new();

        // Create form model (your DTO)
        private RaceEventCreateDTO _model = new()
        {
            Date = DateTime.Today,
            Active = true
        };

        // Enable Event dropdown only after distance chosen
        private bool _eventsEnabled => !string.IsNullOrWhiteSpace(_selectedDistanceCode);

        // ----------------------------
        // Load initial lookups
        // ----------------------------
        protected override async Task OnInitializedAsync()
        {
            await LoadLookupsAsync(distanceCode: null);
            _loading = false;
        }

        // Loads distances always; loads events only if distanceCode is provided
        private async Task LoadLookupsAsync(string? distanceCode)
        {
            _error = null;

            try
            {
                _lookups = await Service.GetCreateLookupsAsync(distanceCode);
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                _lookups = new RaceEventCreateLookupsDTO();
            }
        }

        // ----------------------------
        // Distance changed (async)
        // ----------------------------
        private async Task OnDistanceChangedAsync(ChangeEventArgs e)
        {
            _error = null;
            _success = null;
            _duplicateWarning = false;

            // Update selected distance from UI
            _selectedDistanceCode = e.Value?.ToString();

            // Reset event selection whenever distance changes
            _model.EventId = 0;

            if (string.IsNullOrWhiteSpace(_selectedDistanceCode))
            {
                // Clear events list if nothing selected
                _lookups.Events = new List<EventLookupDTO>();
                return;
            }

            // Reload lookups, including events filtered by distance
            await LoadLookupsAsync(_selectedDistanceCode);
        }

        // ----------------------------
        // Create RaceEvent
        // ----------------------------
        private async Task CreateAsync()
        {
            _error = null;
            _success = null;
            _duplicateWarning = false;

            // Simple manual validation
            if (string.IsNullOrWhiteSpace(_selectedDistanceCode))
            {
                _error = "Please select a distance first.";
                return;
            }

            if (_model.EventId <= 0)
            {
                _error = "Please select an event.";
                return;
            }

            if (_model.Date == default)
            {
                _error = "Please select a date.";
                return;
            }

            // Normalise to date-only semantics (ignore time)
            _model.Date = _model.Date.Date;

            _saving = true;

            try
            {
                // Duplicate check (EventId + Date)
                var exists = await Service.ExistsAsync(_model.EventId, _model.Date);
                if (exists)
                {
                    _duplicateWarning = true;
                    return;
                }

                var newId = await Service.CreateAsync(_model);

                _success = $"Created RaceEvent #{newId}.";

                // Navigate back to list (or your details route if you have one)
                Nav.NavigateTo("/raceevents");
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
            finally
            {
                _saving = false;
            }
        }

        private void GoBack()
        {
            Nav.NavigateTo("/raceevents");
        }
    }
}