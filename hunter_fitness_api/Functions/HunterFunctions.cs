using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using HunterFitness.API.Services;
using HunterFitness.API.DTOs;

namespace HunterFitness.API.Functions
{
    public class HunterFunctions
    {
        private readonly IHunterService _hunterService;
        private readonly IAuthService _authService;
        private readonly ILogger<HunterFunctions> _logger;

        public HunterFunctions(
            IHunterService hunterService,
            IAuthService authService,
            ILogger<HunterFunctions> logger)
        {
            _hunterService = hunterService;
            _authService = authService;
            _logger = logger;
        }

        [Function("GetHunterProfile")]
        public async Task<HttpResponseData> GetProfile(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hunters/profile")] 
            HttpRequestData req)
        {
            _logger.LogInformation("👤 Get hunter profile request");

            try
            {
                // Obtener hunter del token
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                // Obtener perfil completo
                var profile = await _hunterService.GetHunterProfileAsync(hunter.HunterID);
                
                if (profile == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Hunter profile not found."
                    });
                    return notFoundResponse;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<HunterProfileDto>
                {
                    Success = true,
                    Message = $"Welcome back, {profile.HunterName}! 🏹",
                    Data = profile
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting hunter profile");
                return await CreateErrorResponse(req, "Error retrieving profile");
            }
        }

