using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using HunterFitness.API.Services;
using HunterFitness.API.DTOs;

namespace HunterFitness.API.Functions
{
    public class AchievementFunctions
    {
        private readonly IAchievementService _achievementService;
        private readonly IAuthService _authService;
        private readonly ILogger<AchievementFunctions> _logger;

        public AchievementFunctions(
            IAchievementService achievementService,
            IAuthService authService,
            ILogger<AchievementFunctions> logger)
        {
            _achievementService = achievementService;
            _authService = authService;
            _logger = logger;
        }

        [Function("GetHunterAchievements")]
        public async Task<HttpResponseData> GetHunterAchievements(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "achievements")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🏆 Get hunter achievements request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var achievements = await _achievementService.GetHunterAchievementsAsync(hunter.HunterID);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<HunterAchievementsDto>
                {
                    Success = true,
                    Message = $"Achievement progress: {achievements.UnlockedCount}/{achievements.TotalAchievements} ({achievements.CompletionPercentage:F1}%) 🏆",
                    Data = achievements
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting hunter achievements");
                return await CreateErrorResponse(req, "Error retrieving achievements");
            }
        }

        [Function("GetAvailableAchievements")]
        public async Task<HttpResponseData> GetAvailableAchievements(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "achievements/available")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🎯 Get available achievements request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var achievements = await _achievementService.GetAvailableAchievementsAsync(hunter.HunterID);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<List<AchievementDto>>
                {
                    Success = true,
                    Message = $"Found {achievements.Count} achievements to unlock! 🎯",
                    Data = achievements
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting available achievements");
                return await CreateErrorResponse(req, "Error retrieving available achievements");
            }
        }

        [Function("GetAchievementsByCategory")]
        public async Task<HttpResponseData> GetAchievementsByCategory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "achievements/category/{category}")] 
            HttpRequestData req, string category)
        {
            _logger.LogInformation("🏆 Get achievements by category request: {Category}", category);

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var validCategories = new[] { "Consistency", "Strength", "Endurance", "Social", "Special", "Milestone" };
                if (!validCategories.Contains(category, StringComparer.OrdinalIgnoreCase))
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = $"Invalid category. Valid categories: {string.Join(", ", validCategories)}"
                    });
                    return badRequestResponse;
                }

                var achievements = await _achievementService.GetAchievementsByCategoryAsync(category, hunter.HunterID);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<List<AchievementDto>>
                {
                    Success = true,
                    Message = $"{category} achievements: {achievements.Count(a => a.IsUnlocked)}/{achievements.Count} unlocked! 🏆",
                    Data = achievements
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting achievements by category: {Category}", category);
                return await CreateErrorResponse(req, "Error retrieving achievements by category");
            }
        }

        [Function("GetAchievementStats")]
        public async Task<HttpResponseData> GetAchievementStats(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "achievements/stats")] 
            HttpRequestData req)
        {
            _logger.LogInformation("📊 Get achievement stats request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var stats = await _achievementService.GetAchievementStatsAsync(hunter.HunterID);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<Dictionary<string, object>>
                {
                    Success = true,
                    Message = "Achievement statistics retrieved! 📊",
                    Data = stats
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting achievement stats");
                return await CreateErrorResponse(req, "Error retrieving achievement statistics");
            }
        }

        [Function("UnlockAchievement")]
        public async Task<HttpResponseData> UnlockAchievement(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "achievements/unlock")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🎁 Unlock achievement request (internal)");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var unlockRequest = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (unlockRequest == null || 
                    !unlockRequest.ContainsKey("hunterId") || 
                    !unlockRequest.ContainsKey("achievementId"))
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request. Required: hunterId, achievementId"
                    });
                    return badRequestResponse;
                }

                var hunterId = Guid.Parse(unlockRequest["hunterId"].ToString()!);
                var achievementId = Guid.Parse(unlockRequest["achievementId"].ToString()!);

                var success = await _achievementService.UnlockAchievementAsync(hunterId, achievementId);

                var statusCode = success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                var response = req.CreateResponse(statusCode);
                
                await response.WriteAsJsonAsync(new ApiResponseDto<object>
                {
                    Success = success,
                    Message = success ? "🏆 Achievement unlocked!" : "Failed to unlock achievement",
                    Data = new { HunterID = hunterId, AchievementID = achievementId }
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error unlocking achievement");
                return await CreateErrorResponse(req, "Error unlocking achievement");
            }
        }

        [Function("CheckAchievements")]
        public async Task<HttpResponseData> CheckAchievements(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "achievements/check")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🔍 Check achievements request (internal)");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var checkRequest = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (checkRequest == null || 
                    !checkRequest.ContainsKey("hunterId") || 
                    !checkRequest.ContainsKey("eventType"))
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request. Required: hunterId, eventType"
                    });
                    return badRequestResponse;
                }

                var hunterId = Guid.Parse(checkRequest["hunterId"].ToString()!);
                var eventType = checkRequest["eventType"].ToString()!;
                var incrementValue = checkRequest.ContainsKey("incrementValue") 
                    ? int.Parse(checkRequest["incrementValue"].ToString()!) 
                    : 1;

                var newAchievements = await _achievementService.CheckAndUpdateAchievementsAsync(
                    hunterId, eventType, incrementValue);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<List<AchievementDto>>
                {
                    Success = true,
                    Message = newAchievements.Any() 
                        ? $"🎉 {newAchievements.Count} new achievement(s) unlocked!"
                        : "No new achievements unlocked.",
                    Data = newAchievements
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error checking achievements");
                return await CreateErrorResponse(req, "Error checking achievements");
            }
        }

        // Helper methods
        private async Task<Models.Hunter?> GetHunterFromToken(HttpRequestData req)
        {
            try
            {
                var authHeader = req.Headers.FirstOrDefault(h => h.Key.ToLower() == "authorization");
                var token = authHeader.Value?.FirstOrDefault()?.Replace("Bearer ", "");

                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                return await _authService.GetHunterFromTokenAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error extracting hunter from token");
                return null;
            }
        }

        private async Task<HttpResponseData> CreateUnauthorizedResponse(HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.Unauthorized);
            await response.WriteAsJsonAsync(new ApiResponseDto<object>
            {
                Success = false,
                Message = "Authentication required.",
                Errors = new List<string> { "Please include a valid 'Authorization: Bearer <token>' header" }
            });
            return response;
        }

        private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new ApiResponseDto<object>
            {
                Success = false,
                Message = message,
                Errors = new List<string> { "Please try again later" }
            });
            return response;
        }
    }
}