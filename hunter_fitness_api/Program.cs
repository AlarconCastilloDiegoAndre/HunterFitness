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
        
        // Entity Framework
        var connectionString = Environment.GetEnvironmentVariable("HunterFitnessDB");
        if (!string.IsNullOrEmpty(connectionString))
        {
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
                if (Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development")
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });
        }
        
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
        
        // Servicios personalizados
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IHunterService, HunterService>();
        // services.AddScoped<IQuestService, QuestService>(); // Lo agregaremos después
    })
    .ConfigureLogging(logging =>
    {
        logging.AddApplicationInsights();
        
        // Solo en desarrollo, mostrar logs detallados
        if (Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development")
        {
            logging.SetMinimumLevel(LogLevel.Information);
        }
    })
    .Build();

// Verificar conexión a base de datos al iniciar
using (var scope = host.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<HunterFitnessDbContext>();
        var canConnect = await dbContext.Database.CanConnectAsync();
        
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        if (canConnect)
        {
            logger.LogInformation("🏹 Hunter Fitness API - Database connection successful!");
            logger.LogInformation("⚔️ Ready to serve hunters across all realms!");
        }
        else
        {
            logger.LogError("❌ Database connection failed!");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "💀 Error during database connection check");
    }
}

host.Run();