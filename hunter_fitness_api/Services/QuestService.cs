using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HunterFitness.API.Data;
using HunterFitness.API.Models;
using HunterFitness.API.DTOs;

namespace HunterFitness.API.Services
{
    public interface IQuestService
    {
        Task<DailyQuestsSummaryDto> GetDailyQuestsAsync(Guid hunterId, DateTime? questDate = null);
        Task<QuestOperationResponseDto> StartQuestAsync(Guid hunterId, StartQuestDto startDto);
        Task<QuestOperationResponseDto> UpdateQuestProgressAsync(Guid hunterId, UpdateQuestProgressDto updateDto);
        Task<QuestOperationResponseDto> CompleteQuestAsync(Guid hunterId, CompleteQuestDto completeDto);
        Task<List<QuestHistoryDto>> GetQuestHistoryAsync(Guid hunterId, int limit = 50);
        Task<QuestStatsDto> GetQuestStatsAsync(Guid hunterId);
        Task<bool> GenerateDailyQuestsAsync(Guid hunterId, DateTime questDate, int questCount = 3);
        Task<List<AvailableQuestDto>> GetAvailableQuestsAsync(Guid hunterId);
    }

    public class QuestService : IQuestService
    {
        private readonly HunterFitnessDbContext _context;
        private readonly ILogger<QuestService> _logger;
        private readonly IHunterService _hunterService;

        public QuestService(
            HunterFitnessDbContext context, 
            ILogger<QuestService> logger,
            IHunterService hunterService)
        {
            _context = context;
            _logger = logger;
            _hunterService = hunterService;
        }

        public async Task<DailyQuestsSummaryDto> GetDailyQuestsAsync(Guid hunterId, DateTime? questDate = null)
        {
            try
            {
                var targetDate = questDate?.Date ?? DateTime.UtcNow.Date;

                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null)
                {
                    throw new ArgumentException("Hunter not found");
                }

                // Obtener quests del día
                var hunterQuests = await _context.HunterDailyQuests
                    .Include(hq => hq.Quest)
                    .Where(hq => hq.HunterID == hunterId && hq.QuestDate == targetDate)
                    .OrderBy(hq => hq.AssignedAt)
                    .ToListAsync();

                // Si no hay quests para hoy, generar automáticamente
                if (!hunterQuests.Any() && targetDate == DateTime.UtcNow.Date)
                {
                    await GenerateDailyQuestsAsync(hunterId, targetDate);
                    hunterQuests = await _context.HunterDailyQuests
                        .Include(hq => hq.Quest)
                        .Where(hq => hq.HunterID == hunterId && hq.QuestDate == targetDate)
                        .OrderBy(hq => hq.AssignedAt)
                        .ToListAsync();
                }

                var questDtos = hunterQuests.Select(hq => ConvertToHunterDailyQuestDto(hq)).ToList();

                var summary = new DailyQuestsSummaryDto
                {
                    QuestDate = targetDate,
                    TotalQuests = questDtos.Count,
                    CompletedQuests = questDtos.Count(q => q.IsCompleted),
                    InProgressQuests = questDtos.Count(q => q.Status == "InProgress"),
                    PendingQuests = questDtos.Count(q => q.Status == "Assigned"),
                    OverallProgress = questDtos.Any() ? questDtos.Average(q => q.Progress) : 0,
                    TotalXPEarned = questDtos.Sum(q => q.XPEarned),
                    TotalXPAvailable = questDtos.Sum(q => q.ScaledXPReward),
                    Quests = questDtos,
                    CanGenerateNewQuests = targetDate == DateTime.UtcNow.Date && !questDtos.Any(),
                    ProgressMessage = GetProgressMessage(questDtos),
                    MotivationalMessage = GetMotivationalMessage(questDtos)
                };

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting daily quests for hunter: {HunterID}", hunterId);
                throw;
            }
        }