        [Function("UpdateHunterProfile")]
        public async Task<HttpResponseData> UpdateProfile(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "hunters/profile")] 
            HttpRequestData req)
        {
            _logger.LogInformation("✏️ Update hunter profile request");

            try
            {
                // Obtener hunter del token
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                // Leer body de la request
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var updateDto = JsonSerializer.Deserialize<UpdateHunterProfileDto>(requestBody, new JsonSerializerOptions
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

                // Actualizar perfil
                var result = await _hunterService.UpdateHunterProfileAsync(hunter.HunterID, updateDto);

                var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                var response = req.CreateResponse(statusCode);
                
                await response.WriteAsJsonAsync(new ApiResponseDto<HunterOperationResponseDto>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error updating hunter profile");
                return await CreateErrorResponse(req, "Error updating profile");
            }
        }

        [Function("GetHunterStats")]
        public async Task<HttpResponseData> GetStats(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hunters/stats")] 
            HttpRequestData req)
        {
            _logger.LogInformation("📊 Get hunter stats request");

            try
            {
                // Obtener hunter del token
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                // Obtener stats
                var stats = await _hunterService.GetHunterStatsAsync(hunter.HunterID);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<HunterStatsDto>
                {
                    Success = true,
                    Message = "Hunter stats retrieved successfully! 💪",
                    Data = stats
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting hunter stats");
                return await CreateErrorResponse(req, "Error retrieving stats");
            }
        }

        [Function("GetHunterProgress")]
        public async Task<HttpResponseData> GetProgress(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hunters/progress")] 
            HttpRequestData req)
        {
            _logger.LogInformation("📈 Get hunter progress request");

            try
            {
                // Obtener hunter del token
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                // Obtener progreso
                var progress = await _hunterService.GetHunterProgressAsync(hunter.HunterID);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<HunterProgressDto>
                {
                    Success = true,
                    Message = "Hunter progress retrieved successfully! 🚀",
                    Data = progress
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting hunter progress");
                return await CreateErrorResponse(req, "Error retrieving progress");
            }
        }

        [Function("ChangePassword")]
        public async Task<HttpResponseData> ChangePassword(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "hunters/change-password")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🔐 Change password request");

            try
            {
                // Obtener hunter del token
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                // Leer body de la request
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var changePasswordDto = JsonSerializer.Deserialize<ChangePasswordDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (changePasswordDto == null)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request data."
                    });
                    return badRequestResponse;
                }

                // Validaciones
                var validationErrors = ValidateChangePasswordDto(changePasswordDto);
                if (validationErrors.Any())
                {
                    var validationResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await validationResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Validation failed.",
                        Errors = validationErrors
                    });
                    return validationResponse;
                }

                // Cambiar contraseña
                var result = await _hunterService.ChangePasswordAsync(hunter.HunterID, changePasswordDto);

                var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                var response = req.CreateResponse(statusCode);
                
                await response.WriteAsJsonAsync(new ApiResponseDto<HunterOperationResponseDto>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error changing password");
                return await CreateErrorResponse(req, "Error changing password");
            }
        }

        [Function("GetLeaderboard")]
        public async Task<HttpResponseData> GetLeaderboard(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hunters/leaderboard")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🏆 Get leaderboard request");

            try
            {
                // Obtener parámetros de query
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var limitStr = query["limit"] ?? "100";
                var limit = int.TryParse(limitStr, out var parsedLimit) ? Math.Min(parsedLimit, 500) : 100;

                // Obtener hunter actual (opcional)
                var currentHunter = await GetHunterFromToken(req, required: false);

                // Obtener leaderboard
                var leaderboard = await _hunterService.GetLeaderboardAsync(limit, currentHunter?.HunterID);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<List<LeaderboardEntryDto>>
                {
                    Success = true,
                    Message = $"Top {leaderboard.Count} hunters retrieved! 🏆",
                    Data = leaderboard,
                    Metadata = new Dictionary<string, object>
                    {
                        {"TotalHunters", leaderboard.Count},
                        {"CurrentHunterRank", currentHunter != null ? 
                            leaderboard.FirstOrDefault(l => l.IsCurrentUser)?.Rank ?? 0 : 0},
                        {"Limit", limit}
                    }
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting leaderboard");
                return await CreateErrorResponse(req, "Error retrieving leaderboard");
            }
        }

        [Function("AddHunterXP")]
        public async Task<HttpResponseData> AddXP(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "hunters/add-xp")] 
            HttpRequestData req)
        {
            _logger.LogInformation("⭐ Add XP request (internal)");

            try
            {
                // Esta función es solo para uso interno del sistema
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var addXpRequest = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (addXpRequest == null || 
                    !addXpRequest.ContainsKey("hunterId") || 
                    !addXpRequest.ContainsKey("xpAmount") ||
                    !addXpRequest.ContainsKey("source"))
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request. Required: hunterId, xpAmount, source"
                    });
                    return badRequestResponse;
                }

                var hunterId = Guid.Parse(addXpRequest["hunterId"].ToString()!);
                var xpAmount = int.Parse(addXpRequest["xpAmount"].ToString()!);
                var source = addXpRequest["source"].ToString()!;

                var success = await _hunterService.AddXPAsync(hunterId, xpAmount, source);

                var statusCode = success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                var response = req.CreateResponse(statusCode);
                
                await response.WriteAsJsonAsync(new ApiResponseDto<object>
                {
                    Success = success,
                    Message = success ? $"Added {xpAmount} XP successfully!" : "Failed to add XP",
                    Data = new { HunterID = hunterId, XPAdded = xpAmount, Source = source }
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error adding XP");
                return await CreateErrorResponse(req, "Error adding XP");
            }
        }

        // Helper methods
        private async Task<Models.Hunter?> GetHunterFromToken(HttpRequestData req, bool required = true)
        {
            try
            {
                var authHeader = req.Headers.FirstOrDefault(h => h.Key.ToLower() == "authorization");
                var token = authHeader.Value?.FirstOrDefault()?.Replace("Bearer ", "");

                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                var hunter = await _authService.GetHunterFromTokenAsync(token);
                return hunter;
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

        private List<string> ValidateChangePasswordDto(ChangePasswordDto dto)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                errors.Add("Current password is required");

            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                errors.Add("New password is required");
            else if (dto.NewPassword.Length < 6)
                errors.Add("New password must be at least 6 characters long");

            if (string.IsNullOrWhiteSpace(dto.ConfirmPassword))
                errors.Add("Password confirmation is required");
            else if (dto.NewPassword != dto.ConfirmPassword)
                errors.Add("New passwords do not match");

            if (dto.CurrentPassword == dto.NewPassword)
                errors.Add("New password must be different from current password");

            return errors;
        }
    }
}