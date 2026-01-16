using RRCServices;
using RRCServices.Runner;
using System.ComponentModel.DataAnnotations;

namespace RRCApp.Components.Pages
{
    public partial class AddRunner
    {
        private string? _error;
        private string? _success;

        private string? _timeSuccess;
        private string? _timeError;
        private int? _lastCreatedEventRunnerTimeId;

        private bool _busyCreateRunner;
        private bool _busyCreateTime;

        private int? _createdRunnerId;

        private RaceEventCreateLookupsDTO? _lookups;
        private string? _selectedDistanceCode;
        private List<EventLookupDTO>? _eventsForDistance;

        private RunnerFormModel _runnerForm = new();
        private TimeFormModel _timeForm = new();

        protected override async Task OnInitializedAsync()
        {
            _lookups = await RaceEventService.GetCreateLookupsAsync(distanceCode: null);
        }

        private void RunnerInvalid()
        {
            _error = "Runner form is invalid - OnValidSubmit won't run.";
        }

        private async Task CreateRunnerAsync()
        {
            _error = null;
            _success = null;

            try
            {
                _busyCreateRunner = true;

                var dto = new RunnerUpsertDto
                {
                    Firstname = _runnerForm.Firstname,
                    Secondname = _runnerForm.Secondname,
                    Ukan = _runnerForm.Ukan,
                    Dob = _runnerForm.DobDate.HasValue
                    ? DateOnly.FromDateTime(_runnerForm.DobDate.Value)
                    : null,
                    Email = _runnerForm.Email,
                    Active = _runnerForm.Active,
                    AgeGradeCode = _runnerForm.AgeGradeCode,
                    Gender = _runnerForm.Gender
                };

                var id = await RunnerService.CreateRunnerAsync(dto);
                _createdRunnerId = id;

                _success = $"Runner created (Id={id}). You can now add an EventRunnerTime.";

                _timeForm.Date = DateTime.Today;
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
            finally
            {
                _busyCreateRunner = false;
            }
        }

        private async Task OnDistanceChangedAsync()
        {
            _error = null;
            _success = null;

            _eventsForDistance = null;
            _timeForm.EventId = 0;

            if (string.IsNullOrWhiteSpace(_selectedDistanceCode))
                return;

            _eventsForDistance = await RaceEventService.GetActiveEventsByDistanceAsync(_selectedDistanceCode);
        }

        private async Task CreateTimeAsync()
        {
            _error = null;
            _success = null;

            if (_createdRunnerId is null)
            {
                _error = "Create the runner first.";
                return;
            }

            if (_timeForm.EventId <= 0)
            {
                _error = "Please select an event.";
                return;
            }

            // Convert HH:MM:SS -> seconds (nullable)
            if (!TryParseHmsToSeconds(_timeForm.TargetTimeText, out var targetSeconds, out var targetErr))
            {
                _error = targetErr;
                return;
            }

            if (!TryParseHmsToSeconds(_timeForm.ActualTimeText, out var actualSeconds, out var actualErr))
            {
                _error = actualErr;
                return;
            }

            try
            {
                _busyCreateTime = true;

                var dto = new EventRunnerTimeUpsertDto
                {
                    EventId = _timeForm.EventId,
                    RaceEventId = _timeForm.RaceEventId,
                    TargetSeconds = targetSeconds,
                    ActualSeconds = actualSeconds,
                    Date = _timeForm.Date,
                    Active = _timeForm.Active
                };

                var newId = await RunnerService.CreateEventRunnerTimeAsync(_createdRunnerId.Value, dto);
                _success = $"EventRunnerTime added (Id={newId}).";

                _lastCreatedEventRunnerTimeId = newId;
                _timeSuccess = $"EventRunnerTime added successfully (Id={newId}).";
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
            finally
            {
                _busyCreateTime = false;
            }
        }

        private void GoToRunnerDetails()
        {
            if (_createdRunnerId is null) return;
            Nav.NavigateTo($"/runners/{_createdRunnerId.Value}");
        }

        // Accepts:
        //  - blank/null => returns null seconds (meaning "no value")
        //  - HH:MM:SS or MM:SS (we normalise)
        private static bool TryParseHmsToSeconds(string? input, out int? seconds, out string? error)
        {
            seconds = null;
            error = null;

            if (string.IsNullOrWhiteSpace(input))
                return true; // optional

            var s = input.Trim();

            // Allow MM:SS by prefixing 00:
            var parts = s.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                s = $"00:{parts[0]}:{parts[1]}";
                parts = s.Split(':', StringSplitOptions.RemoveEmptyEntries);
            }

            if (parts.Length != 3)
            {
                error = "Time must be HH:MM:SS (or MM:SS). Example: 00:19:30";
                return false;
            }

            if (!int.TryParse(parts[0], out var hh) ||
                !int.TryParse(parts[1], out var mm) ||
                !int.TryParse(parts[2], out var ss))
            {
                error = "Time must contain numbers only. Example: 00:19:30";
                return false;
            }

            if (hh < 0 || mm < 0 || mm > 59 || ss < 0 || ss > 59)
            {
                error = "Minutes/seconds must be 00-59. Example: 00:19:30";
                return false;
            }

            seconds = (hh * 3600) + (mm * 60) + ss;
            return true;
        }

        private sealed class RunnerFormModel
        {
            [Required]
            public string Firstname { get; set; } = "";

            [Required]
            public string Secondname { get; set; } = "";

            public string? Ukan { get; set; }

            // 👇 UI-friendly
            public DateTime? DobDate { get; set; }

            [EmailAddress]
            public string? Email { get; set; }

            public bool? Active { get; set; }
            public string? AgeGradeCode { get; set; }

            [Required]
            public string Gender { get; set; } = "Unknown";
        }


        private sealed class TimeFormModel
        {
            public int EventId { get; set; }
            public int? RaceEventId { get; set; }

            // UI fields (HH:MM:SS)
            public string? TargetTimeText { get; set; }
            public string? ActualTimeText { get; set; }

            public DateTime? Date { get; set; }
            public bool? Active { get; set; } = true;
        }
    }
}