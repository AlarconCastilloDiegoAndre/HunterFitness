using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using HunterFitness.API.Services;
using HunterFitness.API.DTOs;

namespace HunterFitness.API.Functions
{
    public class EquipmentFunctions
    {
        private readonly IEquipmentService _equipmentService;
        private readonly IAuthService _authService;
        private readonly ILogger<EquipmentFunctions> _logger;

        public EquipmentFunctions(
            IEquipmentService equipmentService,
            IAuthService authService,
            ILogger<EquipmentFunctions> logger)
        {
            _equipmentService = equipmentService;
            _authService = authService;
            _logger = logger;
        }

        [Function("GetHunterInventory")]
        public async Task<HttpResponseData> GetHunterInventory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "equipment/inventory")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🎒 Get hunter inventory request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var inventory = await _equipmentService.GetHunterInventoryAsync(hunter.HunterID);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<HunterInventoryDto>
                {
                    Success = true,
                    Message = $"Inventory loaded! {inventory.TotalItems} items, {inventory.EquippedItemsCount} equipped! 🎒",
                    Data = inventory
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting hunter inventory");
                return await CreateErrorResponse(req, "Error retrieving inventory");
            }
        }

        [Function("GetAvailableEquipment")]
        public async Task<HttpResponseData> GetAvailableEquipment(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "equipment/available")] 
            HttpRequestData req)
        {
            _logger.LogInformation("⚔️ Get available equipment request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var equipment = await _equipmentService.GetAvailableEquipmentAsync(hunter.HunterID);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<List<EquipmentDto>>
                {
                    Success = true,
                    Message = $"Found {equipment.Count} equipment items! ⚔️",
                    Data = equipment
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting available equipment");
                return await CreateErrorResponse(req, "Error retrieving available equipment");
            }
        }

        [Function("EquipItem")]
        public async Task<HttpResponseData> EquipItem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "equipment/equip")] 
            HttpRequestData req)
        {
            _logger.LogInformation("⚔️ Equip item request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var equipDto = JsonSerializer.Deserialize<EquipItemDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (equipDto == null)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request data."
                    });
                    return badRequestResponse;
                }

                var result = await _equipmentService.EquipItemAsync(hunter.HunterID, equipDto);

                var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                var response = req.CreateResponse(statusCode);
                
                await response.WriteAsJsonAsync(new ApiResponseDto<EquipmentOperationResponseDto>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error equipping item");
                return await CreateErrorResponse(req, "Error equipping item");
            }
        }

        [Function("UnequipItem")]
        public async Task<HttpResponseData> UnequipItem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "equipment/unequip")] 
            HttpRequestData req)
        {
            _logger.LogInformation("📦 Unequip item request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var equipDto = JsonSerializer.Deserialize<EquipItemDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (equipDto == null)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request data."
                    });
                    return badRequestResponse;
                }

                equipDto.Equip = false; // Asegurar que sea unequip
                var result = await _equipmentService.UnequipItemAsync(hunter.HunterID, equipDto);

                var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                var response = req.CreateResponse(statusCode);
                
                await response.WriteAsJsonAsync(new ApiResponseDto<EquipmentOperationResponseDto>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error unequipping item");
                return await CreateErrorResponse(req, "Error unequipping item");
            }
        }

        [Function("GetEquippedItems")]
        public async Task<HttpResponseData> GetEquippedItems(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "equipment/equipped")] 
            HttpRequestData req)
        {
            _logger.LogInformation("⚔️ Get equipped items request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var equippedItems = await _equipmentService.GetEquippedItemsAsync(hunter.HunterID);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<List<HunterEquipmentDto>>
                {
                    Success = true,
                    Message = $"Currently equipped: {equippedItems.Count} items! ⚔️",
                    Data = equippedItems
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting equipped items");
                return await CreateErrorResponse(req, "Error retrieving equipped items");
            }
        }

        [Function("GetInventoryStats")]
        public async Task<HttpResponseData> GetInventoryStats(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "equipment/stats")] 
            HttpRequestData req)
        {
            _logger.LogInformation("📊 Get inventory stats request");

            try
            {
                var hunter = await GetHunterFromToken(req);
                if (hunter == null)
                {
                    return await CreateUnauthorizedResponse(req);
                }

                var stats = await _equipmentService.GetInventoryStatsAsync(hunter.HunterID);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<Dictionary<string, object>>
                {
                    Success = true,
                    Message = "Inventory statistics retrieved! 📊",
                    Data = stats
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting inventory stats");
                return await CreateErrorResponse(req, "Error retrieving inventory statistics");
            }
        }

        [Function("UnlockEquipment")]
        public async Task<HttpResponseData> UnlockEquipment(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "equipment/unlock")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🎁 Unlock equipment request (internal)");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var unlockRequest = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (unlockRequest == null || 
                    !unlockRequest.ContainsKey("hunterId") || 
                    !unlockRequest.ContainsKey("equipmentId"))
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request. Required: hunterId, equipmentId"
                    });
                    return badRequestResponse;
                }

                var hunterId = Guid.Parse(unlockRequest["hunterId"].ToString()!);
                var equipmentId = Guid.Parse(unlockRequest["equipmentId"].ToString()!);
                var reason = unlockRequest.ContainsKey("reason") ? unlockRequest["reason"].ToString()! : "System";

                var success = await _equipmentService.UnlockEquipmentAsync(hunterId, equipmentId, reason);

                var statusCode = success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                var response = req.CreateResponse(statusCode);
                
                await response.WriteAsJsonAsync(new ApiResponseDto<object>
                {
                    Success = success,
                    Message = success ? "Equipment unlocked successfully!" : "Failed to unlock equipment",
                    Data = new { HunterID = hunterId, EquipmentID = equipmentId, Reason = reason }
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error unlocking equipment");
                return await CreateErrorResponse(req, "Error unlocking equipment");
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