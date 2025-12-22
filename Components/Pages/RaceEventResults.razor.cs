//using Microsoft.AspNetCore.Components;
//using RRCServices;

//namespace RRCApp.Components.Pages
//{
//    public partial class RaceEventResults
//    {
//        [Parameter] public int RaceEventId { get; set; }

//        private bool _loading = true;
//        private string? _error;
//        private RaceResultPageDTO? _page;

//        protected override async Task OnParametersSetAsync()
//        {
//            _loading = true;
//            _error = null;
//            _page = null;

//            try
//            {
//                _page = await ResultsService.GetResultsForRaceEventAsync(RaceEventId, includeOnlyCompleteTimes: true);
//            }
//            catch (Exception ex)
//            {
//                _error = ex.Message;
//            }
//            finally
//            {
//                _loading = false;
//            }
//        }
//    }
//}