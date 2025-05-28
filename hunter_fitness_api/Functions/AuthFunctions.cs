using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using HunterFitness.API.Services;
using HunterFitness.API.DTOs;

namespace HunterFitness.API.Functions
{
    public class AuthFunctions
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthFunctions> _logger;

        public AuthFunctions(IAuthService authService, ILogger<AuthFunctions> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [Function("RegisterHunter")]
        public async Task<HttpResponseData> Register(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🏹 New hunter registration attempt");

            try
            {
                // Leer body de la request
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var registerDto = JsonSerializer.Deserialize<RegisterHunterDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (registerDto == null)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request data.",
                        Errors = new List<string> { "Request body is required" }
                    });
                    return badRequestResponse;
                }

                // Validaciones básicas
                var validationErrors = ValidateRegisterDto(registerDto);
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

                // Registrar hunter
                var result = await _authService.RegisterHunterAsync(registerDto);

                var statusCode = result.Success ? HttpStatusCode.Created : HttpStatusCode.BadRequest;
                var response = req.CreateResponse(statusCode);
                
                await response.WriteAsJsonAsync(new ApiResponseDto<AuthResponseDto>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error in RegisterHunter function");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An internal error occurred.",
                    Errors = new List<string> { "Please try again later" }
                });
                
                return errorResponse;
            }
        }

        [Function("LoginHunter")]
        public async Task<HttpResponseData> Login(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🔐 Hunter login attempt");

            try
            {
                // Leer body de la request
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var loginDto = JsonSerializer.Deserialize<LoginDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginDto == null)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid request data.",
                        Errors = new List<string> { "Request body is required" }
                    });
                    return badRequestResponse;
                }

                // Validaciones básicas
                var validationErrors = ValidateLoginDto(loginDto);
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

                // Login
                var result = await _authService.LoginAsync(loginDto);

                var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.Unauthorized;
                var response = req.CreateResponse(statusCode);
                
                await response.WriteAsJsonAsync(new ApiResponseDto<AuthResponseDto>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error in LoginHunter function");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An internal error occurred.",
                    Errors = new List<string> { "Please try again later" }
                });
                
                return errorResponse;
            }
        }

        [Function("RefreshToken")]
        public async Task<HttpResponseData> RefreshToken(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/refresh")] 
            HttpRequestData req)
        {
            _logger.LogInformation("🔄 Token refresh attempt");

            try
            {
                // Obtener token del header Authorization
                var authHeader = req.Headers.FirstOrDefault(h => h.Key.ToLower() == "authorization");
                var token = authHeader.Value?.FirstOrDefault()?.Replace("Bearer ", "");

                if (string.IsNullOrEmpty(token))
                {
                    var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                    await unauthorizedResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Authorization token is required.",
                        Errors = new List<string> { "Include 'Authorization: Bearer <token>' header" }
                    });
                    return unauthorizedResponse;
                }

                // Refresh token
                var result = await _authService.RefreshTokenAsync(token);

                var statusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.Unauthorized;
                var response = req.CreateResponse(statusCode);
                
                await response.WriteAsJsonAsync(new ApiResponseDto<AuthResponseDto>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error in RefreshToken function");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An internal error occurred.",
                    Errors = new List<string> { "Please try again later" }
                });
                
                return errorResponse;
            }
        }

        [Function("ValidateToken")]
        public async Task<HttpResponseData> ValidateToken(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/validate")] 
            HttpRequestData req)
        {
            _logger.LogInformation("✅ Token validation attempt");

            try
            {
                // Obtener token del header Authorization
                var authHeader = req.Headers.FirstOrDefault(h => h.Key.ToLower() == "authorization");
                var token = authHeader.Value?.FirstOrDefault()?.Replace("Bearer ", "");

                if (string.IsNullOrEmpty(token))
                {
                    var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                    await unauthorizedResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Authorization token is required."
                    });
                    return unauthorizedResponse;
                }

                // Validar token
                var isValid = await _authService.ValidateTokenAsync(token);
                var hunter = isValid ? await _authService.GetHunterFromTokenAsync(token) : null;

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ApiResponseDto<object>
                {
                    Success = isValid,
                    Message = isValid ? "Token is valid" : "Token is invalid or expired",
                    Data = new 
                    {
                        IsValid = isValid,
                        HunterInfo = hunter != null ? new 
                        {
                            hunter.HunterID,
                            hunter.Username,
                            hunter.HunterName,
                            hunter.Level,
                            hunter.HunterRank
                        } : null
                    }
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error in ValidateToken function");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An internal error occurred."
                });
                
                return errorResponse;
            }
        }

        private List<string> ValidateRegisterDto(RegisterHunterDto dto)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.Username))
                errors.Add("Username is required");
            else if (dto.Username.Length < 3 || dto.Username.Length > 20)
                errors.Add("Username must be between 3 and 20 characters");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Username, @"^[a-zA-Z0-9_-]+$"))
                errors.Add("Username can only contain letters, numbers, underscores and hyphens");

            if (string.IsNullOrWhiteSpace(dto.Email))
                errors.Add("Email is required");
            else if (!IsValidEmail(dto.Email))
                errors.Add("Invalid email format");

            if (string.IsNullOrWhiteSpace(dto.Password))
                errors.Add("Password is required");
            else if (dto.Password.Length < 6)
                errors.Add("Password must be at least 6 characters long");

            if (string.IsNullOrWhiteSpace(dto.HunterName))
                errors.Add("Hunter name is required");
            else if (dto.HunterName.Length < 2 || dto.HunterName.Length > 50)
                errors.Add("Hunter name must be between 2 and 50 characters");

            return errors;
        }

        private List<string> ValidateLoginDto(LoginDto dto)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.Username))
                errors.Add("Username or email is required");

            if (string.IsNullOrWhiteSpace(dto.Password))
                errors.Add("Password is required");

            return errors;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}