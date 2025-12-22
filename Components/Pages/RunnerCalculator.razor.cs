using Microsoft.AspNetCore.Components;
using RRCServices;
using RRCServices.Calculator;
using RRCServices.Runner;

namespace RRCApp.Components.Pages
{
    public partial class RunnerCalculator
    {
        // Query string: /calcrace?runnerId=123
        [SupplyParameterFromQuery] public int runnerId { get; set; }

        private bool _loading = true;
        private bool _saving = false;

        private RunnerDetailsDto? _runner;

        // Race events around Clock.Now
        private List<RaceEventListItemDTO> _raceEvents = new();
        private List<RaceEventListItemDTO> _past3 = new();
        private List<RaceEventListItemDTO> _next3 = new();

        private int? _selectedRaceEventId;

        private int? SelectedRaceEventId
        {
            get => _selectedRaceEventId;
            set
            {
                if (_selectedRaceEventId == value) return;

                _selectedRaceEventId = value;

                // ✅ this is where your breakpoint should go
                OnRaceSelectedIdChanged(value);
            }
        }

        private void OnRaceSelectedIdChanged(int? id)
        {
            _predictedSeconds = null;
            _calcMessage = null;
            _saveMessage = null;
            _existingForSelected = null;

            if (id is null)
            {
                _selectedRaceEvent = null;
                return;
            }

            _selectedRaceEvent = _raceEvents.SingleOrDefault(x => x.RaceEventId == id.Value);

            if (_runner is not null && _selectedRaceEvent is not null)
            {
                _existingForSelected = _runner.EventTimes.FirstOrDefault(t =>
                    t.EventId == _selectedRaceEvent.EventId
                    && t.RaceDate.HasValue
                    && t.RaceDate.Value.Date == _selectedRaceEvent.Date.Date
                );
            }
        }


        private RaceEventListItemDTO? _selectedRaceEvent;
        private EventRaceTimesDto? _existingForSelected;

        private int? _predictedSeconds;
        private string? _calcMessage;
        private string? _saveMessage;

        protected override async Task OnParametersSetAsync()
        {
            _loading = true;

            _predictedSeconds = null;
            _calcMessage = null;
            _saveMessage = null;

            // Option A: reset via property so it clears selection consistently
            SelectedRaceEventId = null;

            _selectedRaceEvent = null;
            _existingForSelected = null;

            if (runnerId <= 0)
            {
                _runner = null;
                _loading = false;
                return;
            }

            _runner = await RunnerService.GetRunnerDetailsAsync(runnerId, includeInactiveTimes: false);

            await LoadRaceEventsAroundToday();

            _loading = false;
        }

        private async Task LoadRaceEventsAroundToday()
        {
            _raceEvents.Clear();
            _past3.Clear();
            _next3.Clear();

            var from = Clock.Now.AddMonths(-6);
            var to = Clock.Now.AddMonths(6);

            _raceEvents = await RaceEventService.GetRaceEventListAsync(
                activeOnly: true,
                distanceCode: null,
                from: from,
                to: to
            );

            var today = Clock.Now.Date;

            _past3 = _raceEvents
                .Where(x => x.Date.Date < today)
                .OrderByDescending(x => x.Date)
                .Take(3)
                .ToList();

            _next3 = _raceEvents
                .Where(x => x.Date.Date >= today)
                .OrderBy(x => x.Date)
                .Take(3)
                .ToList();
        }

        // ✅ Option A "reaction" method: called from SelectedRaceEventId setter
        private void ApplySelectedRaceEvent()
        {
            _predictedSeconds = null;
            _calcMessage = null;
            _saveMessage = null;
            _existingForSelected = null;

            if (_selectedRaceEventId is null)
            {
                _selectedRaceEvent = null;
                return;
            }

            _selectedRaceEvent = _raceEvents.SingleOrDefault(x => x.RaceEventId == _selectedRaceEventId.Value);

            if (_runner is not null && _selectedRaceEvent is not null)
            {
                // Best-effort match: same EventId + same date
                _existingForSelected = _runner.EventTimes.FirstOrDefault(t =>
                    t.EventId == _selectedRaceEvent.EventId
                    && t.RaceDate.HasValue
                    && t.RaceDate.Value.Date == _selectedRaceEvent.Date.Date
                );
            }
        }