        public async Task<QuestOperationResponseDto> StartQuestAsync(Guid hunterId, StartQuestDto startDto)
        {
            try
            {
                var hunterQuest = await _context.HunterDailyQuests
                    .Include(hq => hq.Quest)
                    .Include(hq => hq.Hunter)
                    .FirstOrDefaultAsync(hq => hq.AssignmentID == startDto.AssignmentID && hq.HunterID == hunterId);

                if (hunterQuest == null)
                {
                    return new QuestOperationResponseDto
                    {
                        Success = false,
                        Message = "Quest assignment not found."
                    };
                }

                if (hunterQuest.Status != "Assigned")
                {
                    return new QuestOperationResponseDto
                    {
                        Success = false,
                        Message = "Quest has already been started or completed."
                    };
                }

                hunterQuest.StartQuest();
                await _context.SaveChangesAsync();

                _logger.LogInformation("🎯 Quest started: {QuestName} by Hunter {HunterID}", 
                    hunterQuest.Quest.QuestName, hunterId);

                return new QuestOperationResponseDto
                {
                    Success = true,
                    Message = $"Quest '{hunterQuest.Quest.QuestName}' started! Let's do this! 💪",
                    Quest = ConvertToHunterDailyQuestDto(hunterQuest)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error starting quest: {AssignmentID}", startDto.AssignmentID);
                throw;
            }
        }

        public async Task<QuestOperationResponseDto> UpdateQuestProgressAsync(Guid hunterId, UpdateQuestProgressDto updateDto)
        {
            try
            {
                var hunterQuest = await _context.HunterDailyQuests
                    .Include(hq => hq.Quest)
                    .Include(hq => hq.Hunter)
                    .FirstOrDefaultAsync(hq => hq.AssignmentID == updateDto.AssignmentID && hq.HunterID == hunterId);

                if (hunterQuest == null)
                {
                    return new QuestOperationResponseDto
                    {
                        Success = false,
                        Message = "Quest assignment not found."
                    };
                }

                if (hunterQuest.Status == "Completed")
                {
                    return new QuestOperationResponseDto
                    {
                        Success = false,
                        Message = "Quest is already completed."
                    };
                }

                // Actualizar progreso
                if (updateDto.CurrentReps.HasValue)
                    hunterQuest.CurrentReps = Math.Max(hunterQuest.CurrentReps, updateDto.CurrentReps.Value);

                if (updateDto.CurrentSets.HasValue)
                    hunterQuest.CurrentSets = Math.Max(hunterQuest.CurrentSets, updateDto.CurrentSets.Value);

                if (updateDto.CurrentDuration.HasValue)
                    hunterQuest.CurrentDuration = Math.Max(hunterQuest.CurrentDuration, updateDto.CurrentDuration.Value);

                if (updateDto.CurrentDistance.HasValue)
                    hunterQuest.CurrentDistance = Math.Max(hunterQuest.CurrentDistance, updateDto.CurrentDistance.Value);

                // Si el status era "Assigned", cambiarlo a "InProgress"
                if (hunterQuest.Status == "Assigned")
                {
                    hunterQuest.StartQuest();
                }

                hunterQuest.UpdateProgress();
                await _context.SaveChangesAsync();

                _logger.LogInformation("📈 Quest progress updated: {Progress}% for {QuestName}", 
                    hunterQuest.Progress, hunterQuest.Quest.QuestName);

                return new QuestOperationResponseDto
                {
                    Success = true,
                    Message = hunterQuest.Status == "Completed" 
                        ? "🎉 Quest completed! Amazing work!" 
                        : $"Progress updated: {hunterQuest.Progress:F1}% complete!",
                    Quest = ConvertToHunterDailyQuestDto(hunterQuest)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error updating quest progress: {AssignmentID}", updateDto.AssignmentID);
                throw;
            }
        }

        public async Task<QuestOperationResponseDto> CompleteQuestAsync(Guid hunterId, CompleteQuestDto completeDto)
        {
            try
            {
                var hunterQuest = await _context.HunterDailyQuests
                    .Include(hq => hq.Quest)
                    .Include(hq => hq.Hunter)
                    .FirstOrDefaultAsync(hq => hq.AssignmentID == completeDto.AssignmentID && hq.HunterID == hunterId);

                if (hunterQuest == null)
                {
                    return new QuestOperationResponseDto
                    {
                        Success = false,
                        Message = "Quest assignment not found."
                    };
                }

                if (hunterQuest.Status == "Completed")
                {
                    return new QuestOperationResponseDto
                    {
                        Success = false,
                        Message = "Quest is already completed."
                    };
                }

                // Actualizar valores finales
                if (completeDto.FinalReps.HasValue)
                    hunterQuest.CurrentReps = completeDto.FinalReps.Value;

                if (completeDto.FinalSets.HasValue)
                    hunterQuest.CurrentSets = completeDto.FinalSets.Value;

                if (completeDto.FinalDuration.HasValue)
                    hunterQuest.CurrentDuration = completeDto.FinalDuration.Value;

                if (completeDto.FinalDistance.HasValue)
                    hunterQuest.CurrentDistance = completeDto.FinalDistance.Value;

                // Calcular bonus multiplier
                if (completeDto.PerfectExecution)
                {
                    hunterQuest.BonusMultiplier = hunterQuest.GetBonusMultiplierForCompletion();
                }

                hunterQuest.CompleteQuest();

                // Agregar al historial
                var questHistory = new QuestHistory
                {
                    HunterID = hunterId,
                    QuestID = hunterQuest.QuestID,
                    CompletedAt = hunterQuest.CompletedAt ?? DateTime.UtcNow,
                    XPEarned = hunterQuest.XPEarned,
                    CompletionTime = hunterQuest.GetCompletionTime()?.TotalSeconds is double seconds ? (int)seconds : null,
                    PerfectExecution = completeDto.PerfectExecution,
                    BonusMultiplier = hunterQuest.BonusMultiplier,
                    FinalReps = completeDto.FinalReps,
                    FinalSets = completeDto.FinalSets,
                    FinalDuration = completeDto.FinalDuration,
                    FinalDistance = completeDto.FinalDistance
                };

                _context.QuestHistory.Add(questHistory);

                // Agregar XP al hunter
                await _hunterService.AddXPAsync(hunterId, hunterQuest.XPEarned, $"Quest: {hunterQuest.Quest.QuestName}");

                // Incrementar contador de workouts
                await _hunterService.IncrementWorkoutCountAsync(hunterId);

                // Actualizar streak (simplificado - en producción sería más complejo)
                await _hunterService.UpdateStreakAsync(hunterId, true);

                await _context.SaveChangesAsync();

                _logger.LogInformation("🎉 Quest completed: {QuestName} by Hunter {HunterID} - XP: {XP}", 
                    hunterQuest.Quest.QuestName, hunterId, hunterQuest.XPEarned);

                return new QuestOperationResponseDto
                {
                    Success = true,
                    Message = $"🎉 {hunterQuest.Quest.QuestName} completed! +{hunterQuest.XPEarned} XP earned!",
                    Quest = ConvertToHunterDailyQuestDto(hunterQuest),
                    XPEarned = hunterQuest.XPEarned,
                    LeveledUp = false, // TODO: Implementar detección de level up
                    AchievementsUnlocked = new List<string>() // TODO: Implementar detección de achievements
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error completing quest: {AssignmentID}", completeDto.AssignmentID);
                throw;
            }
        }

        public async Task<List<QuestHistoryDto>> GetQuestHistoryAsync(Guid hunterId, int limit = 50)
        {
            try
            {
                var history = await _context.QuestHistory
                    .Include(qh => qh.Quest)
                    .Where(qh => qh.HunterID == hunterId)
                    .OrderByDescending(qh => qh.CompletedAt)
                    .Take(limit)
                    .Select(qh => new QuestHistoryDto
                    {
                        HistoryID = qh.HistoryID,
                        QuestID = qh.QuestID,
                        QuestName = qh.Quest.QuestName,
                        QuestType = qh.Quest.QuestType,
                        ExerciseName = qh.Quest.ExerciseName,
                        Difficulty = qh.Quest.Difficulty,
                        DifficultyColor = qh.Quest.GetDifficultyColor(),
                        CompletedAt = qh.CompletedAt,
                        XPEarned = qh.XPEarned,
                        CompletionTime = qh.GetCompletionTimeFormatted(),
                        PerfectExecution = qh.PerfectExecution,
                        BonusMultiplier = qh.BonusMultiplier,
                        PerformanceRating = qh.GetPerformanceRating(),
                        PerformanceColor = qh.GetPerformanceColor(),
                        FinalReps = qh.FinalReps,
                        FinalSets = qh.FinalSets,
                        FinalDuration = qh.FinalDuration,
                        FinalDistance = qh.FinalDistance,
                        StatsDescription = qh.GetStatsDescription(),
                        RelativeTime = qh.GetRelativeTimeDescription(),
                        IsFromToday = qh.IsFromToday(),
                        IsFromThisWeek = qh.IsFromThisWeek(),
                        IsPersonalBest = false, // TODO: Implementar lógica de personal best
                        WasFasterThanEstimated = qh.WasFasterThanEstimated()
                    })
                    .ToListAsync();

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting quest history for hunter: {HunterID}", hunterId);
                throw;
            }
        }

        public async Task<QuestStatsDto> GetQuestStatsAsync(Guid hunterId)
        {
            try
            {
                var totalCompleted = await _context.QuestHistory
                    .Where(qh => qh.HunterID == hunterId)
                    .CountAsync();

                var totalXP = await _context.QuestHistory
                    .Where(qh => qh.HunterID == hunterId)
                    .SumAsync(qh => qh.XPEarned);

                var todayCount = await _context.QuestHistory
                    .Where(qh => qh.HunterID == hunterId && qh.CompletedAt.Date == DateTime.UtcNow.Date)
                    .CountAsync();

                var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
                var weeklyCount = await _context.QuestHistory
                    .Where(qh => qh.HunterID == hunterId && qh.CompletedAt >= startOfWeek)
                    .CountAsync();

                var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var monthlyCount = await _context.QuestHistory
                    .Where(qh => qh.HunterID == hunterId && qh.CompletedAt >= startOfMonth)
                    .CountAsync();

                return new QuestStatsDto
                {
                    HunterID = hunterId,
                    TotalQuestsCompleted = totalCompleted,
                    TotalXPFromQuests = totalXP,
                    CurrentStreak = 0, // TODO: Implementar cálculo de streak
                    LongestStreak = 0, // TODO: Implementar cálculo de streak
                    QuestsCompletedToday = todayCount,
                    QuestsCompletedThisWeek = weeklyCount,
                    QuestsCompletedThisMonth = monthlyCount,
                    QuestsByType = new Dictionary<string, int>(),
                    XPByType = new Dictionary<string, int>(),
                    AverageTimeByType = new Dictionary<string, double>(),
                    QuestsByDifficulty = new Dictionary<string, int>(),
                    AveragePerformanceByDifficulty = new Dictionary<string, decimal>(),
                    PersonalBests = new List<PersonalBestDto>(),
                    WeeklyTrends = new List<QuestTrendDto>(),
                    ProgressTrend = 0.0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting quest stats for hunter: {HunterID}", hunterId);
                throw;
            }
        }

        public async Task<bool> GenerateDailyQuestsAsync(Guid hunterId, DateTime questDate, int questCount = 3)
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null) return false;

                // Verificar que no existan quests para esta fecha
                var existingQuests = await _context.HunterDailyQuests
                    .Where(hq => hq.HunterID == hunterId && hq.QuestDate == questDate.Date)
                    .AnyAsync();

                if (existingQuests) return true; // Ya existen quests para este día

                // Obtener quests disponibles para el nivel del hunter
                var availableQuests = await _context.DailyQuests
                    .Where(q => q.IsActive && q.MinLevel <= hunter.Level)
                    .ToListAsync();

                if (!availableQuests.Any()) return false;

                // Seleccionar quests variados
                var selectedQuests = SelectVariedQuests(availableQuests, questCount, hunter);

                // Crear asignaciones
                foreach (var quest in selectedQuests)
                {
                    var assignment = new HunterDailyQuest
                    {
                        HunterID = hunterId,
                        QuestID = quest.QuestID,
                        QuestDate = questDate.Date,
                        AssignedAt = DateTime.UtcNow
                    };

                    _context.HunterDailyQuests.Add(assignment);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("📋 Generated {Count} daily quests for Hunter {HunterID} on {Date}", 
                    selectedQuests.Count, hunterId, questDate.Date);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error generating daily quests for hunter: {HunterID}", hunterId);
                return false;
            }
        }

        public async Task<List<AvailableQuestDto>> GetAvailableQuestsAsync(Guid hunterId)
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null) return new List<AvailableQuestDto>();

                var availableQuests = await _context.DailyQuests
                    .Where(q => q.IsActive)
                    .Select(q => new AvailableQuestDto
                    {
                        QuestID = q.QuestID,
                        QuestName = q.QuestName,
                        Description = q.Description,
                        QuestType = q.QuestType,
                        QuestTypeIcon = q.GetQuestTypeIcon(),
                        ExerciseName = q.ExerciseName,
                        Difficulty = q.Difficulty,
                        DifficultyColor = q.GetDifficultyColor(),
                        TargetReps = q.TargetReps,
                        TargetSets = q.TargetSets,
                        TargetDuration = q.TargetDuration,
                        TargetDistance = q.TargetDistance,
                        BaseXPReward = q.BaseXPReward,
                        ScaledXPReward = q.GetScaledXPReward(hunter),
                        StrengthBonus = q.StrengthBonus,
                        AgilityBonus = q.AgilityBonus,
                        VitalityBonus = q.VitalityBonus,
                        EnduranceBonus = q.EnduranceBonus,
                        MinLevel = q.MinLevel,
                        MinRank = q.MinRank,
                        IsEligible = q.IsValidForHunter(hunter),
                        IneligibilityReason = !q.IsValidForHunter(hunter) 
                            ? $"Requires Level {q.MinLevel} and {q.MinRank} Rank"
                            : null,
                        EstimatedTimeMinutes = q.GetEstimatedTimeMinutes(),
                        EstimatedTimeText = $"~{q.GetEstimatedTimeMinutes()} min"
                    })
                    .ToListAsync();

                return availableQuests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting available quests for hunter: {HunterID}", hunterId);
                return new List<AvailableQuestDto>();
            }
        }

        // Helper methods
        private List<DailyQuest> SelectVariedQuests(List<DailyQuest> availableQuests, int count, Hunter hunter)
        {
            var selected = new List<DailyQuest>();
            var random = new Random();

            // Intentar seleccionar quests de diferentes tipos
            var questTypes = new[] { "Cardio", "Strength", "Flexibility", "Endurance", "Mixed" };
            var usedTypes = new HashSet<string>();

            foreach (var type in questTypes.OrderBy(t => random.Next()))
            {
                if (selected.Count >= count) break;

                var typeQuests = availableQuests
                    .Where(q => q.QuestType == type && q.IsValidForHunter(hunter))
                    .ToList();

                if (typeQuests.Any())
                {
                    var selectedQuest = typeQuests[random.Next(typeQuests.Count)];
                    selected.Add(selectedQuest);
                    usedTypes.Add(type);
                }
            }

            // Si no tenemos suficientes, agregar quests aleatorias
            while (selected.Count < count)
            {
                var remainingQuests = availableQuests
                    .Where(q => !selected.Contains(q) && q.IsValidForHunter(hunter))
                    .ToList();

                if (!remainingQuests.Any()) break;

                var randomQuest = remainingQuests[random.Next(remainingQuests.Count)];
                selected.Add(randomQuest);
            }

            return selected;
        }

        private HunterDailyQuestDto ConvertToHunterDailyQuestDto(HunterDailyQuest hunterQuest)
        {
            return new HunterDailyQuestDto
            {
                AssignmentID = hunterQuest.AssignmentID,
                QuestID = hunterQuest.QuestID,
                QuestName = hunterQuest.Quest.QuestName,
                Description = hunterQuest.Quest.Description,
                QuestType = hunterQuest.Quest.QuestType,
                QuestTypeIcon = hunterQuest.Quest.GetQuestTypeIcon(),
                ExerciseName = hunterQuest.Quest.ExerciseName,
                TargetReps = hunterQuest.Quest.TargetReps,
                TargetSets = hunterQuest.Quest.TargetSets,
                TargetDuration = hunterQuest.Quest.TargetDuration,
                TargetDistance = hunterQuest.Quest.TargetDistance,
                TargetDescription = GetTargetDescription(hunterQuest.Quest),
                CurrentReps = hunterQuest.CurrentReps,
                CurrentSets = hunterQuest.CurrentSets,
                CurrentDuration = hunterQuest.CurrentDuration,
                CurrentDistance = hunterQuest.CurrentDistance,
                ProgressDescription = hunterQuest.GetProgressDescription(),
                Status = hunterQuest.Status,
                Progress = hunterQuest.Progress,
                CanComplete = hunterQuest.CanComplete(),
                IsCompleted = hunterQuest.Status == "Completed",
                Difficulty = hunterQuest.Quest.Difficulty,
                DifficultyColor = hunterQuest.Quest.GetDifficultyColor(),
                BaseXPReward = hunterQuest.Quest.BaseXPReward,
                ScaledXPReward = hunterQuest.Quest.GetScaledXPReward(hunterQuest.Hunter),
                XPEarned = hunterQuest.XPEarned,
                BonusMultiplier = hunterQuest.BonusMultiplier,
                StrengthBonus = hunterQuest.Quest.StrengthBonus,
                AgilityBonus = hunterQuest.Quest.AgilityBonus,
                VitalityBonus = hunterQuest.Quest.VitalityBonus,
                EnduranceBonus = hunterQuest.Quest.EnduranceBonus,
                AssignedAt = hunterQuest.AssignedAt,
                StartedAt = hunterQuest.StartedAt,
                CompletedAt = hunterQuest.CompletedAt,
                CompletionTime = hunterQuest.GetCompletionTime()?.ToString(@"mm\:ss"),
                EstimatedTimeMinutes = hunterQuest.Quest.GetEstimatedTimeMinutes(),
                QuestDate = hunterQuest.QuestDate,
                RelativeTime = GetRelativeTime(hunterQuest.AssignedAt),
                IsFromToday = hunterQuest.QuestDate == DateTime.UtcNow.Date
            };
        }

        private string GetTargetDescription(DailyQuest quest)
        {
            var targets = new List<string>();

            if (quest.TargetReps.HasValue)
                targets.Add($"{quest.TargetReps} reps");

            if (quest.TargetSets.HasValue)
                targets.Add($"{quest.TargetSets} sets");

            if (quest.TargetDuration.HasValue)
            {
                var duration = TimeSpan.FromSeconds(quest.TargetDuration.Value);
                if (duration.TotalMinutes >= 1)
                    targets.Add($"{duration.Minutes}m {duration.Seconds}s");
                else
                    targets.Add($"{duration.Seconds}s");
            }

            if (quest.TargetDistance.HasValue)
                targets.Add($"{quest.TargetDistance:F1}m");

            return targets.Any() ? string.Join(", ", targets) : "Complete exercise";
        }

        private string GetProgressMessage(List<HunterDailyQuestDto> quests)
        {
            if (!quests.Any()) return "No quests assigned today.";

            var completed = quests.Count(q => q.IsCompleted);
            var total = quests.Count;

            return completed switch
            {
                0 when total > 0 => "Ready to start your daily challenges! 🏹",
                var c when c == total => "🎉 All quests completed! You're unstoppable today!",
                var c when c > total / 2 => $"Great progress! {c}/{total} quests completed! 💪",
                _ => $"Keep going! {completed}/{total} quests completed so far! 🔥"
            };
        }

        private string GetMotivationalMessage(List<HunterDailyQuestDto> quests)
        {
            var random = new Random();
            var messages = new[]
            {
                "Every Shadow needs training to become a Monarch! 👑",
                "The System has prepared your daily trials! ⚔️",
                "Level up your real-world stats, Hunter! 💪",
                "Your shadow army grows stronger with each workout! 🌟",
                "Face these challenges like Jin-Woo faced the dungeons! 🏹"
            };

            return messages[random.Next(messages.Length)];
        }

        private string GetRelativeTime(DateTime dateTime)
        {
            var timeSince = DateTime.UtcNow - dateTime;

            if (timeSince.TotalMinutes < 1)
                return "Just now";
            else if (timeSince.TotalHours < 1)
                return $"{(int)timeSince.TotalMinutes} minutes ago";
            else if (timeSince.TotalDays < 1)
                return $"{(int)timeSince.TotalHours} hours ago";
            else
                return dateTime.ToString("MMM dd");
        }
    }
}