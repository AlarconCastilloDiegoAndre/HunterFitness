// hunter_fitness_api/Functions/QuestFunctions.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using HunterFitness.API.Services; // Para IAuthService, IQuestService
using HunterFitness.API.DTOs;    // Para ApiResponseDto, CompleteQuestDto, QuestOperationResponseDto, etc.
using HunterFitness.API.Models;  // Para Hunter (si GetHunterFromToken devuelve el modelo)
using System.IO;                 // Para StreamReader
using System.Threading.Tasks;    // Para Task
using System.Linq;               // Para FirstOrDefault en Headers
// using System;                 // Necesario para Guid, DateTime
// using System.Collections.Generic; // Necesario para List, Dictionary
// using System.Web; // No es necesario para ParseQueryString en .NET Core/5+ para req.Url.Query

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
            _logger.LogInformation("📋 GetDailyQuests function triggered.");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    _logger.LogWarning("GetDailyQuests: Unauthorized access attempt or invalid token.");
                    return await CreateUnauthorizedResponse(req);
                }

                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var questDateStr = query["date"];
                DateTime? questDate = null;

                if (!string.IsNullOrEmpty(questDateStr) && DateTime.TryParse(questDateStr, out var parsedDate))
                {
                    questDate = parsedDate.Date;
                }
                _logger.LogInformation("GetDailyQuests: Fetching quests for HunterID {HunterID}, Date: {QuestDate}", hunter.HunterID, questDate?.ToString("o") ?? "Today");

                var dailyQuestsSummary = await _questService.GetDailyQuestsAsync(hunter.HunterID, questDate);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<DailyQuestsSummaryDto>
                {
                    Success = true,
                    Message = dailyQuestsSummary.Quests.Any()
                        ? $"Today's challenges await, {hunter.HunterName}! 🏹"
                        : (questDate.HasValue && questDate.Value.Date != DateTime.UtcNow.Date ? "No quests found for the specified date." : "Ready to generate new quests? 📋"),
                    Data = dailyQuestsSummary
                });
                _logger.LogInformation("GetDailyQuests: Successfully retrieved quests for HunterID {HunterID}.", hunter.HunterID);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error in GetDailyQuests function.");
                return await CreateErrorResponse(req, "Error retrieving daily quests.");
            }
        }

        [Function("StartQuest")]
        public async Task<HttpResponseData> StartQuest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "quests/start")]
            HttpRequestData req)
        {
            _logger.LogInformation("🎯 StartQuest function triggered.");
            string requestBodyForLogging = "Not read yet";

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    _logger.LogWarning("StartQuest: Unauthorized access attempt or invalid token.");
                    return await CreateUnauthorizedResponse(req);
                }

                requestBodyForLogging = await new StreamReader(req.Body).ReadToEndAsync();
                 _logger.LogInformation("StartQuest: Received request body: {RequestBody}", requestBodyForLogging);


                if (string.IsNullOrWhiteSpace(requestBodyForLogging))
                {
                    _logger.LogWarning("StartQuest: Request body is null or empty.");
                    var badReqResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badReqResponse.WriteAsJsonAsync(new ApiResponseDto<object> { Success = false, Message = "Request body cannot be empty." });
                    return badReqResponse;
                }

                StartQuestDto? startDto = null;
                try
                {
                    startDto = JsonSerializer.Deserialize<StartQuestDto>(requestBodyForLogging, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "StartQuest: JSON Deserialization error. RequestBody: {RequestBody}", requestBodyForLogging);
                    var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await errorResponse.WriteAsJsonAsync(new ApiResponseDto<object> { Success = false, Message = "Invalid JSON format in request body.", Errors = new List<string> { jsonEx.Message } });
                    return errorResponse;
                }


                if (startDto == null || startDto.AssignmentID == Guid.Empty)
                {
                    _logger.LogWarning("StartQuest: Invalid request data. DTO is null or AssignmentID is empty. RequestBody: {RequestBody}", requestBodyForLogging);
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request data. AssignmentID is required."
                    });
                    return badRequestResponse;
                }
                 _logger.LogInformation("StartQuest: Calling _questService.StartQuestAsync for HunterID {HunterID}, AssignmentID {AssignmentID}", hunter.HunterID, startDto.AssignmentID);
                var result = await _questService.StartQuestAsync(hunter.HunterID, startDto);

                var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                var response = req.CreateResponse(statusCode);

                await response.WriteAsJsonAsync(new ApiResponseDto<QuestOperationResponseDto>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result
                });
                 _logger.LogInformation("StartQuest: Responding with StatusCode {StatusCode} for AssignmentID {AssignmentID}. Success: {OpSuccess}", statusCode, startDto.AssignmentID, result.Success);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error in StartQuest function. RequestBody: {RequestBody}", requestBodyForLogging);
                return await CreateErrorResponse(req, "An unexpected error occurred while starting the quest.");
            }
        }

        [Function("UpdateQuestProgress")]
        public async Task<HttpResponseData> UpdateQuestProgress(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "quests/progress")]
            HttpRequestData req)
        {
            _logger.LogInformation("📈 UpdateQuestProgress function triggered.");
            string requestBodyForLogging = "Not read yet";
            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                     _logger.LogWarning("UpdateQuestProgress: Unauthorized access attempt or invalid token.");
                    return await CreateUnauthorizedResponse(req);
                }

                requestBodyForLogging = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation("UpdateQuestProgress: Received request body: {RequestBody}", requestBodyForLogging);

                if (string.IsNullOrWhiteSpace(requestBodyForLogging))
                {
                    _logger.LogWarning("UpdateQuestProgress: Request body is null or empty.");
                    var badReqResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badReqResponse.WriteAsJsonAsync(new ApiResponseDto<object> { Success = false, Message = "Request body cannot be empty." });
                    return badReqResponse;
                }
                
                UpdateQuestProgressDto? updateDto = null;
                try
                {
                    updateDto = JsonSerializer.Deserialize<UpdateQuestProgressDto>(requestBodyForLogging, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "UpdateQuestProgress: JSON Deserialization error. RequestBody: {RequestBody}", requestBodyForLogging);
                    var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await errorResponse.WriteAsJsonAsync(new ApiResponseDto<object> { Success = false, Message = "Invalid JSON format in request body.", Errors = new List<string> { jsonEx.Message } });
                    return errorResponse;
                }


                if (updateDto == null || updateDto.AssignmentID == Guid.Empty)
                {
                    _logger.LogWarning("UpdateQuestProgress: Invalid request data. DTO is null or AssignmentID is empty. RequestBody: {RequestBody}", requestBodyForLogging);
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request data. AssignmentID is required."
                    });
                    return badRequestResponse;
                }
                _logger.LogInformation("UpdateQuestProgress: Calling _questService.UpdateQuestProgressAsync for HunterID {HunterID}, AssignmentID {AssignmentID}", hunter.HunterID, updateDto.AssignmentID);
                var result = await _questService.UpdateQuestProgressAsync(hunter.HunterID, updateDto);

                var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                var response = req.CreateResponse(statusCode);

                await response.WriteAsJsonAsync(new ApiResponseDto<QuestOperationResponseDto>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result
                });
                _logger.LogInformation("UpdateQuestProgress: Responding with StatusCode {StatusCode} for AssignmentID {AssignmentID}. Success: {OpSuccess}", statusCode, updateDto.AssignmentID, result.Success);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error in UpdateQuestProgress function. RequestBody: {RequestBody}", requestBodyForLogging);
                return await CreateErrorResponse(req, "An unexpected error occurred while updating quest progress.");
            }
        }

        [Function("CompleteQuest")]
        public async Task<HttpResponseData> CompleteQuest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "quests/complete")]
            HttpRequestData req)
        {
            _logger.LogInformation("🎉 CompleteQuest function triggered.");
            string requestBodyForLogging = "Not read yet";

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    _logger.LogWarning("CompleteQuest: Unauthorized access attempt or invalid token.");
                    return await CreateUnauthorizedResponse(req);
                }

                requestBodyForLogging = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation("CompleteQuest: Received request body: {RequestBody}", requestBodyForLogging);

                if (string.IsNullOrWhiteSpace(requestBodyForLogging))
                {
                    _logger.LogWarning("CompleteQuest: Request body is null or empty.");
                    var badReqResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badReqResponse.WriteAsJsonAsync(new ApiResponseDto<object> { Success = false, Message = "Request body cannot be empty." });
                    return badReqResponse;
                }

                CompleteQuestDto? completeDto = null;
                try
                {
                    completeDto = JsonSerializer.Deserialize<CompleteQuestDto>(requestBodyForLogging, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "CompleteQuest: JSON Deserialization error. RequestBody: {RequestBody}", requestBodyForLogging);
                    var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await errorResponse.WriteAsJsonAsync(new ApiResponseDto<object> { Success = false, Message = "Invalid JSON format in request body.", Errors = new List<string> { jsonEx.Message } });
                    return errorResponse;
                }

                if (completeDto == null)
                {
                    _logger.LogWarning("CompleteQuest: completeDto is null after deserialization. RequestBody: {RequestBody}", requestBodyForLogging);
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object> { Success = false, Message = "Invalid request data. Could not parse DTO." });
                    return badRequestResponse;
                }

                if (completeDto.AssignmentID == Guid.Empty)
                {
                    _logger.LogWarning("CompleteQuest: completeDto.AssignmentID is Guid.Empty. RequestBody: {RequestBody}", requestBodyForLogging);
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object> { Success = false, Message = "Invalid request data. AssignmentID is required and cannot be empty." });
                    return badRequestResponse;
                }

                _logger.LogInformation("CompleteQuest: Calling _questService.CompleteQuestAsync for HunterID {HunterID}, AssignmentID {AssignmentID}", hunter.HunterID, completeDto.AssignmentID);
                var result = await _questService.CompleteQuestAsync(hunter.HunterID, completeDto);

                var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                if (!result.Success && statusCode == HttpStatusCode.OK) { 
                    statusCode = HttpStatusCode.InternalServerError; 
                    _logger.LogWarning("CompleteQuest: Service reported failure but status was OK. Overriding to {StatusCode}. Message: {ResultMessage}", statusCode, result.Message);
                }


                var response = req.CreateResponse(statusCode);
                await response.WriteAsJsonAsync(new ApiResponseDto<QuestOperationResponseDto>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result
                });
                _logger.LogInformation("CompleteQuest: Responding with StatusCode {StatusCode} for AssignmentID {AssignmentID}. Success: {OpSuccess}, Message: {ServiceMessage}", statusCode, completeDto.AssignmentID, result.Success, result.Message);
                return response;
            }
            catch (ArgumentException argEx)
            {
                _logger.LogError(argEx, "CompleteQuest: Argument error processing request. RequestBody: {RequestBody}", requestBodyForLogging);
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new ApiResponseDto<object> { Success = false, Message = argEx.Message });
                return errorResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 CompleteQuest: Unhandled critical error. RequestBody: {RequestBody}", requestBodyForLogging);
                return await CreateErrorResponse(req, "An unexpected internal server error occurred while completing the quest.");
            }
        }

        [Function("GetQuestHistory")]
        public async Task<HttpResponseData> GetQuestHistory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "quests/history")]
            HttpRequestData req)
        {
            _logger.LogInformation("📚 GetQuestHistory function triggered.");
            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    _logger.LogWarning("GetQuestHistory: Unauthorized access attempt or invalid token.");
                    return await CreateUnauthorizedResponse(req);
                }

                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var limitStr = query["limit"] ?? "50";
                var limit = int.TryParse(limitStr, out var parsedLimit) ? Math.Min(parsedLimit, 200) : 50;
                _logger.LogInformation("GetQuestHistory: Fetching history for HunterID {HunterID} with limit {Limit}", hunter.HunterID, limit);

                var history = await _questService.GetQuestHistoryAsync(hunter.HunterID, limit);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<List<QuestHistoryDto>>
                {
                    Success = true,
                    Message = $"Retrieved {history.Count} quest records! 📚",
                    Data = history,
                    Metadata = new Dictionary<string, object> { { "TotalRecords", history.Count }, { "Limit", limit } }
                });
                 _logger.LogInformation("GetQuestHistory: Successfully retrieved {Count} records for HunterID {HunterID}.", history.Count, hunter.HunterID);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error in GetQuestHistory function.");
                return await CreateErrorResponse(req, "Error retrieving quest history.");
            }
        }

        [Function("GetQuestStats")]
        public async Task<HttpResponseData> GetQuestStats(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "quests/stats")]
            HttpRequestData req)
        {
            _logger.LogInformation("📊 GetQuestStats function triggered.");
            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    _logger.LogWarning("GetQuestStats: Unauthorized access attempt or invalid token.");
                    return await CreateUnauthorizedResponse(req);
                }
                _logger.LogInformation("GetQuestStats: Fetching stats for HunterID {HunterID}", hunter.HunterID);
                var stats = await _questService.GetQuestStatsAsync(hunter.HunterID);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<QuestStatsDto>
                {
                    Success = true,
                    Message = "Quest statistics retrieved! 📊",
                    Data = stats
                });
                 _logger.LogInformation("GetQuestStats: Successfully retrieved stats for HunterID {HunterID}.", hunter.HunterID);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error in GetQuestStats function.");
                return await CreateErrorResponse(req, "Error retrieving quest statistics.");
            }
        }

        [Function("GetAvailableQuests")]
        public async Task<HttpResponseData> GetAvailableQuests(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "quests/available")]
            HttpRequestData req)
        {
            _logger.LogInformation("🎯 GetAvailableQuests function triggered.");
            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    _logger.LogWarning("GetAvailableQuests: Unauthorized access attempt or invalid token.");
                    return await CreateUnauthorizedResponse(req);
                }
                _logger.LogInformation("GetAvailableQuests: Fetching available quests for HunterID {HunterID}", hunter.HunterID);
                var availableQuests = await _questService.GetAvailableQuestsAsync(hunter.HunterID);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<List<AvailableQuestDto>>
                {
                    Success = true,
                    Message = $"Found {availableQuests.Count} available quests! 🎯",
                    Data = availableQuests
                });
                 _logger.LogInformation("GetAvailableQuests: Successfully retrieved {Count} available quests for HunterID {HunterID}.", availableQuests.Count, hunter.HunterID);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error in GetAvailableQuests function.");
                return await CreateErrorResponse(req, "Error retrieving available quests.");
            }
        }

        [Function("GenerateDailyQuests")]
        public async Task<HttpResponseData> GenerateDailyQuests(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "quests/generate")]
            HttpRequestData req)
        {
            _logger.LogInformation("🎲 GenerateDailyQuests function triggered.");
            string requestBodyForLogging = "Not read yet";
            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                     _logger.LogWarning("GenerateDailyQuests: Unauthorized access attempt or invalid token.");
                    return await CreateUnauthorizedResponse(req);
                }

                requestBodyForLogging = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation("GenerateDailyQuests: Received request body: {RequestBody}", requestBodyForLogging);

                GenerateDailyQuestsDto? generateDto = null;
                if (!string.IsNullOrWhiteSpace(requestBodyForLogging)) 
                {
                    try
                    {
                        generateDto = JsonSerializer.Deserialize<GenerateDailyQuestsDto>(requestBodyForLogging, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "GenerateDailyQuests: JSON Deserialization error. RequestBody: {RequestBody}", requestBodyForLogging);
                        var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                        await errorResponse.WriteAsJsonAsync(new ApiResponseDto<object> { Success = false, Message = "Invalid JSON format in request body.", Errors = new List<string> { jsonEx.Message } });
                        return errorResponse;
                    }
                }
                else
                {
                    _logger.LogInformation("GenerateDailyQuests: Request body is empty. Using default values for DTO or allowing DTO to be null.");
                }

                // CORREGIDO: Lógica para determinar questDate
                var questDate = (generateDto == null || generateDto.QuestDate == DateTime.MinValue)
                    ? DateTime.UtcNow.Date
                    : generateDto.QuestDate.Date;

                // CORREGIDO: Lógica para determinar questCount
                // generateDto.NumberOfQuests es 'int', no 'int?'. Su valor por defecto en el DTO es 3 si no se especifica en el JSON.
                // Si el JSON envía "numberOfQuests": 0, entonces será 0.
                var questCount = (generateDto != null && generateDto.NumberOfQuests > 0)
                    ? generateDto.NumberOfQuests
                    : 3;
                
                _logger.LogInformation("GenerateDailyQuests: Parameters determined - HunterID {HunterID}, QuestDate: {QuestDate}, QuestCount: {QuestCount}", hunter.HunterID, questDate.ToString("o"), questCount);
                var success = await _questService.GenerateDailyQuestsAsync(hunter.HunterID, questDate, questCount);

                if (success)
                {
                    var dailyQuestsSummary = await _questService.GetDailyQuestsAsync(hunter.HunterID, questDate);
                    var response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(new ApiResponseDto<DailyQuestsSummaryDto>
                    {
                        Success = true,
                        Message = $"🎲 Generated {dailyQuestsSummary.Quests.Count} new quests for {questDate:MMM dd}! Ready to train?",
                        Data = dailyQuestsSummary
                    });
                    _logger.LogInformation("GenerateDailyQuests: Successfully generated quests for HunterID {HunterID}.", hunter.HunterID);
                    return response;
                }
                else
                {
                    _logger.LogWarning("GenerateDailyQuests: Failed to generate quests for HunterID {HunterID}. They may already exist or no available quests.", hunter.HunterID);
                    var failResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await failResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Unable to generate quests. They may already exist for this date or no quests are available for your level."
                    });
                    return failResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error in GenerateDailyQuests function. RequestBody: {RequestBody}", requestBodyForLogging);
                return await CreateErrorResponse(req, "An unexpected error occurred while generating daily quests.");
            }
        }

        // --- MÉTODOS HELPER ---
        private async Task<Hunter?> GetHunterFromToken(HttpRequestData req)
        {
            try
            {
                var authHeader = req.Headers.FirstOrDefault(h => h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase));
                var token = authHeader.Value?.FirstOrDefault()?.Replace("Bearer ", "");

                if (string.IsNullOrEmpty(token))
                {
                    // No loguear aquí como Warning si el token puede ser opcional para algunas rutas (aunque para quests no lo es)
                    // _logger.LogInformation("GetHunterFromToken: Authorization header missing or empty.");
                    return null;
                }
                // _logger.LogInformation("GetHunterFromToken: Token found, attempting to get hunter."); // Puede ser muy verboso para cada request
                return await _authService.GetHunterFromTokenAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error extracting hunter from token in GetHunterFromToken helper.");
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
                Errors = new List<string> { "Please include a valid 'Authorization: Bearer <token>' header." }
            });
            return response;
        }

        private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        {
            var response = req.CreateResponse(statusCode);
            await response.WriteAsJsonAsync(new ApiResponseDto<object>
            {
                Success = false,
                Message = message,
                Errors = new List<string> { "An error occurred. Please try again later or contact support." }
            });
            return response;
        }
    }
}