using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HunterFitness.API.Data;
using HunterFitness.API.Models;
using HunterFitness.API.DTOs;

namespace HunterFitness.API.Services
{
    public interface IAchievementService
    {
        Task<HunterAchievementsDto> GetHunterAchievementsAsync(Guid hunterId);
        Task<List<AchievementDto>> GetAvailableAchievementsAsync(Guid hunterId);
        Task<List<AchievementDto>> CheckAndUpdateAchievementsAsync(Guid hunterId, string eventType, int incrementValue = 1);
        Task<bool> UnlockAchievementAsync(Guid hunterId, Guid achievementId);
        Task<List<AchievementDto>> GetAchievementsByCategoryAsync(string category, Guid hunterId);
        Task<Dictionary<string, object>> GetAchievementStatsAsync(Guid hunterId);
    }

    public class AchievementService : IAchievementService
    {
        private readonly HunterFitnessDbContext _context;
        private readonly ILogger<AchievementService> _logger;

        public AchievementService(
            HunterFitnessDbContext context,
            ILogger<AchievementService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HunterAchievementsDto> GetHunterAchievementsAsync(Guid hunterId)
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null)
                {
                    throw new ArgumentException("Hunter not found");
                }

                // Obtener todos los achievements del hunter
                var hunterAchievements = await _context.HunterAchievements
                    .Include(ha => ha.Achievement)
                    .Where(ha => ha.HunterID == hunterId)
                    .ToListAsync();

                // Obtener todos los achievements disponibles
                var allAchievements = await _context.Achievements
                    .Where(a => a.IsActive)
                    .ToListAsync();

                var achievementDtos = new List<AchievementDto>();

                foreach (var achievement in allAchievements)
                {
                    var hunterAchievement = hunterAchievements.FirstOrDefault(ha => ha.AchievementID == achievement.AchievementID);
                    var dto = ConvertToAchievementDto(achievement, hunterAchievement, hunter);
                    achievementDtos.Add(dto);
                }

                // Categorizar achievements
                var unlockedAchievements = achievementDtos.Where(a => a.IsUnlocked).OrderByDescending(a => a.UnlockedAt).ToList();
                var inProgressAchievements = achievementDtos.Where(a => !a.IsUnlocked && a.CurrentProgress > 0).OrderByDescending(a => a.ProgressPercentage).ToList();
                var availableAchievements = achievementDtos.Where(a => !a.IsUnlocked && a.CurrentProgress == 0 && !a.IsHidden).OrderBy(a => a.Category).ThenBy(a => a.AchievementName).ToList();
                var hiddenAchievements = achievementDtos.Where(a => a.IsHidden && !a.IsUnlocked).ToList();

                // Agrupar por categoría
                var achievementsByCategory = achievementDtos
                    .GroupBy(a => a.Category)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Achievements recientes (últimos 7 días)
                var recentlyUnlocked = unlockedAchievements
                    .Where(a => a.IsRecentlyUnlocked)
                    .Take(10)
                    .ToList();

                // Achievements cerca de completarse
                var nearCompletion = inProgressAchievements
                    .Where(a => a.ProgressPercentage >= 75)
                    .Take(5)
                    .ToList();

                // Títulos desbloqueados
                var unlockedTitles = unlockedAchievements
                    .Where(a => !string.IsNullOrEmpty(a.TitleReward))
                    .Select(a => a.TitleReward!)
                    .ToList();

                return new HunterAchievementsDto
                {
                    HunterID = hunterId,
                    HunterName = hunter.HunterName,
                    UnlockedAchievements = unlockedAchievements,
                    InProgressAchievements = inProgressAchievements,
                    AvailableAchievements = availableAchievements,
                    HiddenAchievements = hiddenAchievements,
                    AchievementsByCategory = achievementsByCategory,
                    TotalAchievements = achievementDtos.Count,
                    UnlockedCount = unlockedAchievements.Count,
                    InProgressCount = inProgressAchievements.Count,
                    CompletionPercentage = achievementDtos.Count > 0 ? (decimal)unlockedAchievements.Count / achievementDtos.Count * 100 : 0,
                    TotalXPFromAchievements = unlockedAchievements.Sum(a => a.XPReward),
                    RecentlyUnlocked = recentlyUnlocked,
                    NearCompletion = nearCompletion,
                    UnlockedTitles = unlockedTitles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting hunter achievements: {HunterID}", hunterId);
                throw;
            }
        }

