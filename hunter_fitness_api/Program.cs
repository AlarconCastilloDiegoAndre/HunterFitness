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
            
            // Only in development
            var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
            if (environment == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });
        
        // CORS for development
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });
        
        // Custom services - CORRECT ORDER to avoid circular dependencies
        // AuthService first (no dependencies on other custom services)
        services.AddScoped<IAuthService, AuthService>();
        
        // HunterService next (depends only on DbContext)
        services.AddScoped<IHunterService, HunterService>();
        
        // AchievementService (may depend on HunterService)
        services.AddScoped<IAchievementService, AchievementService>();
        
        // QuestService (may depend on HunterService)
        services.AddScoped<IQuestService, QuestService>();
        
        // DungeonService and EquipmentService require IHunterService, so they go after
        services.AddScoped<IDungeonService, DungeonService>();
        services.AddScoped<IEquipmentService, EquipmentService>();
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
        
        // Try to apply pending migrations in development
        var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
        if (environment == "Development")
        {
            try
            {
                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("📊 Applying {Count} pending migrations...", pendingMigrations.Count());
                    await dbContext.Database.MigrateAsync();
                    logger.LogInformation("✅ Migrations applied successfully!");
                }
            }
            catch (Exception migrationEx)
            {
                logger.LogWarning(migrationEx, "⚠️ Could not apply migrations automatically");
            }
        }
    }
    else
    {
        logger.LogError("❌ Database connection failed!");
    }
}
catch (Exception ex)
{
    using var scope = host.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "💀 Error during database connection check");
}

// Start the application
host.Run();