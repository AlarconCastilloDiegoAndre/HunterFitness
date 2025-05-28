using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using HunterFitness.API.Services;
using HunterFitness.API.DTOs;
using HunterFitness.API.Models;

namespace HunterFitness.API.Functions
{
    public class DungeonFunctions
    {
        private readonly IDungeonService _dungeonService;
        private readonly IAuthService _authService;
        private readonly ILogger<DungeonFunctions> _logger;

        public DungeonFunctions(
            IDungeonService dungeonService,
            IAuthService authService,
            ILogger<DungeonFunctions> logger)
        {
            _dungeonService = dungeonService;
            _authService = authService;
            _logger = logger;
        }

        [Function("GetAvailableDungeons")]
        public async Task<HttpResponseData> GetAvailableDungeons(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dungeons")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🏰 Get available dungeons request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var dungeons = await _dungeonService.GetAvailableDungeonsAsync(hunter.HunterID);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<List<DungeonDto>>
                {
                    Success = true,
                    Message = $"Found {dungeons.Count} dungeons ready for conquest! 🏰",
                    Data = dungeons
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting available dungeons");
                return await CreateErrorResponse(req, "Error retrieving available dungeons");
            }
        }

        [Function("GetDungeonDetails")]
        public async Task<HttpResponseData> GetDungeonDetails(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dungeons/{dungeonId}")] 
            HttpRequestData req, string dungeonId)
        {
            _logger.LogInformation("🏰 Get dungeon details request: {DungeonId}", dungeonId);

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                if (!Guid.TryParse(dungeonId, out var dungeonGuid))
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid dungeon ID format."
                    });
                    return badRequestResponse;
                }

                var dungeon = await _dungeonService.GetDungeonDetailsAsync(dungeonGuid, hunter.HunterID);

                if (dungeon == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Dungeon not found."
                    });
                    return notFoundResponse;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<DungeonDto>
                {
                    Success = true,
                    Message = $"Dungeon details loaded: {dungeon.DungeonName} 🏰",
                    Data = dungeon
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting dungeon details: {DungeonId}", dungeonId);
                return await CreateErrorResponse(req, "Error retrieving dungeon details");
            }
        }

        [Function("StartRaid")]
        public async Task<HttpResponseData> StartRaid(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "dungeons/start-raid")] 
            HttpRequestData req)
        {
            _logger.LogInformation("⚔️ Start dungeon raid request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var startRaidDto = JsonSerializer.Deserialize<StartRaidDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (startRaidDto == null)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request data."
                    });
                    return badRequestResponse;
                }

                var result = await _dungeonService.StartRaidAsync(hunter.HunterID, startRaidDto);

                var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                var response = req.CreateResponse(statusCode);
                
                await response.WriteAsJsonAsync(result);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error starting dungeon raid");
                return await CreateErrorResponse(req, "Error starting dungeon raid");
            }
        }

        [Function("UpdateRaidProgress")]
        public async Task<HttpResponseData> UpdateRaidProgress(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "dungeons/raid-progress")] 
            HttpRequestData req)
        {
            _logger.LogInformation("📈 Update raid progress request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var updateDto = JsonSerializer.Deserialize<UpdateRaidProgressDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (updateDto == null)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request data."
                    });
                    return badRequestResponse;
                }

                var result = await _dungeonService.UpdateRaidProgressAsync(hunter.HunterID, updateDto);

                var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                var response = req.CreateResponse(statusCode);
                
                await response.WriteAsJsonAsync(result);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error updating raid progress");
                return await CreateErrorResponse(req, "Error updating raid progress");
            }
        }

        [Function("CompleteRaid")]
        public async Task<HttpResponseData> CompleteRaid(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "dungeons/complete-raid")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🎉 Complete dungeon raid request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var completeDto = JsonSerializer.Deserialize<CompleteRaidDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (completeDto == null)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request data."
                    });
                    return badRequestResponse;
                }

                var result = await _dungeonService.CompleteRaidAsync(hunter.HunterID, completeDto);

                var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                var response = req.CreateResponse(statusCode);
                
                await response.WriteAsJsonAsync(result);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error completing dungeon raid");
                return await CreateErrorResponse(req, "Error completing dungeon raid");
            }
        }

        [Function("GetActiveRaids")]
        public async Task<HttpResponseData> GetActiveRaids(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dungeons/active-raids")] 
            HttpRequestData req)
        {
            _logger.LogInformation("⚔️ Get active raids request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var activeRaids = await _dungeonService.GetActiveRaidsAsync(hunter.HunterID);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<List<DungeonRaidDto>>
                {
                    Success = true,
                    Message = activeRaids.Any() 
                        ? $"You have {activeRaids.Count} active raid(s)! ⚔️"
                        : "No active raids. Ready to start a new adventure? 🏰",
                    Data = activeRaids
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting active raids");
                return await CreateErrorResponse(req, "Error retrieving active raids");
            }
        }

        [Function("GetRaidHistory")]
        public async Task<HttpResponseData> GetRaidHistory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dungeons/raid-history")] 
            HttpRequestData req)
        {
            _logger.LogInformation("📚 Get raid history request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var limitStr = query["limit"] ?? "20";
                var limit = int.TryParse(limitStr, out var parsedLimit) ? Math.Min(parsedLimit, 100) : 20;

                var raidHistory = await _dungeonService.GetRaidHistoryAsync(hunter.HunterID, limit);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<List<DungeonRaidDto>>
                {
                    Success = true,
                    Message = $"Retrieved {raidHistory.Count} raid records! 📚",
                    Data = raidHistory,
                    Metadata = new Dictionary<string, object>
                    {
                        {"TotalRecords", raidHistory.Count},
                        {"Limit", limit}
                    }
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting raid history");
                return await CreateErrorResponse(req, "Error retrieving raid history");
            }
        }

        [Function("AbandonRaid")]
        public async Task<HttpResponseData> AbandonRaid(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "dungeons/abandon-raid/{raidId}")] 
            HttpRequestData req, string raidId)
        {
            _logger.LogInformation("🏃‍♂️ Abandon raid request: {RaidId}", raidId);

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                if (!Guid.TryParse(raidId, out var raidGuid))
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid raid ID format."
                    });
                    return badRequestResponse;
                }

                var result = await _dungeonService.AbandonRaidAsync(hunter.HunterID, raidGuid);

                var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                var response = req.CreateResponse(statusCode);
                
                await response.WriteAsJsonAsync(result);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error abandoning raid: {RaidId}", raidId);
                return await CreateErrorResponse(req, "Error abandoning raid");
            }
        }

        // Helper methods
        private async Task<Hunter?> GetHunterFromToken(HttpRequestData req)
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