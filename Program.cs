using Microsoft.EntityFrameworkCore;
using RRCApp.Components;
using RRCApp.Season;
using RRCDataModel.Data;
using RRCServices;
using RRCServices.Calculator;
using RRCServices.Calculator.RRCServices;
using RRCServices.Clock;
using RRCServices.League;
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

            builder.Services.AddScoped<ILeagueDataService, LeagueDataService>();


            builder.Services.AddScoped<IClock, SystemClock>();





            builder.Services.Configure<SeasonSettings>(
                builder.Configuration.GetSection("SeasonSettings"));

            // Defaults live in appsettings.json (WEB APP)
            builder.Services.Configure<SeasonSettings>(
                builder.Configuration.GetSection("SeasonSettings"));

            // Choose a persistence file path (NOT appsettings.json)
            // This creates: <contentroot>/App_Data/seasonSettings.json
            var seasonSettingsPath = Path.Combine(
                builder.Environment.ContentRootPath,
                "App_Data",
                "seasonSettings.json");

            builder.Services.AddSingleton<ISeasonSettingsStore>(
                _ => new JsonSeasonSettingsStore(seasonSettingsPath));

            // Service can be Singleton because:
            // - settings are global
            // - it uses a thread-safe store + in-memory cache
            builder.Services.AddSingleton<ISeasonSettingsService, SeasonSettingsService>();



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