        public async Task<List<AchievementDto>> GetAvailableAchievementsAsync(Guid hunterId)
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null) return new List<AchievementDto>();

                var achievements = await _context.Achievements
                    .Where(a => a.IsActive && !a.IsHidden)
                    .OrderBy(a => a.Category)
                    .ThenBy(a => a.AchievementName)
                    .ToListAsync();

                var hunterAchievements = await _context.HunterAchievements
                    .Where(ha => ha.HunterID == hunterId)
                    .ToDictionaryAsync(ha => ha.AchievementID);

                var achievementDtos = new List<AchievementDto>();

                foreach (var achievement in achievements)
                {
                    hunterAchievements.TryGetValue(achievement.AchievementID, out var hunterAchievement);
                    var dto = ConvertToAchievementDto(achievement, hunterAchievement, hunter);
                    achievementDtos.Add(dto);
                }

                return achievementDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting available achievements for hunter: {HunterID}", hunterId);
                return new List<AchievementDto>();
            }
        }

        public async Task<List<AchievementDto>> CheckAndUpdateAchievementsAsync(Guid hunterId, string eventType, int incrementValue = 1)
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null) return new List<AchievementDto>();

                var newlyUnlockedAchievements = new List<AchievementDto>();

                // Obtener achievements relevantes al evento
                var relevantAchievements = await GetRelevantAchievementsAsync(eventType);

                foreach (var achievement in relevantAchievements)
                {
                    var hunterAchievement = await _context.HunterAchievements
                        .FirstOrDefaultAsync(ha => ha.HunterID == hunterId && ha.AchievementID == achievement.AchievementID);

                    if (hunterAchievement == null)
                    {
                        // Crear nueva entrada si no existe
                        hunterAchievement = new HunterAchievement
                        {
                            HunterID = hunterId,
                            AchievementID = achievement.AchievementID,
                            CurrentProgress = 0
                        };
                        _context.HunterAchievements.Add(hunterAchievement);
                    }

                    if (hunterAchievement.IsUnlocked) continue;

                    var oldProgress = hunterAchievement.CurrentProgress;

                    // Actualizar progreso basado en el tipo de achievement
                    switch (achievement.AchievementType)
                    {
                        case "Counter":
                            hunterAchievement.IncrementProgress(incrementValue);
                            break;

                        case "Streak":
                            // Para streaks, el valor actual debería venir del hunter
                            if (eventType == "workout_completed")
                            {
                                hunterAchievement.UpdateProgress(hunter.DailyStreak);
                            }
                            break;

                        case "Single":
                            // Achievements únicos se desbloquean inmediatamente
                            hunterAchievement.UnlockAchievement();
                            break;

                        case "Progressive":
                            hunterAchievement.IncrementProgress(incrementValue);
                            break;
                    }

                    // Verificar si se desbloqueó
                    if (hunterAchievement.IsUnlocked && oldProgress < (achievement.TargetValue ?? 1))
                    {
                        // Achievement recién desbloqueado - agregar XP usando método interno
                        await AddXPToHunterAsync(hunterId, achievement.XPReward, $"Achievement: {achievement.AchievementName}");
                        
                        var dto = ConvertToAchievementDto(achievement, hunterAchievement, hunter);
                        newlyUnlockedAchievements.Add(dto);

                        _logger.LogInformation("🏆 Achievement unlocked: {AchievementName} by Hunter {HunterID} - XP: {XP}", 
                            achievement.AchievementName, hunterId, achievement.XPReward);
                    }
                }

                await _context.SaveChangesAsync();
                return newlyUnlockedAchievements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error checking achievements for hunter: {HunterID}, Event: {EventType}", 
                    hunterId, eventType);
                return new List<AchievementDto>();
            }
        }

        public async Task<bool> UnlockAchievementAsync(Guid hunterId, Guid achievementId)
        {
            try
            {
                var achievement = await _context.Achievements.FirstOrDefaultAsync(a => a.AchievementID == achievementId && a.IsActive);
                if (achievement == null) return false;

                var hunterAchievement = await _context.HunterAchievements
                    .FirstOrDefaultAsync(ha => ha.HunterID == hunterId && ha.AchievementID == achievementId);

                if (hunterAchievement == null)
                {
                    hunterAchievement = new HunterAchievement
                    {
                        HunterID = hunterId,
                        AchievementID = achievementId
                    };
                    _context.HunterAchievements.Add(hunterAchievement);
                }

                if (hunterAchievement.IsUnlocked) return true; // Ya desbloqueado

                hunterAchievement.UnlockAchievement();
                
                // Otorgar XP al hunter usando método interno
                await AddXPToHunterAsync(hunterId, achievement.XPReward, $"Achievement: {achievement.AchievementName}");

                await _context.SaveChangesAsync();

                _logger.LogInformation("🏆 Achievement manually unlocked: {AchievementName} for Hunter {HunterID}", 
                    achievement.AchievementName, hunterId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error unlocking achievement: {AchievementID} for Hunter {HunterID}", 
                    achievementId, hunterId);
                return false;
            }
        }

        public async Task<List<AchievementDto>> GetAchievementsByCategoryAsync(string category, Guid hunterId)
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null) return new List<AchievementDto>();

                var achievements = await _context.Achievements
                    .Where(a => a.IsActive && a.Category == category)
                    .OrderBy(a => a.AchievementName)
                    .ToListAsync();

                var hunterAchievements = await _context.HunterAchievements
                    .Where(ha => ha.HunterID == hunterId)
                    .ToDictionaryAsync(ha => ha.AchievementID);

                var achievementDtos = new List<AchievementDto>();

                foreach (var achievement in achievements)
                {
                    hunterAchievements.TryGetValue(achievement.AchievementID, out var hunterAchievement);
                    var dto = ConvertToAchievementDto(achievement, hunterAchievement, hunter);
                    achievementDtos.Add(dto);
                }

                return achievementDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting achievements by category: {Category} for Hunter {HunterID}", 
                    category, hunterId);
                return new List<AchievementDto>();
            }
        }

        public async Task<Dictionary<string, object>> GetAchievementStatsAsync(Guid hunterId)
        {
            try
            {
                var hunterAchievements = await GetHunterAchievementsAsync(hunterId);

                var categoryStats = hunterAchievements.AchievementsByCategory
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => new
                        {
                            Total = kvp.Value.Count,
                            Unlocked = kvp.Value.Count(a => a.IsUnlocked),
                            InProgress = kvp.Value.Count(a => !a.IsUnlocked && a.CurrentProgress > 0),
                            CompletionRate = kvp.Value.Count > 0 ? (decimal)kvp.Value.Count(a => a.IsUnlocked) / kvp.Value.Count * 100 : 0
                        }
                    );

                return new Dictionary<string, object>
                {
                    {"TotalAchievements", hunterAchievements.TotalAchievements},
                    {"UnlockedCount", hunterAchievements.UnlockedCount},
                    {"InProgressCount", hunterAchievements.InProgressCount},
                    {"CompletionPercentage", hunterAchievements.CompletionPercentage},
                    {"TotalXPFromAchievements", hunterAchievements.TotalXPFromAchievements},
                    {"RecentUnlocksCount", hunterAchievements.RecentlyUnlocked.Count},
                    {"NearCompletionCount", hunterAchievements.NearCompletion.Count},
                    {"UnlockedTitlesCount", hunterAchievements.UnlockedTitles.Count},
                    {"CategoryStats", categoryStats},
                    {"AverageProgressPerIncomplete", CalculateAverageProgress(hunterAchievements.InProgressAchievements)},
                    {"MostRecentUnlock", hunterAchievements.RecentlyUnlocked.FirstOrDefault()?.AchievementName ?? "None"}
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting achievement stats for hunter: {HunterID}", hunterId);
                return new Dictionary<string, object>();
            }
        }

        // Helper methods - internos para evitar dependencia circular
        private async Task<bool> AddXPToHunterAsync(Guid hunterId, int xpAmount, string source)
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null) return false;

                hunter.CurrentXP += xpAmount;
                hunter.TotalXP += xpAmount;

                // Verificar level up usando método del modelo
                hunter.ProcessLevelUp();

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

        private async Task<List<Achievement>> GetRelevantAchievementsAsync(string eventType)
        {
            return eventType.ToLower() switch
            {
                "quest_completed" or "workout_completed" => await _context.Achievements
                    .Where(a => a.IsActive && (a.Category == "Consistency" || a.Category == "Milestone"))
                    .ToListAsync(),
                "strength_exercise_completed" => await _context.Achievements
                    .Where(a => a.IsActive && a.Category == "Strength")
                    .ToListAsync(),
                "cardio_exercise_completed" or "endurance_exercise_completed" => await _context.Achievements
                    .Where(a => a.IsActive && a.Category == "Endurance")
                    .ToListAsync(),
                "dungeon_completed" => await _context.Achievements
                    .Where(a => a.IsActive && (a.Category == "Milestone" || a.Category == "Special"))
                    .ToListAsync(),
                "level_up" => await _context.Achievements
                    .Where(a => a.IsActive && a.Category == "Milestone")
                    .ToListAsync(),
                "streak_milestone" => await _context.Achievements
                    .Where(a => a.IsActive && a.Category == "Consistency" && a.AchievementType == "Streak")
                    .ToListAsync(),
                _ => await _context.Achievements
                    .Where(a => a.IsActive && a.Category == "Milestone")
                    .ToListAsync()
            };
        }

        private static AchievementDto ConvertToAchievementDto(Achievement achievement, HunterAchievement? hunterAchievement, Hunter hunter)
        {
            var currentProgress = hunterAchievement?.CurrentProgress ?? 0;
            var isUnlocked = hunterAchievement?.IsUnlocked ?? false;
            var unlockedAt = hunterAchievement?.UnlockedAt;
            var progressPercentage = hunterAchievement?.GetProgressPercentage() ?? 0;
            var remainingProgress = hunterAchievement?.GetRemainingProgress() ?? (achievement.TargetValue ?? 0);

            return new AchievementDto
            {
                AchievementID = achievement.AchievementID,
                AchievementName = achievement.AchievementName,
                Description = achievement.Description,
                Category = achievement.Category,
                CategoryDisplayName = achievement.GetCategoryDisplayName(),
                CategoryDescription = achievement.GetCategoryDescription(),
                CategoryIcon = GetCategoryIcon(achievement.Category),
                TargetValue = achievement.TargetValue,
                AchievementType = achievement.AchievementType,
                TypeDescription = achievement.GetTypeDescription(),
                IsHidden = achievement.IsHidden,
                XPReward = achievement.XPReward,
                TitleReward = achievement.TitleReward,
                DifficultyLevel = achievement.GetDifficultyLevel(),
                RarityColor = achievement.GetRarityColor(),
                EstimatedDaysToComplete = achievement.GetEstimatedDaysToComplete(hunter),
                ProgressHint = achievement.GetProgressHint(),
                CurrentProgress = currentProgress,
                IsUnlocked = isUnlocked,
                UnlockedAt = unlockedAt,
                ProgressPercentage = progressPercentage,
                RemainingProgress = remainingProgress,
                ProgressDescription = hunterAchievement?.GetProgressDescription() ?? "Not started",
                UnlockStatusText = hunterAchievement?.GetUnlockStatusText() ?? "Not started",
                IsRecentlyUnlocked = hunterAchievement?.IsRecentlyUnlocked() ?? false,
                CanBeUnlocked = hunterAchievement?.CanBeUnlocked() ?? false,
                IconUrl = achievement.IconUrl,
                RequiresSpecialConditions = achievement.RequiresSpecialConditions(),
                RelatedAchievements = achievement.GetRelatedAchievements(),
                CompletionCelebrationMessage = achievement.GetCompletionCelebrationMessage()
            };
        }

        private static string GetCategoryIcon(string category)
        {
            return category switch
            {
                "Consistency" => "🔥",
                "Strength" => "💪",
                "Endurance" => "🏃‍♂️",
                "Social" => "👥",
                "Special" => "⭐",
                "Milestone" => "🎯",
                _ => "🏆"
            };
        }

        private static decimal CalculateAverageProgress(List<AchievementDto> inProgressAchievements)
        {
            if (!inProgressAchievements.Any()) return 0;
            
            return inProgressAchievements.Average(a => a.ProgressPercentage);
        }
    }
}