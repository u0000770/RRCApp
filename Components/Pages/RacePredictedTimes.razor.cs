using Microsoft.AspNetCore.Components;
using RRCServices.Calculator.RRCServices;

namespace RRCApp.Components.Pages
{
    public partial class RacePredictedTimes
    {
        // query string: /raceeventprediction?raceEventId=123
        [SupplyParameterFromQuery(Name = "raceEventId")]
        public int RaceEventId { get; set; }

        private readonly int _lastN = 3; // business rule

        private bool _running;
        private string? _error;
        private string? _info;

        private List<RunnerPredictionDto> _results = new();

        private string _raceTitle = "";
        private string _raceDateText = "";

        protected override async Task OnParametersSetAsync()
        {
            // If the URL has a raceEventId, auto-load predictions
            if (RaceEventId > 0)
            {
                await RunPrediction();
            }
        }

        private async Task RunPrediction()
        {
            _running = true;
            _error = null;
            _info = null;
            _results.Clear();
            _raceTitle = "";
            _raceDateText = "";

            try
            {
                if (RaceEventId <= 0)
                {
                    _error = "Please enter a valid Race Event Id.";
                    return;
                }

                var res = await PredictionService.PredictTargetsForRaceEventAsync(
                    raceEventId: RaceEventId,
                    lastN: _lastN);

                _results = res ?? new List<RunnerPredictionDto>();

                if (_results.Count == 0)
                {
                    _info = "No predictions returned (no eligible runners or race event not found/active).";
                    return;
                }

                // derive race details from first row (all rows share the same event)
                var first = _results[0];
                _raceTitle = string.IsNullOrWhiteSpace(first.EventTitle) ? $"Race Event {RaceEventId}" : first.EventTitle;
                _raceDateText = first.RaceDate.ToString("dd MMM yyyy");

                // optional sort: fastest first
                _results = _results
                    .Where(x => x.PredictedTargetSeconds.HasValue && x.PredictedTargetSeconds.Value > 0)
                    .OrderBy(x => x.PredictedTargetSeconds)
                    .ToList();

                _info = $"Predictions generated and staged into Memory - press Confirm to PUBLISH them (last {_lastN} races).";
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
            finally
            {
                _running = false;
            }
        }

        private async Task ConfirmApply()
        {
            _running = true;
            _error = null;
            _info = null;

            try
            {
                if (RaceEventId <= 0)
                {
                    _error = "Please enter a valid Race Event Id.";
                    return;
                }

                var result = await PredictionService.ApplyPredictionsForRaceEventAsync(RaceEventId);

                _info = result?.Message
                    ?? "Apply complete.";

                // Optionally re-run predictions to reflect any future UI enhancements
                // (not strictly required for this simplified table)
                // await RunPrediction();
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
            finally
            {
                _running = false;
            }
        }

        private static string FormatHms(int? seconds)
        {
            if (!seconds.HasValue || seconds.Value <= 0) return "-";
            var ts = TimeSpan.FromSeconds(seconds.Value);
            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }
}