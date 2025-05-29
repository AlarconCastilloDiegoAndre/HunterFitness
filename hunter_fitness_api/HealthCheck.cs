using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;
using HunterFitness.API.Data;

namespace HunterFitness.API
{
    public class HealthCheck
    {
        private readonly ILogger<HealthCheck> _logger;
        private readonly HunterFitnessDbContext _context;

        public HealthCheck(ILoggerFactory loggerFactory, HunterFitnessDbContext context)
        {
            _logger = loggerFactory.CreateLogger<HealthCheck>();
            _context = context;
        }

        [Function("HealthCheck")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🏹 Hunter Fitness API - Health check requested at {Time}", DateTime.UtcNow);

            var healthData = new HealthCheckResponse
            {
                Success = true,
                Message = "Hunter Fitness API is running like a Shadow Monarch! 🏹⚔️",
                Service = "Hunter Fitness API",
                Version = GetApiVersion(),
                Timestamp = DateTime.UtcNow,
                Environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Production",
                Status = "Healthy",
                Uptime = GetUptime(),
                Checks = new Dictionary<string, object>()
            };

            // Database Health Check
            var dbHealth = await CheckDatabaseHealthAsync();
            healthData.Checks["Database"] = dbHealth;
            
            // Memory Check
            var memoryHealth = CheckMemoryHealth();
            healthData.Checks["Memory"] = memoryHealth;

            // Services Check
            var servicesHealth = CheckServicesHealth();
            healthData.Checks["Services"] = servicesHealth;

            // System Info
            healthData.SystemInfo = GetSystemInfo();

            // API Endpoints
            healthData.Endpoints = GetApiEndpoints();

            // Determine overall health status
            var allChecksHealthy = healthData.Checks.Values
                .All(check => check is Dictionary<string, object> checkDict && 
                     checkDict.ContainsKey("Status") && 
                     checkDict["Status"].ToString() == "Healthy");

            if (!allChecksHealthy)
            {
                healthData.Success = false;
                healthData.Status = "Degraded";
                healthData.Message = "Some health checks are failing. Check individual components.";
            }

            var statusCode = healthData.Success ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(healthData, jsonOptions));
            
            _logger.LogInformation("🩺 Health check completed - Status: {Status}", healthData.Status);
            
            return response;
        }

        [Function("HealthCheckSimple")]
        public async Task<HttpResponseData> SimpleHealthCheck(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🏓 Simple ping health check requested");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var pingResponse = new
            {
                status = "ok",
                message = "pong 🏹",
                timestamp = DateTime.UtcNow,
                service = "Hunter Fitness API"
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(pingResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));

            return response;
        }

        private async Task<Dictionary<string, object>> CheckDatabaseHealthAsync()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var canConnect = await _context.Database.CanConnectAsync();
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                if (canConnect)
                {
                    // Intentar obtener estadísticas básicas
                    try
                    {
                        var hunterCount = await _context.Hunters.CountAsync();
                        var questCount = await _context.DailyQuests.CountAsync();
                        
                        return new Dictionary<string, object>
                        {
                            {"Status", "Healthy"},
                            {"ResponseTimeMs", Math.Round(responseTime, 2)},
                            {"CanConnect", true},
                            {"Statistics", new {
                                Hunters = hunterCount,
                                Quests = questCount,
                                LastChecked = DateTime.UtcNow
                            }}
                        };
                    }
                    catch (Exception ex)
                    {
                        return new Dictionary<string, object>
                        {
                            {"Status", "Degraded"},
                            {"ResponseTimeMs", Math.Round(responseTime, 2)},
                            {"CanConnect", true},
                            {"Warning", "Connected but cannot query data"},
                            {"Error", ex.Message}
                        };
                    }
                }
                else
                {
                    return new Dictionary<string, object>
                    {
                        {"Status", "Unhealthy"},
                        {"ResponseTimeMs", Math.Round(responseTime, 2)},
                        {"CanConnect", false},
                        {"Error", "Cannot connect to database"}
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Database health check failed");
                return new Dictionary<string, object>
                {
                    {"Status", "Unhealthy"},
                    {"CanConnect", false},
                    {"Error", ex.Message},
                    {"ExceptionType", ex.GetType().Name}
                };
            }
        }

