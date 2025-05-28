using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HunterFitness.API.Data;
using HunterFitness.API.Models;
using HunterFitness.API.DTOs;

namespace HunterFitness.API.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterHunterAsync(RegisterHunterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RefreshTokenAsync(string token);
        Task<bool> ValidateTokenAsync(string token);
        Task<Hunter?> GetHunterFromTokenAsync(string token);
        string GenerateJwtToken(Hunter hunter);
    }

    public class AuthService : IAuthService
    {
        private readonly HunterFitnessDbContext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpireHours;

        public AuthService(
            HunterFitnessDbContext context,
            ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
            
            // Configuración JWT desde environment variables
            _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? 
                        "HunterFitness_SuperSecretKey_2025_ShadowMonarch_JinWoo";
            _jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "HunterFitnessAPI";
            _jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "HunterFitnessApp";
            _jwtExpireHours = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRE_HOURS") ?? "168"); // 1 semana
        }

        public async Task<AuthResponseDto> RegisterHunterAsync(RegisterHunterDto registerDto)
        {
            try
            {
                _logger.LogInformation("🏹 Attempting to register new hunter: {Username}", registerDto.Username);

                // Validar que no exista el username o email
                var existingHunter = await _context.Hunters
                    .FirstOrDefaultAsync(h => h.Username == registerDto.Username || h.Email == registerDto.Email);

                if (existingHunter != null)
                {
                    var conflict = existingHunter.Username == registerDto.Username ? "Username" : "Email";
                    _logger.LogWarning("❌ Registration failed - {Conflict} already exists: {Value}", 
                        conflict, conflict == "Username" ? registerDto.Username : registerDto.Email);
                    
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = $"{conflict} already exists. Choose a different {conflict.ToLower()}."
                    };
                }

                // Crear nuevo hunter
                var hunter = new Hunter
                {
                    Username = registerDto.Username.Trim(),
                    Email = registerDto.Email.ToLower().Trim(),
                    HunterName = registerDto.HunterName.Trim(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };

                _context.Hunters.Add(hunter);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Hunter registered successfully: {HunterID} - {Username}", 
                    hunter.HunterID, hunter.Username);

                // Generar token JWT
                var token = GenerateJwtToken(hunter);

                // Convertir a DTO
                var hunterProfile = await ConvertToHunterProfileDto(hunter);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = $"Welcome to Hunter Fitness, {hunter.HunterName}! Your journey begins now! 🏹⚔️",
                    Token = token,
                    Hunter = hunterProfile
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error registering hunter: {Username}", registerDto.Username);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during registration. Please try again."
                };
            }
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            try
            {
                _logger.LogInformation("🔐 Login attempt for: {Username}", loginDto.Username);

                // Buscar hunter por username o email
                var hunter = await _context.Hunters
                    .Include(h => h.Equipment.Where(e => e.IsEquipped))
                        .ThenInclude(e => e.Equipment)
                    .FirstOrDefaultAsync(h => 
                        (h.Username == loginDto.Username || h.Email == loginDto.Username) && 
                        h.IsActive);

                if (hunter == null)
                {
                    _logger.LogWarning("❌ Login failed - Hunter not found: {Username}", loginDto.Username);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid username or password."
                    };
                }

                // Verificar contraseña
                if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, hunter.PasswordHash))
                {
                    _logger.LogWarning("❌ Login failed - Invalid password for: {Username}", loginDto.Username);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid username or password."
                    };
                }

                // Actualizar último login
                hunter.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Login successful: {HunterID} - {Username}", 
                    hunter.HunterID, hunter.Username);

                // Generar token JWT
                var token = GenerateJwtToken(hunter);

                // Convertir a DTO
                var hunterProfile = await ConvertToHunterProfileDto(hunter);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = $"Welcome back, {hunter.HunterName}! Ready for today's challenges? 🔥",
                    Token = token,
                    Hunter = hunterProfile
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error during login: {Username}", loginDto.Username);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during login. Please try again."
                };
            }
        }

        public string GenerateJwtToken(Hunter hunter)
        {
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, hunter.HunterID.ToString()),
                new(ClaimTypes.Name, hunter.Username),
                new(ClaimTypes.Email, hunter.Email),
                new("HunterName", hunter.HunterName),
                new("Level", hunter.Level.ToString()),
                new("Rank", hunter.HunterRank),
                new("TotalXP", hunter.TotalXP.ToString()),
                new("CreatedAt", hunter.CreatedAt.ToString("O"))
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(_jwtExpireHours),
                Issuer = _jwtIssuer,
                Audience = _jwtAudience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string token)
        {
            try
            {
                var hunter = await GetHunterFromTokenAsync(token);
                if (hunter == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid token."
                    };
                }

                // Generar nuevo token
                var newToken = GenerateJwtToken(hunter);
                var hunterProfile = await ConvertToHunterProfileDto(hunter);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Token refreshed successfully.",
                    Token = newToken,
                    Hunter = hunterProfile
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error refreshing token");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Error refreshing token."
                };
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                return validatedToken != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Hunter?> GetHunterFromTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                var hunterIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);

                if (hunterIdClaim != null && Guid.TryParse(hunterIdClaim.Value, out var hunterId))
                {
                    return await _context.Hunters
                        .Include(h => h.Equipment.Where(e => e.IsEquipped))
                            .ThenInclude(e => e.Equipment)
                        .FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting hunter from token");
                return null;
            }
        }

        private async Task<HunterProfileDto> ConvertToHunterProfileDto(Hunter hunter)
        {
            // Obtener equipment equipado
            var equippedItems = hunter.Equipment?
                .Where(e => e.IsEquipped && e.Equipment != null)
                .Select(e => new EquippedItemDto
                {
                    EquipmentID = e.Equipment.EquipmentID,
                    ItemName = e.Equipment.ItemName,
                    ItemType = e.Equipment.ItemType,
                    Rarity = e.Equipment.Rarity,
                    RarityColor = e.Equipment.GetRarityColor(),
                    StrengthBonus = e.Equipment.StrengthBonus,
                    AgilityBonus = e.Equipment.AgilityBonus,
                    VitalityBonus = e.Equipment.VitalityBonus,
                    EnduranceBonus = e.Equipment.EnduranceBonus,
                    XPMultiplier = e.Equipment.XPMultiplier,
                    StatBonusDescription = e.Equipment.GetStatBonusDescription(),
                    IconUrl = e.Equipment.IconUrl,
                    PowerLevel = e.Equipment.GetPowerLevel()
                }).ToList() ?? new List<EquippedItemDto>();

            return new HunterProfileDto
            {
                HunterID = hunter.HunterID,
                Username = hunter.Username,
                Email = hunter.Email,
                HunterName = hunter.HunterName,
                Level = hunter.Level,
                CurrentXP = hunter.CurrentXP,
                TotalXP = hunter.TotalXP,
                HunterRank = hunter.HunterRank,
                RankDisplayName = hunter.GetRankDisplayName(),
                Strength = hunter.Strength,
                Agility = hunter.Agility,
                Vitality = hunter.Vitality,
                Endurance = hunter.Endurance,
                TotalStats = hunter.GetTotalStatsWithEquipment(),
                DailyStreak = hunter.DailyStreak,
                LongestStreak = hunter.LongestStreak,
                TotalWorkouts = hunter.TotalWorkouts,
                XPRequiredForNextLevel = hunter.GetXPRequiredForNextLevel(),
                LevelProgressPercentage = hunter.CurrentXP * 100m / hunter.GetXPRequiredForNextLevel(),
                CanLevelUp = hunter.CanLevelUp(),
                NextRankRequirement = hunter.GetNextRankRequirement(),
                CreatedAt = hunter.CreatedAt,
                LastLoginAt = hunter.LastLoginAt,
                ProfilePictureUrl = hunter.ProfilePictureUrl,
                EquippedItems = equippedItems,
                AdditionalStats = new Dictionary<string, object>
                {
                    {"JoinedDaysAgo", (DateTime.UtcNow - hunter.CreatedAt).Days},
                    {"AverageXPPerDay", hunter.TotalXP / Math.Max(1, (DateTime.UtcNow - hunter.CreatedAt).Days)},
                    {"EquippedItemsCount", equippedItems.Count},
                    {"TotalPowerLevel", equippedItems.Sum(e => e.PowerLevel)}
                }
            };
        }
    }
}