        private void Calculate()
        {
            var d = Clock.Now.Date;
            _predictedSeconds = null;
            _calcMessage = null;
            _saveMessage = null;

            if (_runner is null || _selectedRaceEvent is null)
            {
                _calcMessage = "Please select a runner and a race event.";
                return;
            }

            var newDistanceMeters = GetRaceEventDistanceMeters(_selectedRaceEvent);

            if (newDistanceMeters <= 0)
            {
                _calcMessage = "Could not determine the selected race distance (meters). Check your RaceEventListItemDTO.";
                return;
            }

            var all = _runner.EventTimes.ToList();
            var c0 = all.Count;
            var s1 = all.Where(t => t.RaceDate.HasValue).ToList();
            var c1 = s1.Count;

            var today = Clock.Now.Date; // capture once
            var s2 = s1.Where(t => t.RaceDate!.Value.Date < today).ToList();
            var c2 = s2.Count;

            var s3 = s2.Where(t => t.ActualSeconds.HasValue).ToList();
            var c3 = s3.Count;

            var s4 = s3.Where(t => t.ActualSeconds!.Value > 0).ToList();
            var c4 = s4.Count;

            var s5 = s4.Where(t => t.DistanceMeters.HasValue).ToList();
            var c5 = s5.Count;

            var s6 = s5.Where(t => t.DistanceMeters!.Value > 0).ToList();
            var c6 = s6.Count;



            var recent = _runner.EventTimes
                .Where(t => t.RaceDate.HasValue && t.RaceDate.Value.Date < Clock.Now.Date)
                .Where(t => t.ActualSeconds.HasValue && t.ActualSeconds.Value > 0)
                .Where(t => t.DistanceMeters.HasValue && t.DistanceMeters.Value > 0)   // ✅ ensure distance exists
                .OrderByDescending(t => t.RaceDate)
                .Take(3)
                .Select(t => new RecentRaceDto
                {
                    Distance = t.DistanceMeters!.Value,          // ✅ meters
                    Actual = t.ActualSeconds!.Value              // ✅ seconds
                })
                .ToList();


            if (recent.Count == 0)
            {
                _calcMessage = "No recent completed races (with actual times) found for this runner — cannot predict.";
                return;
            }


            var predicted = CalculatorService.PredictFromRecentRaces(recent, newDistanceMeters);

            if (predicted is null)
            {
                _calcMessage = "Prediction failed (no usable recent race data).";
                return;
            }

            _predictedSeconds = (int)Math.Round(predicted.Value);
            _calcMessage = $"Prediction calculated using {recent.Count} recent race(s).";
        }

        private async Task ConfirmUpdate()
        {
            if (_runner is null || _selectedRaceEvent is null || _predictedSeconds is null)
                return;

            _saving = true;
            _saveMessage = null;

            try
            {
                var dto = new EventRunnerTimeUpsertDto
                {
                    EventId = _selectedRaceEvent.EventId,
                    RaceEventId = _selectedRaceEvent.RaceEventId,
                    TargetSeconds = _predictedSeconds,
                    Active = true,
                    ActualSeconds = null,
                    Date = _selectedRaceEvent.Date
                };

                if (_existingForSelected is not null)
                {
                    var ok = await RunnerService.UpdateEventRunnerTimeAsync(
                        runnerId: _runner.Id,
                        eventRunnerTimeId: _existingForSelected.EventRunnerTimeId,
                        dto: dto
                    );

                    _saveMessage = ok
                        ? $"Updated target time to {FormatSeconds(_predictedSeconds.Value)}."
                        : "Update failed.";
                }
                else
                {
                    var newId = await RunnerService.CreateEventRunnerTimeAsync(_runner.Id, dto);
                    _saveMessage = $"Created target time ({FormatSeconds(_predictedSeconds.Value)}). New record id: {newId}";

                    _existingForSelected = new EventRaceTimesDto
                    {
                        EventRunnerTimeId = newId,
                        EventId = _selectedRaceEvent.EventId,
                        RaceDistance = "",
                        RaceTitle = _selectedRaceEvent.EventTitle ?? "",
                        TargetTime = _predictedSeconds.Value,
                        RaceTargetTime = FormatSeconds(_predictedSeconds.Value),
                        RaceActualTime = "No Result",
                        RaceDate = _selectedRaceEvent.Date,
                        TimeDifference = "",
                        AgeGrade = 0
                    };
                }
            }
            finally
            {
                _saving = false;
            }
        }

        private void BackToRunnerDetails()
            => Nav.NavigateTo($"/runnerdetails?runnerId={runnerId}");

        private void BackToRunners() => Nav.NavigateTo("/runners");

        private static string FormatRaceEvent(RaceEventListItemDTO re)
        {
            var title = string.IsNullOrWhiteSpace(re.EventTitle) ? $"Event {re.EventId}" : re.EventTitle;
            return $"{re.Date:dd MMM yyyy} — {title}";
        }

        private static string FormatSeconds(int seconds)
        {
            if (seconds <= 0) return "No Result";
            var t = TimeSpan.FromSeconds(seconds);
            return $"{(int)t.TotalHours:D2}h:{t.Minutes:D2}m:{t.Seconds:D2}s";
        }

        // ------------------------------------------------------------
        // Distance helpers (self-contained)
        // ------------------------------------------------------------
        private static double GetRaceEventDistanceMeters(RaceEventListItemDTO re)
        {
            return re.DistanceMeters;
        }

       


    }
}