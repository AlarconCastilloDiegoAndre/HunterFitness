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
    public class QuestFunctions
    {
        private readonly IQuestService _questService;
        private readonly IAuthService _authService;
        private readonly ILogger<QuestFunctions> _logger;

        public QuestFunctions(
            IQuestService questService,
            IAuthService authService,
            ILogger<QuestFunctions> logger)
        {
            _questService = questService;
            _authService = authService;
            _logger = logger;
        }

        [Function("GetDailyQuests")]
        public async Task<HttpResponseData> GetDailyQuests(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "quests/daily")] 
            HttpRequestData req)
        {
            _logger.LogInformation("📋 Get daily quests request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                // Obtener fecha de query parameter (opcional)
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var questDateStr = query["date"];
                DateTime? questDate = null;

                if (!string.IsNullOrEmpty(questDateStr) && DateTime.TryParse(questDateStr, out var parsedDate))
                {
                    questDate = parsedDate.Date;
                }

                var dailyQuests = await _questService.GetDailyQuestsAsync(hunter.HunterID, questDate);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<DailyQuestsSummaryDto>
                {
                    Success = true,
                    Message = dailyQuests.Quests.Any() 
                        ? $"Today's challenges await, {hunter.HunterName}! 🏹"
                        : "Ready to generate new quests? 📋",
                    Data = dailyQuests
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting daily quests");
                return await CreateErrorResponse(req, "Error retrieving daily quests");
            }
        }

        [Function("StartQuest")]
        public async Task<HttpResponseData> StartQuest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "quests/start")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🎯 Start quest request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var startDto = JsonSerializer.Deserialize<StartQuestDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (startDto == null)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request data."
                    });
                    return badRequestResponse;
                }

                var result = await _questService.StartQuestAsync(hunter.HunterID, startDto);

                var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                var response = req.CreateResponse(statusCode);
                
                await response.WriteAsJsonAsync(new ApiResponseDto<QuestOperationResponseDto>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error starting quest");
                return await CreateErrorResponse(req, "Error starting quest");
            }
        }

        [Function("UpdateQuestProgress")]
        public async Task<HttpResponseData> UpdateQuestProgress(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "quests/progress")] 
            HttpRequestData req)
        {
            _logger.LogInformation("📈 Update quest progress request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var updateDto = JsonSerializer.Deserialize<UpdateQuestProgressDto>(requestBody, new JsonSerializerOptions
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

                var result = await _questService.UpdateQuestProgressAsync(hunter.HunterID, updateDto);

                var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                var response = req.CreateResponse(statusCode);
                
                await response.WriteAsJsonAsync(new ApiResponseDto<QuestOperationResponseDto>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error updating quest progress");
                return await CreateErrorResponse(req, "Error updating quest progress");
            }
        }

        [Function("CompleteQuest")]
        public async Task<HttpResponseData> CompleteQuest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "quests/complete")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🎉 Complete quest request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var completeDto = JsonSerializer.Deserialize<CompleteQuestDto>(requestBody, new JsonSerializerOptions
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

                var result = await _questService.CompleteQuestAsync(hunter.HunterID, completeDto);

                var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                var response = req.CreateResponse(statusCode);
                
                await response.WriteAsJsonAsync(new ApiResponseDto<QuestOperationResponseDto>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error completing quest");
                return await CreateErrorResponse(req, "Error completing quest");
            }
        }

        [Function("GetQuestHistory")]
        public async Task<HttpResponseData> GetQuestHistory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "quests/history")] 
            HttpRequestData req)
        {
            _logger.LogInformation("📚 Get quest history request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var limitStr = query["limit"] ?? "50";
                var limit = int.TryParse(limitStr, out var parsedLimit) ? Math.Min(parsedLimit, 200) : 50;

                var history = await _questService.GetQuestHistoryAsync(hunter.HunterID, limit);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<List<QuestHistoryDto>>
                {
                    Success = true,
                    Message = $"Retrieved {history.Count} quest records! 📚",
                    Data = history,
                    Metadata = new Dictionary<string, object>
                    {
                        {"TotalRecords", history.Count},
                        {"Limit", limit}
                    }
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting quest history");
                return await CreateErrorResponse(req, "Error retrieving quest history");
            }
        }

        [Function("GetQuestStats")]
        public async Task<HttpResponseData> GetQuestStats(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "quests/stats")] 
            HttpRequestData req)
        {
            _logger.LogInformation("📊 Get quest stats request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var stats = await _questService.GetQuestStatsAsync(hunter.HunterID);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<QuestStatsDto>
                {
                    Success = true,
                    Message = "Quest statistics retrieved! 📊",
                    Data = stats
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting quest stats");
                return await CreateErrorResponse(req, "Error retrieving quest statistics");
            }
        }

        [Function("GetAvailableQuests")]
        public async Task<HttpResponseData> GetAvailableQuests(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "quests/available")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🎯 Get available quests request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var availableQuests = await _questService.GetAvailableQuestsAsync(hunter.HunterID);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<List<AvailableQuestDto>>
                {
                    Success = true,
                    Message = $"Found {availableQuests.Count} available quests! 🎯",
                    Data = availableQuests
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting available quests");
                return await CreateErrorResponse(req, "Error retrieving available quests");
            }
        }

        [Function("GenerateDailyQuests")]
        public async Task<HttpResponseData> GenerateDailyQuests(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "quests/generate")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🎲 Generate daily quests request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var generateDto = JsonSerializer.Deserialize<GenerateDailyQuestsDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Usar valores por defecto si no se proporcionan
                var questDate = generateDto?.QuestDate.Date ?? DateTime.UtcNow.Date;
                var questCount = generateDto?.NumberOfQuests ?? 3;

                var success = await _questService.GenerateDailyQuestsAsync(hunter.HunterID, questDate, questCount);

                if (success)
                {
                    // Obtener los quests generados
                    var dailyQuests = await _questService.GetDailyQuestsAsync(hunter.HunterID, questDate);

                    var response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(new ApiResponseDto<DailyQuestsSummaryDto>
                    {
                        Success = true,
                        Message = $"🎲 Generated {questCount} new quests for {questDate:MMM dd}! Ready to train?",
                        Data = dailyQuests
                    });

                    return response;
                }
                else
                {
                    var failResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await failResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Unable to generate quests. They may already exist for this date."
                    });
                    return failResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error generating daily quests");
                return await CreateErrorResponse(req, "Error generating daily quests");
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