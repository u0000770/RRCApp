using Microsoft.EntityFrameworkCore;
using RRCApp.Components;
using RRCDataModel.Data;
using RRCServices;
using RRCServices.Runner;

namespace RRCApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContextFactory<RRCContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RRC")));

            builder.Services.AddScoped<DistanceService>();
            builder.Services.AddScoped<CompetitionService>();
            builder.Services.AddScoped<DisciplineService>();
            builder.Services.AddScoped<EventService>();
            builder.Services.AddScoped<IRaceEventService, RaceEventService>();
            builder.Services.AddScoped<ITrophyCalculator, TrophyCalculator>();
            builder.Services.AddScoped<ITimeFormatter, TimeFormatter>();
            builder.Services.AddScoped<IRaceResultService,RaceResultService>();
            builder.Services.AddScoped<IRunnerService,RunnerService>();


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
