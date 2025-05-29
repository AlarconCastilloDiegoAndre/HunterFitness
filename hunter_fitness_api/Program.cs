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
        
        // Entity Framework - Usando la cadena de conexión que proporcionaste
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
            
            // Solo en desarrollo
            var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
            if (environment == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });
        
        // CORS para desarrollo
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });
        
        // Servicios personalizados - ORDEN CORRECTO para evitar dependencias circulares
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IHunterService, HunterService>();
        services.AddScoped<IAchievementService, AchievementService>();
        services.AddScoped<IQuestService, QuestService>();
        
        // DungeonService y EquipmentService requieren IHunterService, por eso van después
        services.AddScoped<IDungeonService, DungeonService>();
        services.AddScoped<IEquipmentService, EquipmentService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.AddApplicationInsights();
        
        // Solo en desarrollo, mostrar logs detallados
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

// Verificar conexión a base de datos al iniciar
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
        
        // Intentar aplicar migraciones pendientes en desarrollo
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

// Iniciar la aplicación
host.Run();