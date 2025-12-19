using Microsoft.AspNetCore.Components;
using RRCServices.Runner;
using System.ComponentModel.DataAnnotations;

namespace RRCApp.Components.Pages
{
    public partial class RunnerDetail
    {
        [Parameter] public int RunnerId { get; set; }

        private RunnerDetailsDto? _runner;
        private List<EventRaceTimesDto> _sortedTimes = new();

        private bool _loading = true;
        private string? _error;
        private string? _success;

        private bool _includeInactiveTimes = false;

        // Sorting
        private string _sortColumn = "Date";
        private bool _sortAscending = false; // most recent first by default

        protected override async Task OnParametersSetAsync()
        {
            await ReloadAsync();
        }

        private async Task ReloadAsync()
        {
            _error = null;
            _loading = true;

            try
            {
                _runner = await RunnerService.GetRunnerDetailsAsync(
                    RunnerId,
                    includeInactiveTimes: _includeInactiveTimes);

                if (_runner is not null)
                {
                    _sortedTimes = _runner.EventTimes.ToList();
                    ApplySort();
                }
                else
                {
                    _sortedTimes.Clear();
                }
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                _runner = null;
                _sortedTimes.Clear();
            }
            finally
            {
                _loading = false;
            }
        }

        // ----------------------------
        //Edit  logic
        // ----------------------------


        private bool _editing;
        private bool _busySave;

        private RunnerEditFormModel _editForm = new();
        private bool? _originalActive; // to preserve current active if user leaves blank

        private void BeginEdit()
        {
            if (_runner is null) return;

            _error = null;
            _success = null;

            _editing = true;

            _originalActive = _runner.Active;

            _editForm = new RunnerEditFormModel
            {
                Firstname = _runner.Firstname,
                Secondname = _runner.Secondname,
                Ukan = _runner.Ukan,
                Dob = _runner.Dob,
                Email = _runner.Email,
                AgeGradeCode = _runner.AgeGradeCode,
                Gender = _runner.Gender,
                Active = null // null means “leave unchanged”
            };
        }

        private void CancelEdit()
        {
            _editing = false;
            _busySave = false;
        }

        private async Task SaveRunnerAsync()
        {
            if (_runner is null) return;

            _error = null;
            _success = null;

            try
            {
                _busySave = true;

                var dto = new RunnerUpsertDto
                {
                    Firstname = _editForm.Firstname,
                    Secondname = _editForm.Secondname,
                    Ukan = _editForm.Ukan,
                    Dob = _editForm.Dob,
                    Email = _editForm.Email,
                    AgeGradeCode = _editForm.AgeGradeCode,
                    Gender = _editForm.Gender,

                    // if user left Active blank, keep existing
                    Active = _editForm.Active ?? _originalActive
                };

                var ok = await RunnerService.UpdateRunnerAsync(RunnerId, dto);
                if (!ok)
                {
                    _error = "Runner not found (update failed).";
                    _runner = null;
                    _sortedTimes.Clear();
                    return;
                }

                _success = "Runner updated.";
                _editing = false;

                await ReloadAsync(); // re-fetch updated details
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
            finally
            {
                _busySave = false;
            }
        }

        private sealed class RunnerEditFormModel
        {
            [Required] public string Firstname { get; set; } = "";
            [Required] public string Secondname { get; set; } = "";
            public string? Ukan { get; set; }
            public DateOnly? Dob { get; set; }

            [EmailAddress]
            public string? Email { get; set; }

            public string? AgeGradeCode { get; set; }

            [Required]
            public string Gender { get; set; } = "Unknown";

            public bool? Active { get; set; } // null => keep existing
        }



        // ----------------------------
        // Sorting logic
        // ----------------------------
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
            _sortedTimes = (_sortColumn, _sortAscending) switch
            {
                ("Date", true) =>
                    _sortedTimes.OrderBy(t => t.RaceDate).ToList(),

                ("Date", false) =>
                    _sortedTimes.OrderByDescending(t => t.RaceDate).ToList(),

                ("Event", true) =>
                    _sortedTimes.OrderBy(t => t.RaceTitle).ToList(),

                ("Event", false) =>
                    _sortedTimes.OrderByDescending(t => t.RaceTitle).ToList(),

                ("Distance", true) =>
                    _sortedTimes.OrderBy(t => t.RaceDistance).ToList(),

                ("Distance", false) =>
                    _sortedTimes.OrderByDescending(t => t.RaceDistance).ToList(),

                ("AgeGrade", true) =>
                    _sortedTimes.OrderBy(t => t.AgeGrade).ToList(),

                ("AgeGrade", false) =>
                    _sortedTimes.OrderByDescending(t => t.AgeGrade).ToList(),

                _ => _sortedTimes
            };
        }

        private string SortIndicator(string column)
        {
            if (_sortColumn != column) return "";
            return _sortAscending ? "▲" : "▼";
        }

        private void BackToList()
            => Nav.NavigateTo("/runners");

        private void CalcRace(int runnerId)
         => Nav.NavigateTo($"/calcrunnerrace?runnerId={runnerId}");
        // CalcRace
        // Edit region

    }
}