        private Dictionary<string, object> CheckMemoryHealth()
        {
            try
            {
                var totalMemory = GC.GetTotalMemory(false);
                var gen0Collections = GC.CollectionCount(0);
                var gen1Collections = GC.CollectionCount(1);
                var gen2Collections = GC.CollectionCount(2);

                // Convertir a MB para mejor legibilidad
                var totalMemoryMB = Math.Round(totalMemory / 1024.0 / 1024.0, 2);

                var status = totalMemoryMB < 500 ? "Healthy" : totalMemoryMB < 1000 ? "Warning" : "Critical";

                return new Dictionary<string, object>
                {
                    {"Status", status},
                    {"TotalMemoryMB", totalMemoryMB},
                    {"GarbageCollector", new {
                        Gen0Collections = gen0Collections,
                        Gen1Collections = gen1Collections,
                        Gen2Collections = gen2Collections
                    }}
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Memory health check failed");
                return new Dictionary<string, object>
                {
                    {"Status", "Unknown"},
                    {"Error", ex.Message}
                };
            }
        }

        private Dictionary<string, object> CheckServicesHealth()
        {
            return new Dictionary<string, object>
            {
                {"Status", "Healthy"},
                {"RegisteredServices", new[] {
                    "AuthService",
                    "HunterService", 
                    "QuestService",
                    "DungeonService",
                    "EquipmentService",
                    "AchievementService"
                }},
                {"DatabaseContext", "HunterFitnessDbContext"}
            };
        }

        private Dictionary<string, object> GetSystemInfo()
        {
            return new Dictionary<string, object>
            {
                {"MachineName", Environment.MachineName},
                {"ProcessorCount", Environment.ProcessorCount},
                {"OSVersion", Environment.OSVersion.ToString()},
                {"DotNetVersion", Environment.Version.ToString()},
                {"WorkingSet", Math.Round(Environment.WorkingSet / 1024.0 / 1024.0, 2) + " MB"},
                {"ThreadCount", System.Diagnostics.Process.GetCurrentProcess().Threads.Count}
            };
        }

        private Dictionary<string, object> GetApiEndpoints()
        {
            return new Dictionary<string, object>
            {
                {"Authentication", new[] {
                    "POST /api/auth/register",
                    "POST /api/auth/login", 
                    "POST /api/auth/refresh",
                    "GET /api/auth/validate"
                }},
                {"Hunters", new[] {
                    "GET /api/hunters/profile",
                    "PUT /api/hunters/profile",
                    "GET /api/hunters/stats",
                    "GET /api/hunters/progress",
                    "GET /api/hunters/leaderboard"
                }},
                {"Quests", new[] {
                    "GET /api/quests/daily",
                    "POST /api/quests/start",
                    "PUT /api/quests/progress", 
                    "POST /api/quests/complete",
                    "GET /api/quests/history"
                }},
                {"Dungeons", new[] {
                    "GET /api/dungeons",
                    "GET /api/dungeons/{id}",
                    "POST /api/dungeons/start-raid",
                    "PUT /api/dungeons/raid-progress",
                    "POST /api/dungeons/complete-raid"
                }},
                {"Equipment", new[] {
                    "GET /api/equipment/inventory",
                    "GET /api/equipment/available",
                    "POST /api/equipment/equip",
                    "POST /api/equipment/unequip"
                }},
                {"Achievements", new[] {
                    "GET /api/achievements",
                    "GET /api/achievements/available",
                    "GET /api/achievements/category/{category}"
                }},
                {"Health", new[] {
                    "GET /api/health",
                    "GET /api/ping"
                }}
            };
        }

        private string GetApiVersion()
        {
            return Environment.GetEnvironmentVariable("API_VERSION") ?? "1.0.0";
        }

        private string GetUptime()
        {
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var uptime = DateTime.Now - process.StartTime;
                
                if (uptime.TotalDays >= 1)
                    return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
                else if (uptime.TotalHours >= 1)
                    return $"{uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
                else
                    return $"{uptime.Minutes}m {uptime.Seconds}s";
            }
            catch
            {
                return "Unknown";
            }
        }

        // Modelo para la respuesta de health check
        private class HealthCheckResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string Service { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public string Environment { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Uptime { get; set; } = string.Empty;
            public Dictionary<string, object> Checks { get; set; } = new();
            public Dictionary<string, object> SystemInfo { get; set; } = new();
            public Dictionary<string, object> Endpoints { get; set; } = new();
        }
    }
}