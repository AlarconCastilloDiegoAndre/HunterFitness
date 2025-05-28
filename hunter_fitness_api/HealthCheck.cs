using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace HunterFitness.API
{
    public class HealthCheck
    {
        private readonly ILogger _logger;

        public HealthCheck(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HealthCheck>();
        }

        [Function("HealthCheck")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🏹 Hunter Fitness API - Health check requested");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var healthData = new
            {
                success = true,
                message = "Hunter Fitness API is running like a Shadow Monarch! 🏹⚔️",
                service = "Hunter Fitness API",
                version = "1.0.0",
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Development",
                status = "Ready for Hunter adventures!",
                endpoints = new
                {
                    health = "/api/health",
                    auth = "/api/auth/*",
                    hunters = "/api/hunters/*",
                    quests = "/api/quests/*",
                    dungeons = "/api/dungeons/*"
                }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(healthData, jsonOptions));
            return response;
        }
    }
}