using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.EntityFrameworkCore;
using HunterFitness.API.Data;
using HunterFitness.API.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        // Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Entity Framework - Using connection string
        var connectionString = Environment.GetEnvironmentVariable("HunterFitnessDB") ??
                             "Server=tcp:hunter-fitness-server.database.windows.net,1433;Initial Catalog=HunterFitnessDB;Persist Security Info=False;User ID=hunterfitness_admin;Password=HunterFit2025!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        services.AddDbContext<HunterFitnessDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
            });
        });

        // Services - Solo agregar los que existen
        services.AddScoped<IQuestService, QuestService>();
        services.AddScoped<IHunterService, HunterService>();
        services.AddScoped<IDungeonService, DungeonService>();
        services.AddScoped<IEquipmentService, EquipmentService>();
        services.AddScoped<IAchievementService, AchievementService>();
        services.AddScoped<AuthService>();

        // DungeonService and EquipmentService require IHunterService, so they go after
        // services.AddScoped<IDungeonService, DungeonService>();
        // services.AddScoped<IEquipmentService, EquipmentService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.AddApplicationInsights();

        // Only in development, show detailed logs
        var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
        if (environment == "Development")
        {
            logging.SetMinimumLevel(LogLevel.Information);
        }
        else
        {
            logging.SetMinimumLevel(LogLevel.Warning);
        }
    })
    .Build();

// Verify database connection at startup
try
{
    using var scope = host.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<HunterFitnessDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    var canConnect = await dbContext.Database.CanConnectAsync();
    
    if (canConnect)
    {
        logger.LogInformation("🏹 Hunter Fitness API - Database connection successful!");
        logger.LogInformation("⚔️ Ready to serve hunters across all realms!");
    }
    else
    {
        logger.LogError("💀 Database connection failed!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"💀 Database connection error: {ex.Message}");
}

Console.WriteLine("🏹 Hunter Fitness Azure Functions API starting...");
host.Run();