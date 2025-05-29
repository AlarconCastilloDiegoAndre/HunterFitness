using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HunterFitness.API.Data;
using HunterFitness.API.Models;
using HunterFitness.API.DTOs;

namespace HunterFitness.API.Services
{
    public interface IHunterService
    {
        Task<HunterProfileDto?> GetHunterProfileAsync(Guid hunterId);
        Task<HunterOperationResponseDto> UpdateHunterProfileAsync(Guid hunterId, UpdateHunterProfileDto updateDto);
        Task<HunterOperationResponseDto> ChangePasswordAsync(Guid hunterId, ChangePasswordDto changePasswordDto);
        Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int limit = 100, Guid? currentHunterId = null);
        Task<HunterStatsDto> GetHunterStatsAsync(Guid hunterId);
        Task<HunterProgressDto> GetHunterProgressAsync(Guid hunterId);
        Task<bool> AddXPAsync(Guid hunterId, int xpAmount, string source);
        Task<bool> UpdateStatsAsync(Guid hunterId, int strengthBonus, int agilityBonus, int vitalityBonus, int enduranceBonus);
        Task<bool> IncrementWorkoutCountAsync(Guid hunterId);
        Task<bool> UpdateStreakAsync(Guid hunterId, bool maintainStreak);
    }

    public class HunterService : IHunterService
    {
        private readonly HunterFitnessDbContext _context;
        private readonly ILogger<HunterService> _logger;

        public HunterService(HunterFitnessDbContext context, ILogger<HunterService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HunterProfileDto?> GetHunterProfileAsync(Guid hunterId)
        {
            try
            {
                var hunter = await _context.Hunters
                    .Include(h => h.Equipment.Where(e => e.IsEquipped))
                        .ThenInclude(e => e.Equipment)
                    .FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);

                if (hunter == null)
                {
                    _logger.LogWarning("🔍 Hunter not found: {HunterID}", hunterId);
                    return null;
                }

                return await ConvertToHunterProfileDto(hunter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting hunter profile: {HunterID}", hunterId);
                return null;
            }
        }

        public async Task<HunterOperationResponseDto> UpdateHunterProfileAsync(Guid hunterId, UpdateHunterProfileDto updateDto)
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                
                if (hunter == null)
                {
                    return new HunterOperationResponseDto
                    {
                        Success = false,
                        Message = "Hunter not found."
                    };
                }

                // Actualizar campos si se proporcionaron
                if (!string.IsNullOrWhiteSpace(updateDto.HunterName))
                {
                    hunter.HunterName = updateDto.HunterName.Trim();
                }

                if (!string.IsNullOrWhiteSpace(updateDto.ProfilePictureUrl))
                {
                    hunter.ProfilePictureUrl = updateDto.ProfilePictureUrl.Trim();
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Hunter profile updated: {HunterID}", hunterId);

                var updatedProfile = await GetHunterProfileAsync(hunterId);
                return new HunterOperationResponseDto
                {
                    Success = true,
                    Message = "Profile updated successfully! 🏹",
                    Hunter = updatedProfile
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error updating hunter profile: {HunterID}", hunterId);
                return new HunterOperationResponseDto
                {
                    Success = false,
                    Message = "An error occurred while updating your profile."
                };
            }
        }

        public async Task<HunterOperationResponseDto> ChangePasswordAsync(Guid hunterId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                
                if (hunter == null)
                {
                    return new HunterOperationResponseDto
                    {
                        Success = false,
                        Message = "Hunter not found."
                    };
                }

                // Verificar contraseña actual
                if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, hunter.PasswordHash))
                {
                    return new HunterOperationResponseDto
                    {
                        Success = false,
                        Message = "Current password is incorrect."
                    };
                }

                // Verificar que las nuevas contraseñas coincidan
                if (changePasswordDto.NewPassword != changePasswordDto.ConfirmPassword)
                {
                    return new HunterOperationResponseDto
                    {
                        Success = false,
                        Message = "New passwords do not match."
                    };
                }

                // Actualizar contraseña
                hunter.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation("🔐 Password changed for hunter: {HunterID}", hunterId);

                return new HunterOperationResponseDto
                {
                    Success = true,
                    Message = "Password changed successfully! 🔒"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error changing password: {HunterID}", hunterId);
                return new HunterOperationResponseDto
                {
                    Success = false,
                    Message = "An error occurred while changing your password."
                };
            }
        }

        public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int limit = 100, Guid? currentHunterId = null)
        {
            try
            {
                var hunters = await _context.Hunters
                    .Where(h => h.IsActive)
                    .OrderByDescending(h => h.TotalXP)
                    .ThenByDescending(h => h.Level)
                    .ThenByDescending(h => h.LongestStreak)
                    .Take(limit)
                    .Select(h => new LeaderboardEntryDto
                    {
                        HunterID = h.HunterID,
                        Username = h.Username,
                        HunterName = h.HunterName,
                        Level = h.Level,
                        TotalXP = h.TotalXP,
                        HunterRank = h.HunterRank,
                        RankDisplayName = h.GetRankDisplayName(),
                        DailyStreak = h.DailyStreak,
                        LongestStreak = h.LongestStreak,
                        TotalWorkouts = h.TotalWorkouts,
                        ProfilePictureUrl = h.ProfilePictureUrl,
                        IsCurrentUser = currentHunterId.HasValue && h.HunterID == currentHunterId.Value,
                        RankChange = "=" // TODO: Implementar cambio de ranking
                    })
                    .ToListAsync();

                // Asignar rankings
                for (int i = 0; i < hunters.Count; i++)
                {
                    hunters[i].Rank = i + 1;
                }

                return hunters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting leaderboard");
                return new List<LeaderboardEntryDto>();
            }
        }

        public async Task<HunterStatsDto> GetHunterStatsAsync(Guid hunterId)
        {
            try
            {
                var hunter = await _context.Hunters
                    .Include(h => h.Equipment.Where(e => e.IsEquipped))
                        .ThenInclude(e => e.Equipment)
                    .FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);

                if (hunter == null)
                {
                    throw new ArgumentException("Hunter not found");
                }

                var equippedItems = hunter.Equipment?.Where(e => e.IsEquipped && e.Equipment != null) ?? new List<HunterEquipment>();
                
                var strengthBonus = equippedItems.Sum(e => e.Equipment?.StrengthBonus ?? 0);
                var agilityBonus = equippedItems.Sum(e => e.Equipment?.AgilityBonus ?? 0);
                var vitalityBonus = equippedItems.Sum(e => e.Equipment?.VitalityBonus ?? 0);
                var enduranceBonus = equippedItems.Sum(e => e.Equipment?.EnduranceBonus ?? 0);
                var xpMultiplier = equippedItems.Where(e => e.Equipment != null).Sum(e => e.Equipment.XPMultiplier - 1.0m) + 1.0m;

                return new HunterStatsDto
                {
                    HunterID = hunter.HunterID,
                    HunterName = hunter.HunterName,
                    Level = hunter.Level,
                    HunterRank = hunter.HunterRank,
                    BaseStrength = hunter.Strength,
                    BaseAgility = hunter.Agility,
                    BaseVitality = hunter.Vitality,
                    BaseEndurance = hunter.Endurance,
                    TotalStrength = hunter.Strength + strengthBonus,
                    TotalAgility = hunter.Agility + agilityBonus,
                    TotalVitality = hunter.Vitality + vitalityBonus,
                    TotalEndurance = hunter.Endurance + enduranceBonus,
                    StrengthBonus = strengthBonus,
                    AgilityBonus = agilityBonus,
                    VitalityBonus = vitalityBonus,
                    EnduranceBonus = enduranceBonus,
                    TotalStatsBase = hunter.Strength + hunter.Agility + hunter.Vitality + hunter.Endurance,
                    TotalStatsWithEquipment = hunter.GetTotalStatsWithEquipment(),
                    XPMultiplier = xpMultiplier
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting hunter stats: {HunterID}", hunterId);
                throw;
            }
        }

        public async Task<HunterProgressDto> GetHunterProgressAsync(Guid hunterId)
        {
            try
            {
                var hunter = await _context.Hunters
                    .Include(h => h.Achievements.Where(a => a.IsUnlocked))
                        .ThenInclude(a => a.Achievement)
                    .FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);

                if (hunter == null)
                {
                    throw new ArgumentException("Hunter not found");
                }

                // Obtener achievements recientes (últimos 7 días)
                var recentAchievements = hunter.Achievements?
                    .Where(a => a.IsUnlocked && a.UnlockedAt >= DateTime.UtcNow.AddDays(-7))
                    .Select(a => new RecentAchievementDto
                    {
                        AchievementID = a.Achievement.AchievementID,
                        AchievementName = a.Achievement.AchievementName,
                        Description = a.Achievement.Description,
                        Category = a.Achievement.Category,
                        XPReward = a.Achievement.XPReward,
                        TitleReward = a.Achievement.TitleReward,
                        UnlockedAt = a.UnlockedAt ?? DateTime.UtcNow,
                        IconUrl = a.Achievement.IconUrl,
                        RarityColor = a.Achievement.GetRarityColor(),
                        IsNew = a.UnlockedAt >= DateTime.UtcNow.AddDays(-1)
                    })
                    .OrderByDescending(a => a.UnlockedAt)
                    .ToList() ?? new List<RecentAchievementDto>();

                // Obtener workouts de esta semana/mes
                var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
                var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

                var weeklyWorkouts = await _context.QuestHistory
                    .Where(q => q.HunterID == hunterId && q.CompletedAt >= startOfWeek)
                    .CountAsync();

                var monthlyWorkouts = await _context.QuestHistory
                    .Where(q => q.HunterID == hunterId && q.CompletedAt >= startOfMonth)
                    .CountAsync();

                var todayWorkouts = await _context.QuestHistory
                    .Where(q => q.HunterID == hunterId && q.CompletedAt.Date == DateTime.UtcNow.Date)
                    .CountAsync();

                var xpForNextLevel = hunter.GetXPRequiredForNextLevel();
                var levelProgressPercentage = xpForNextLevel > 0 ? (decimal)hunter.CurrentXP / xpForNextLevel * 100 : 0;

                return new HunterProgressDto
                {
                    HunterID = hunter.HunterID,
                    HunterName = hunter.HunterName,
                    CurrentLevel = hunter.Level,
                    CurrentXP = hunter.CurrentXP,
                    XPRequiredForNextLevel = xpForNextLevel,
                    LevelProgressPercentage = levelProgressPercentage,
                    CurrentRank = hunter.HunterRank,
                    NextRank = GetNextRank(hunter.HunterRank),
                    LevelRequiredForNextRank = GetLevelRequiredForNextRank(hunter.HunterRank),
                    TodayWorkouts = todayWorkouts,
                    WeeklyWorkouts = weeklyWorkouts,
                    MonthlyWorkouts = monthlyWorkouts,
                    CurrentStreak = hunter.DailyStreak,
                    LongestStreak = hunter.LongestStreak,
                    RecentAchievements = recentAchievements,
                    StatGrowthThisWeek = new Dictionary<string, int>
                    {
                        {"Strength", 0}, // TODO: Implementar tracking de crecimiento de stats
                        {"Agility", 0},
                        {"Vitality", 0},
                        {"Endurance", 0}
                    },
                    StatGrowthThisMonth = new Dictionary<string, int>
                    {
                        {"Strength", 0}, // TODO: Implementar tracking de crecimiento de stats
                        {"Agility", 0},
                        {"Vitality", 0},
                        {"Endurance", 0}
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting hunter progress: {HunterID}", hunterId);
                throw;
            }
        }

        public async Task<bool> AddXPAsync(Guid hunterId, int xpAmount, string source)
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null) return false;

                var oldLevel = hunter.Level;
                hunter.CurrentXP += xpAmount;
                hunter.TotalXP += xpAmount;

                // Verificar level up
                while (hunter.CanLevelUp())
                {
                    var xpRequired = hunter.GetXPRequiredForNextLevel();
                    hunter.CurrentXP -= xpRequired;
                    hunter.Level++;
                    
                    _logger.LogInformation("🎉 LEVEL UP! Hunter {HunterID} reached level {Level}", hunterId, hunter.Level);
                }

                // Actualizar rank basado en nivel
                hunter.UpdateRankBasedOnLevel();

                await _context.SaveChangesAsync();

                _logger.LogInformation("⭐ XP Added: {XP} to Hunter {HunterID} from {Source}", xpAmount, hunterId, source);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error adding XP: {HunterID}", hunterId);
                return false;
            }
        }

        public async Task<bool> UpdateStatsAsync(Guid hunterId, int strengthBonus, int agilityBonus, int vitalityBonus, int enduranceBonus)
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null) return false;

                hunter.Strength += strengthBonus;
                hunter.Agility += agilityBonus;
                hunter.Vitality += vitalityBonus;
                hunter.Endurance += enduranceBonus;

                // Asegurar que los stats no sean negativos
                hunter.Strength = Math.Max(1, hunter.Strength);
                hunter.Agility = Math.Max(1, hunter.Agility);
                hunter.Vitality = Math.Max(1, hunter.Vitality);
                hunter.Endurance = Math.Max(1, hunter.Endurance);

                await _context.SaveChangesAsync();

                _logger.LogInformation("📊 Stats updated for Hunter {HunterID}: STR+{Str}, AGI+{Agi}, VIT+{Vit}, END+{End}", 
                    hunterId, strengthBonus, agilityBonus, vitalityBonus, enduranceBonus);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error updating stats: {HunterID}", hunterId);
                return false;
            }
        }

        public async Task<bool> IncrementWorkoutCountAsync(Guid hunterId)
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null) return false;

                hunter.TotalWorkouts++;
                await _context.SaveChangesAsync();

                _logger.LogInformation("💪 Workout count incremented for Hunter {HunterID}: {Count}", hunterId, hunter.TotalWorkouts);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error incrementing workout count: {HunterID}", hunterId);
                return false;
            }
        }

        public async Task<bool> UpdateStreakAsync(Guid hunterId, bool maintainStreak)
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null) return false;

                if (maintainStreak)
                {
                    hunter.DailyStreak++;
                    if (hunter.DailyStreak > hunter.LongestStreak)
                    {
                        hunter.LongestStreak = hunter.DailyStreak;
                        _logger.LogInformation("🔥 NEW LONGEST STREAK! Hunter {HunterID}: {Streak} days", hunterId, hunter.LongestStreak);
                    }
                }
                else
                {
                    _logger.LogInformation("💔 Streak broken for Hunter {HunterID}. Was: {Streak} days", hunterId, hunter.DailyStreak);
                    hunter.DailyStreak = 0;
                }

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error updating streak: {HunterID}", hunterId);
                return false;
            }
        }

        private async Task<HunterProfileDto> ConvertToHunterProfileDto(Hunter hunter)
        {
            // Asegurar que equipment esté cargado
            if (hunter.Equipment == null || !hunter.Equipment.Any())
            {
                hunter = await _context.Hunters
                    .Include(h => h.Equipment.Where(e => e.IsEquipped))
                        .ThenInclude(e => e.Equipment)
                    .FirstOrDefaultAsync(h => h.HunterID == hunter.HunterID) ?? hunter;
            }

            // Obtener equipment equipado
            var equippedItems = hunter.Equipment?
                .Where(e => e.IsEquipped && e.Equipment != null)
                .Select(e => new EquippedItemDto
                {
                    EquipmentID = e.Equipment!.EquipmentID,
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

            var xpForNextLevel = hunter.GetXPRequiredForNextLevel();
            var levelProgressPercentage = xpForNextLevel > 0 ? (decimal)hunter.CurrentXP / xpForNextLevel * 100 : 0;
            var daysSinceJoining = Math.Max(1, (DateTime.UtcNow - hunter.CreatedAt).Days);

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
                XPRequiredForNextLevel = xpForNextLevel,
                LevelProgressPercentage = levelProgressPercentage,
                CanLevelUp = hunter.CanLevelUp(),
                NextRankRequirement = hunter.GetNextRankRequirement(),
                CreatedAt = hunter.CreatedAt,
                LastLoginAt = hunter.LastLoginAt,
                ProfilePictureUrl = hunter.ProfilePictureUrl,
                EquippedItems = equippedItems,
                AdditionalStats = new Dictionary<string, object>
                {
                    {"JoinedDaysAgo", daysSinceJoining},
                    {"AverageXPPerDay", hunter.TotalXP / daysSinceJoining},
                    {"EquippedItemsCount", equippedItems.Count},
                    {"TotalPowerLevel", equippedItems.Sum(e => e.PowerLevel)}
                }
            };
        }

        private static string GetNextRank(string currentRank)
        {
            return currentRank switch
            {
                "E" => "D",
                "D" => "C",
                "C" => "B",
                "B" => "A",
                "A" => "S",
                "S" => "SS",
                "SS" => "SSS",
                "SSS" => "SSS", // Maximum rank
                _ => "E"
            };
        }

        private static int GetLevelRequiredForNextRank(string currentRank)
        {
            return currentRank switch
            {
                "E" => 11,
                "D" => 21,
                "C" => 36,
                "B" => 51,
                "A" => 71,
                "S" => 86,
                "SS" => 96,
                "SSS" => 100, // Maximum level
                _ => 11
            };
        }
    }
}