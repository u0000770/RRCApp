using Microsoft.AspNetCore.Components;
using RRCServices;

namespace RRCApp.Components.Pages
{
    public partial class RaceEvents
    {
        // ----------------------------
        // List state
        // ----------------------------
        private List<RaceEventListItemDTO>? _list;
        private bool _activeOnly = true;
        private string? _listError;

        // ----------------------------
        // Details state
        // ----------------------------
        private int? _selectedId;
        private RaceEventDetailsDTO? _details;
        private bool _detailsLoading;
        private string? _detailsError;

        // ----------------------------
        // Sorting state
        // ----------------------------
        private string _sortColumn = "Date";
        private bool _sortAscending = true;

        // ----------------------------
        // Edit panel state (date + title only)
        // ----------------------------
        private bool _isEditingDetails = false;

        // Model used by the edit form (we keep it separate from _details so we can cancel safely)
        private RaceEventDetailsEditModel _editDetailsModel = new();

        // We bind DateOnly via a string input for maximum compatibility
        private string _editDetailsDateText = "";

        protected override async Task OnInitializedAsync()
        {
            await ReloadListAsync();
        }

        private async Task ReloadListAsync()
        {
            _listError = null;

            try
            {
                _list = await Service.GetRaceEventListAsync(activeOnly: _activeOnly);
                ApplySort();

                if (_selectedId is not null && _list.All(x => x.RaceEventId != _selectedId.Value))
                {
                    _selectedId = null;
                    _details = null;
                    _detailsError = null;
                    _isEditingDetails = false;
                }
            }
            catch (Exception ex)
            {
                _listError = ex.Message;
                _list = new List<RaceEventListItemDTO>();
            }
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
            if (_list is null) return;

            _list = (_sortColumn, _sortAscending) switch
            {
                ("Title", true) => _list.OrderBy(x => x.EventTitle).ThenBy(x => x.Date).ToList(),
                ("Title", false) => _list.OrderByDescending(x => x.EventTitle).ThenBy(x => x.Date).ToList(),

                ("Date", true) => _list.OrderBy(x => x.Date).ThenBy(x => x.EventTitle).ToList(),
                ("Date", false) => _list.OrderByDescending(x => x.Date).ThenBy(x => x.EventTitle).ToList(),

                _ => _list
            };
        }

        private string SortIndicator(string column)
        {
            if (_sortColumn != column) return "";
            return _sortAscending ? "▲" : "▼";
        }

        private async Task ShowDetailsAsync(int raceEventId)
        {
            _selectedId = raceEventId;

            _detailsLoading = true;
            _detailsError = null;
            _details = null;
            _isEditingDetails = false;

            try
            {
                _details = await Service.GetDetailsAsync(raceEventId);
            }
            catch (Exception ex)
            {
                _detailsError = ex.Message;
            }
            finally
            {
                _detailsLoading = false;
            }
        }

        // ============================================================
        // Button handlers (you said: build UI + empty handlers)
        // ============================================================

        // Button 1: navigate to a future results page for this race event
        private void GoToResultsPage()
        {
            if (_details is null) return;

            // TODO: implement this page later
            // Example route: /raceevents/{id}/results OR /results?raceEventId=123
            Nav.NavigateTo($"/raceeventresults/{_details.RaceEventId}");
        }

        // Button 3: navigate to a future "create race event" page
        private void GoToCreateRaceEventPage()
        {
            // TODO: implement this page later
            // Example route: /raceevents/create
            Nav.NavigateTo("/raceevents/create");
        }

        [Parameter] public int RaceEventId { get; set; }

        private void GoToRaceEventPrediction()
        {
            // $"/raceeventresults/{_details.RaceEventId}"
            Nav.NavigateTo($"/raceeventprediction?raceEventId={_details.RaceEventId}");
        }

        // Button 2: switch details panel into edit mode (date + title only)
        private void BeginEditDetails()
        {
            if (_details is null) return;

            _isEditingDetails = true;

            // Copy current details into an edit model so cancel is safe
            _editDetailsModel = new RaceEventDetailsEditModel
            {
                RaceEventId = _details.RaceEventId,
                EventId = _details.EventId,
                EventTitle = _details.EventTitle
            };

            // Bind date as text for easy editing (YYYY-MM-DD)
            _editDetailsDateText = _details.Date.ToString("yyyy-MM-dd");
        }

        private void CancelEditDetails()
        {
            _isEditingDetails = false;
            _editDetailsModel = new RaceEventDetailsEditModel();
            _editDetailsDateText = "";
        }

        // Save button in the edit panel
        // You asked for "empty event handlers" — this is a stub you can fill in later.
        private async Task SaveDetailsEditsAsync()
        {
            // TODO: Implement save logic later
            // - Update RaceEvent.Date via Service.UpdateDateAsync(new RaceEventUpdateDTO { RaceEventId = ..., Date = ... })
            // - Update Event.Title via an EventService method you add later (since title belongs to Events template)

            // Example parsing for DateOnly:
            if (!DateOnly.TryParse(_editDetailsDateText, out var parsedDate))
            {
                _detailsError = "Invalid date format. Use YYYY-MM-DD.";
                return;
            }

            // Example call (commented out until you implement it):
            // await Service.UpdateDateAsync(new RaceEventUpdateDTO { RaceEventId = _editDetailsModel.RaceEventId, Date = parsedDate });

            // Example event title update (future):
            // await EventsService.UpdateTitleAsync(_editDetailsModel.EventId, _editDetailsModel.EventTitle);

            // For now, just exit edit mode (you can remove this once you wire real updates)
            _isEditingDetails = false;

            // Optional: reload details after saving
            // await ShowDetailsAsync(_editDetailsModel.RaceEventId);
        }

        // A tiny edit model for the details edit panel
        private sealed class RaceEventDetailsEditModel
        {
            public int RaceEventId { get; set; }
            public int EventId { get; set; }
            public string? EventTitle { get; set; }
        }
    }
}