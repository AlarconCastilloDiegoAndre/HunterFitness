using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

        // Entity Framework - Connection string mejorada
        var connectionString = Environment.GetEnvironmentVariable("HunterFitnessDB") ??
                             Environment.GetEnvironmentVariable("SQLAZURECONNSTR_HunterFitnessDB") ??
                             "Server=tcp:hunter-fitness-server.database.windows.net,1433;Initial Catalog=HunterFitnessDB;Persist Security Info=False;User ID=hunterfitness_admin;Password=HunterFit2025!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        services.AddDbContext<HunterFitnessDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(60);
            });
            
            // Solo para development
            var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
            if (environment == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Services - Registrar todos los servicios con sus interfaces
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IQuestService, QuestService>();
        services.AddScoped<IHunterService, HunterService>();
        services.AddScoped<IDungeonService, DungeonService>();
        services.AddScoped<IEquipmentService, EquipmentService>();
        services.AddScoped<IAchievementService, AchievementService>();

        // Configuraciones adicionales
        services.AddMemoryCache();
        services.AddHttpClient();
    })
    .ConfigureLogging(logging =>
    {
        logging.AddApplicationInsights();

        // Configurar niveles de log según el ambiente
        var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
        if (environment == "Development")
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddConsole();
            logging.AddDebug();
        }
        else
        {
            logging.SetMinimumLevel(LogLevel.Warning);
        }

        // Filtros específicos para reducir ruido
        logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
        logging.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);
        logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
    })
    .Build();

// Verificar y configurar la base de datos al inicio solo si estamos en Azure Functions
if (Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME") != null)
{
    await InitializeDatabaseAsync(host.Services);
}

// Mensaje de inicio
Console.WriteLine("🏹 Hunter Fitness Azure Functions API starting...");
Console.WriteLine($"⚔️ Environment: {Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Production"}");
Console.WriteLine("🌟 Ready to serve hunters across all realms!");

host.Run();

// Método para inicializar la base de datos
static async Task InitializeDatabaseAsync(IServiceProvider services)
{
    try
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HunterFitnessDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("🏹 Hunter Fitness API - Testing database connection...");
        
        var canConnect = await dbContext.Database.CanConnectAsync();
        
        if (canConnect)
        {
            logger.LogInformation("✅ Database connection successful!");
            
            // En development, mostrar información adicional
            var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
            if (environment == "Development")
            {
                try
                {
                    var hunterCount = await dbContext.Hunters.CountAsync();
                    var questCount = await dbContext.DailyQuests.CountAsync();
                    var dungeonCount = await dbContext.Dungeons.CountAsync();
                    var achievementCount = await dbContext.Achievements.CountAsync();
                    var equipmentCount = await dbContext.Equipment.CountAsync();
                    
                    logger.LogInformation("📊 Database Stats:");
                    logger.LogInformation("   • Hunters: {HunterCount}", hunterCount);
                    logger.LogInformation("   • Daily Quests: {QuestCount}", questCount);
                    logger.LogInformation("   • Dungeons: {DungeonCount}", dungeonCount);
                    logger.LogInformation("   • Achievements: {AchievementCount}", achievementCount);
                    logger.LogInformation("   • Equipment: {EquipmentCount}", equipmentCount);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("⚠️ Could not retrieve database stats: {Error}", ex.Message);
                }
            }
            
            logger.LogInformation("🎯 Hunter Fitness API ready for action!");
        }
        else
        {
            logger.LogError("💀 Database connection failed!");
            logger.LogError("💡 Check your connection string and ensure the database server is accessible");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💀 Database initialization error: {ex.Message}");
        
        // En development, mostrar más detalles del error
        var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
        if (environment == "Development")
        {
            Console.WriteLine($"📋 Full error details: {ex}");
        }
        
        // No lanzar excepción para permitir que la app inicie (útil para troubleshooting)
        Console.WriteLine("⚠️ Starting API without database connection - some functions may not work");
    }
}