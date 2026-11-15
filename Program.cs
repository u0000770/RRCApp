using Microsoft.EntityFrameworkCore;
using RRCApp.Components;
using RRCConfig.Data;
using RRCDataModel.Data;
using RRCServices;
using RRCServices.Calculator;
using RRCServices.Calculator.RRCServices;
using RRCServices.Clock;
using RRCServices.League;
using RRCServices.League.Admin;
using RRCServices.RRCServices.League.Admin;
using RRCServices.Runner;
using RRCServices.Season;



namespace RRCApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //        builder.Services.AddDbContextFactory<RRCContext>(options =>
            //options.UseSqlServer(builder.Configuration.GetConnectionString("RRC")));

            var cs = builder.Configuration.GetConnectionString("RRCAzure");
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("Connection string 'RRCAzure' not found.");

            ///
            builder.Services.AddDbContextFactory<RRCContext>(options =>
    options.UseSqlServer(cs, sql =>
        sql.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

            ///


            //builder.Services.AddDbContextFactory<RRCContext>(options =>
            //    options.UseSqlServer(cs));

            builder.Services.AddScoped<DistanceService>();
            builder.Services.AddScoped<CompetitionService>();
            builder.Services.AddScoped<DisciplineService>();
            builder.Services.AddScoped<EventService>();
            builder.Services.AddScoped<CalculatorService>();
            builder.Services.AddScoped<IRaceEventService, RaceEventService>();
            builder.Services.AddScoped<ITrophyCalculator, TrophyCalculator>();
            builder.Services.AddScoped<ITimeFormatter, TimeFormatter>();
            builder.Services.AddScoped<IRaceResultService,RaceResultService>();
            builder.Services.AddScoped<IRunnerService,RunnerService>();
            builder.Services.AddScoped<IRacePredictionService, RacePredictionService>();
            builder.Services.AddScoped<IRunnerLeagueSummaryService, RunnerLeagueSummaryService>();

           // builder.Services.AddScoped<ILeagueDataService, LeagueDataService>();


            // SEASON WORK (batch 5): SystemClock is now RRCServices.Clock.SystemClock
            // (UK time). RRCApp's own copy in Class.cs has been deleted.
            builder.Services.AddScoped<IClock, SystemClock>();

            // SEASON WORK (batch 2): seasons live in config.Season in the same
            // database, via their own small SeasonContext (RRCConfig project).
            // RacePredictionService, RunnerCalculator and LeagueSummary use it.
            builder.Services.AddDbContextFactory<SeasonContext>(options =>
                options.UseSqlServer(cs, sql =>
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null)));
            builder.Services.AddScoped<ISeasonService, SeasonService>();

            // SEASON WORK (batch 5): the old JSON season settings registrations
            // (Configure<SeasonSettings> x2, ISeasonSettingsStore /
            // JsonSeasonSettingsStore, ISeasonSettingsService) have been removed.
            // Seasons are now managed on /admin/season-settings via ISeasonService.



            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
