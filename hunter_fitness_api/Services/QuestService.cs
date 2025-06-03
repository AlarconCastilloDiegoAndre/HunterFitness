// hunter_fitness_api/Services/QuestService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HunterFitness.API.Data;
using HunterFitness.API.Models;
using HunterFitness.API.DTOs;
// using System; // Para System.Diagnostics.Debug.WriteLine si es necesario para debug rápido

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
        private readonly IHunterService _hunterService; // Inyectar IHunterService

        public QuestService(
            HunterFitnessDbContext context,
            ILogger<QuestService> logger,
            IHunterService hunterService) // Modificar constructor
        {
            _context = context;
            _logger = logger;
            _hunterService = hunterService; // Asignar servicio inyectado
        }

        public async Task<DailyQuestsSummaryDto> GetDailyQuestsAsync(Guid hunterId, DateTime? questDate = null)
        {
            try
            {
                var targetDate = questDate?.Date ?? DateTime.UtcNow.Date;

                var hunter = await _context.Hunters.AsNoTracking().FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null)
                {
                    _logger.LogWarning("GetDailyQuestsAsync: Hunter not found with ID {HunterID}", hunterId);
                    throw new ArgumentException("Hunter not found");
                }

                var hunterQuests = await _context.HunterDailyQuests
                    .Include(hq => hq.Quest)
                    .Include(hq => hq.Hunter) // Hunter se incluye para que ConvertTo...Dto tenga acceso
                    .Where(hq => hq.HunterID == hunterId && hq.QuestDate == targetDate)
                    .OrderBy(hq => hq.AssignedAt)
                    .ToListAsync();

                if (!hunterQuests.Any() && targetDate == DateTime.UtcNow.Date)
                {
                    _logger.LogInformation("GetDailyQuestsAsync: No quests for Hunter {HunterID} on {TargetDate}. Attempting to generate.", hunterId, targetDate);
                    await GenerateDailyQuestsAsync(hunterId, targetDate); // GenerateDailyQuestsAsync ya hace SaveChanges
                    hunterQuests = await _context.HunterDailyQuests
                        .Include(hq => hq.Quest)
                        .Include(hq => hq.Hunter)
                        .Where(hq => hq.HunterID == hunterId && hq.QuestDate == targetDate)
                        .OrderBy(hq => hq.AssignedAt)
                        .ToListAsync();
                }

                var questDtos = hunterQuests.Select(hq => ConvertToHunterDailyQuestDto(hq, hunter)).ToList(); // Pasar el hunter explícitamente

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
                _logger.LogInformation("GetDailyQuestsAsync: Successfully retrieved {QuestCount} quests for Hunter {HunterID} on {TargetDate}.", questDtos.Count, hunterId, targetDate);
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error in GetDailyQuestsAsync for hunter: {HunterID}", hunterId);
                throw; // Re-lanzar para que la función de Azure lo maneje
            }
        }

        public async Task<QuestOperationResponseDto> StartQuestAsync(Guid hunterId, StartQuestDto startDto)
        {
             _logger.LogInformation("Attempting to start quest {AssignmentID} for hunter {HunterID}", startDto.AssignmentID, hunterId);
            try
            {
                var hunterQuest = await _context.HunterDailyQuests
                    .Include(hq => hq.Quest) // Para el nombre en el mensaje
                    .Include(hq => hq.Hunter) // Para ConvertToHunterDailyQuestDto
                    .FirstOrDefaultAsync(hq => hq.AssignmentID == startDto.AssignmentID && hq.HunterID == hunterId);

                if (hunterQuest == null)
                {
                     _logger.LogWarning("StartQuestAsync: Quest assignment {AssignmentID} not found for hunter {HunterID}", startDto.AssignmentID, hunterId);
                    return new QuestOperationResponseDto { Success = false, Message = "Quest assignment not found." };
                }
                 if (hunterQuest.Hunter == null) // Defensa adicional
                {
                    _logger.LogError("StartQuestAsync: CRITICAL - Hunter not loaded for AssignmentID {AssignmentID}", startDto.AssignmentID);
                    return new QuestOperationResponseDto { Success = false, Message = "Internal error: Hunter data missing."};
                }


                if (hunterQuest.Status != "Assigned")
                {
                    return new QuestOperationResponseDto { Success = false, Message = "Quest has already been started or completed." };
                }

                hunterQuest.StartQuest();
                await _context.SaveChangesAsync();

                _logger.LogInformation("🎯 Quest '{QuestName}' (ID: {AssignmentID}) started by Hunter {HunterID}",
                    hunterQuest.Quest?.QuestName ?? "Unknown", hunterQuest.AssignmentID, hunterId);

                return new QuestOperationResponseDto
                {
                    Success = true,
                    Message = $"Quest '{hunterQuest.Quest?.QuestName ?? "Selected Quest"}' started! Let's do this! 💪",
                    Quest = ConvertToHunterDailyQuestDto(hunterQuest, hunterQuest.Hunter)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error starting quest {AssignmentID} for hunter {HunterID}", startDto.AssignmentID, hunterId);
                return new QuestOperationResponseDto { Success = false, Message = "An error occurred while starting the quest."};
            }
        }

        public async Task<QuestOperationResponseDto> UpdateQuestProgressAsync(Guid hunterId, UpdateQuestProgressDto updateDto)
        {
            _logger.LogInformation("Attempting to update progress for quest {AssignmentID} for hunter {HunterID}", updateDto.AssignmentID, hunterId);
            try
            {
                var hunterQuest = await _context.HunterDailyQuests
                    .Include(hq => hq.Quest)
                    .Include(hq => hq.Hunter)
                    .FirstOrDefaultAsync(hq => hq.AssignmentID == updateDto.AssignmentID && hq.HunterID == hunterId);

                if (hunterQuest == null)
                {
                    _logger.LogWarning("UpdateQuestProgressAsync: Quest assignment {AssignmentID} not found for hunter {HunterID}", updateDto.AssignmentID, hunterId);
                    return new QuestOperationResponseDto { Success = false, Message = "Quest assignment not found." };
                }
                if (hunterQuest.Hunter == null || hunterQuest.Quest == null) // Defensa adicional
                {
                    _logger.LogError("UpdateQuestProgressAsync: CRITICAL - Hunter or Quest not loaded for AssignmentID {AssignmentID}", updateDto.AssignmentID);
                    return new QuestOperationResponseDto { Success = false, Message = "Internal error: Hunter or Quest data missing."};
                }

                if (hunterQuest.Status == "Completed")
                {
                    return new QuestOperationResponseDto { Success = false, Message = "Quest is already completed." };
                }

                if (updateDto.CurrentReps.HasValue) hunterQuest.CurrentReps = Math.Max(hunterQuest.CurrentReps, updateDto.CurrentReps.Value);
                if (updateDto.CurrentSets.HasValue) hunterQuest.CurrentSets = Math.Max(hunterQuest.CurrentSets, updateDto.CurrentSets.Value);
                if (updateDto.CurrentDuration.HasValue) hunterQuest.CurrentDuration = Math.Max(hunterQuest.CurrentDuration, updateDto.CurrentDuration.Value);
                if (updateDto.CurrentDistance.HasValue) hunterQuest.CurrentDistance = Math.Max(hunterQuest.CurrentDistance, updateDto.CurrentDistance.Value);

                if (hunterQuest.Status == "Assigned")
                {
                    hunterQuest.StartQuest();
                }

                hunterQuest.UpdateProgress(); // This might change status to "Completed" and calculate XPEarned
                await _context.SaveChangesAsync();

                _logger.LogInformation("📈 Quest '{QuestName}' (ID: {AssignmentID}) progress updated to {Progress}% by Hunter {HunterID}. Status: {Status}",
                    hunterQuest.Quest.QuestName, hunterQuest.AssignmentID, hunterQuest.Progress, hunterId, hunterQuest.Status);

                return new QuestOperationResponseDto
                {
                    Success = true,
                    Message = hunterQuest.Status == "Completed"
                        ? "🎉 Quest completed! Amazing work!"
                        : $"Progress updated: {hunterQuest.Progress:F1}% complete!",
                    Quest = ConvertToHunterDailyQuestDto(hunterQuest, hunterQuest.Hunter)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error updating quest progress for {AssignmentID}, hunter {HunterID}", updateDto.AssignmentID, hunterId);
                 return new QuestOperationResponseDto { Success = false, Message = "An error occurred while updating quest progress."};
            }
        }


        public async Task<QuestOperationResponseDto> CompleteQuestAsync(Guid hunterId, CompleteQuestDto completeDto)
        {
            _logger.LogInformation("Attempting to complete quest {AssignmentID} for hunter {HunterID}", completeDto.AssignmentID, hunterId);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var hunterQuest = await _context.HunterDailyQuests
                    .Include(hq => hq.Quest)
                    .Include(hq => hq.Hunter)
                    .FirstOrDefaultAsync(hq => hq.AssignmentID == completeDto.AssignmentID && hq.HunterID == hunterId);

                if (hunterQuest == null)
                {
                    _logger.LogWarning("CompleteQuestAsync: HunterDailyQuest not found for AssignmentID {AssignmentID}", completeDto.AssignmentID);
                    await transaction.RollbackAsync();
                    return new QuestOperationResponseDto { Success = false, Message = "Quest assignment not found." };
                }
                if (hunterQuest.Hunter == null)
                {
                    _logger.LogError("CompleteQuestAsync: CRITICAL - Hunter entity not loaded for HunterDailyQuest.AssignmentID {AssignmentID}", completeDto.AssignmentID);
                    await transaction.RollbackAsync();
                    return new QuestOperationResponseDto { Success = false, Message = "Critical error: Hunter data missing for quest." };
                }
                if (hunterQuest.Quest == null)
                {
                    _logger.LogError("CompleteQuestAsync: CRITICAL - Quest entity not loaded for HunterDailyQuest.AssignmentID {AssignmentID}", completeDto.AssignmentID);
                    await transaction.RollbackAsync();
                    return new QuestOperationResponseDto { Success = false, Message = "Critical error: Quest details missing." };
                }

                if (hunterQuest.Status == "Completed")
                {
                    await transaction.RollbackAsync();
                    return new QuestOperationResponseDto { Success = false, Message = "Quest is already completed." };
                }

                if (hunterQuest.Status == "Assigned") {
                    hunterQuest.StartQuest(); // Ensure StartedAt is set and status is InProgress
                }

                int oldLevel = hunterQuest.Hunter.Level;
                string oldRank = hunterQuest.Hunter.HunterRank;
                int oldCurrentXP = hunterQuest.Hunter.CurrentXP;

                if (completeDto.FinalReps.HasValue) hunterQuest.CurrentReps = completeDto.FinalReps.Value;
                if (completeDto.FinalSets.HasValue) hunterQuest.CurrentSets = completeDto.FinalSets.Value;
                if (completeDto.FinalDuration.HasValue) hunterQuest.CurrentDuration = completeDto.FinalDuration.Value;
                if (completeDto.FinalDistance.HasValue) hunterQuest.CurrentDistance = completeDto.FinalDistance.Value;

                if (completeDto.PerfectExecution)
                {
                    hunterQuest.BonusMultiplier = hunterQuest.GetBonusMultiplierForCompletion();
                    _logger.LogInformation("CompleteQuestAsync: PerfectExecution for {AssignmentID}, BonusMultiplier set to {BonusMultiplier}", completeDto.AssignmentID, hunterQuest.BonusMultiplier);
                } else {
                    hunterQuest.BonusMultiplier = 1.0m;
                     _logger.LogInformation("CompleteQuestAsync: Not PerfectExecution for {AssignmentID}, BonusMultiplier is {BonusMultiplier}", completeDto.AssignmentID, hunterQuest.BonusMultiplier);
                }

                _logger.LogInformation("CompleteQuestAsync: Before hunterQuest.CompleteQuest() for {AssignmentID}. Quest: '{QuestName}', Hunter Lvl: {HunterLevel}, BaseXP: {BaseXP}, BonusMulti: {BonusMulti}",
                    completeDto.AssignmentID, hunterQuest.Quest.QuestName, hunterQuest.Hunter.Level, hunterQuest.Quest.BaseXPReward, hunterQuest.BonusMultiplier);
                
                hunterQuest.CompleteQuest(); // Calculates XPEarned in HunterDailyQuest model

                _logger.LogInformation("CompleteQuestAsync: After hunterQuest.CompleteQuest() for {AssignmentID}. Calculated XPEarned on hunterQuest: {XPEarned}",
                    completeDto.AssignmentID, hunterQuest.XPEarned);

                var questHistory = new QuestHistory {/* ... populate ... */}; // Populate as before
                _context.QuestHistory.Add(questHistory);

                // Delegate Hunter updates to HunterService.
                // These methods should prepare changes on the tracked 'hunterQuest.Hunter' entity.
                // HunterService methods should NOT call SaveChangesAsync() if QuestService handles the transaction.
                // If HunterService methods MUST call SaveChangesAsync, then this transaction is less effective for atomicity across services.
                await _hunterService.AddXPAsync(hunterId, hunterQuest.XPEarned, $"Quest: {hunterQuest.Quest.QuestName}");
                await _hunterService.IncrementWorkoutCountAsync(hunterId);
                // Consider adding streak update logic via HunterService as well
                // await _hunterService.UpdateStreakAsync(hunterId, true); // Example

                // Single SaveChanges call for atomic operation within this service's context
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Re-fetch the hunter with AsNoTracking to get the definitive state from DB for the response
                var updatedHunter = await _context.Hunters.AsNoTracking().FirstOrDefaultAsync(h => h.HunterID == hunterId);
                if (updatedHunter == null) {
                    _logger.LogError("CompleteQuestAsync: CRITICAL - Failed to re-fetch hunter {HunterID} after commit.", hunterId);
                    return new QuestOperationResponseDto { Success = false, Message = "Error finalizing quest due to data inconsistency." };
                }

                bool leveledUp = updatedHunter.Level > oldLevel;
                bool rankChanged = updatedHunter.HunterRank != oldRank;

                _logger.LogInformation("🎉 Quest '{QuestName}' (ID:{AssignmentID}) completed by Hunter {HunterID}. XP Earned: {XPEarned}. Old Lvl: {OldLevel}, New Lvl: {NewLevel}. Old XP: {OldCurrentXP}, New XP: {NewCurrentXP}. Old Rank: {OldRank}, New Rank: {NewRank}",
                    hunterQuest.Quest.QuestName, completeDto.AssignmentID, hunterId, hunterQuest.XPEarned, oldLevel, updatedHunter.Level, oldCurrentXP, updatedHunter.CurrentXP, oldRank, updatedHunter.HunterRank);

                return new QuestOperationResponseDto
                {
                    Success = true,
                    Message = $"🎉 {hunterQuest.Quest.QuestName} completed! +{hunterQuest.XPEarned} XP earned!",
                    Quest = ConvertToHunterDailyQuestDto(hunterQuest, updatedHunter), // Pass updatedHunter for scaling if needed
                    XPEarned = hunterQuest.XPEarned,
                    LeveledUp = leveledUp,
                    NewLevel = updatedHunter.Level,
                    NewCurrentXP = updatedHunter.CurrentXP,
                    NewXPRequiredForNextLevel = updatedHunter.GetXPRequiredForNextLevel(),
                    NewRank = updatedHunter.HunterRank,
                    NewLevelProgressPercentage = updatedHunter.GetLevelProgressPercentage(),
                    AchievementsUnlocked = new List<string>() // TODO
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error completing quest {AssignmentID} for hunter {HunterID}", completeDto.AssignmentID, hunterId);
                await transaction.RollbackAsync();
                return new QuestOperationResponseDto { Success = false, Message = "An internal error occurred. Your progress might not have been saved."};
            }
        }

        public async Task<List<QuestHistoryDto>> GetQuestHistoryAsync(Guid hunterId, int limit = 50)
        {
            // ... (Código existente, sin cambios necesarios para el problema de XP) ...
            // Asegúrate que el Hunter se pasa a ConvertToHunterDailyQuestDto si es necesario allí.
            // Aquí no es directamente relevante para el cálculo de XP al completar.
            try
            {
                var history = await _context.QuestHistory
                    .Include(qh => qh.Quest)
                    .Where(qh => qh.HunterID == hunterId)
                    .OrderByDescending(qh => qh.CompletedAt)
                    .Take(limit)
                    .Select(qh => new QuestHistoryDto {/* ... mapeo ... */})
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
            // ... (Código existente, sin cambios) ...
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null) throw new ArgumentException("Hunter not found");
                // ... resto de la lógica ...
                return new QuestStatsDto{/* ... mapeo ... */};
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting quest stats for hunter: {HunterID}", hunterId);
                throw;
            }
        }

        public async Task<bool> GenerateDailyQuestsAsync(Guid hunterId, DateTime questDate, int questCount = 3)
        {
            // ... (Código existente, sin cambios) ...
            // Este método llama a SaveChangesAsync, lo cual está bien ya que es una operación autocontenida.
            try
            {
                // ... lógica ...
                await _context.SaveChangesAsync();
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
            // ... (Código existente, pero asegúrate de pasar el hunter al DTO si GetScaledXPReward se usa allí) ...
             try
            {
                var hunter = await _context.Hunters.AsNoTracking().FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null) return new List<AvailableQuestDto>();

                var availableQuests = await _context.DailyQuests
                    .Where(q => q.IsActive)
                    .Select(q => new AvailableQuestDto
                    {
                        // ... (mapeo de campos) ...
                        ScaledXPReward = q.GetScaledXPReward(hunter), // Pasar hunter aquí
                        IsEligible = q.IsValidForHunter(hunter), // Pasar hunter aquí
                        // ... (otros campos) ...
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


        // ConvertToHunterDailyQuestDto ahora toma un Hunter como parámetro
        // para asegurar que la información más actualizada del hunter (especialmente el nivel)
        // se use para calcular ScaledXPReward.
        private HunterDailyQuestDto ConvertToHunterDailyQuestDto(HunterDailyQuest hunterQuest, Hunter? hunterContext)
        {
            if (hunterQuest.Quest == null)
            {
                _logger.LogError("ConvertToHunterDailyQuestDto: Quest data is missing from HunterDailyQuest ID {AssignmentID}. This is a critical data loading issue.", hunterQuest.AssignmentID);
                return new HunterDailyQuestDto { AssignmentID = hunterQuest.AssignmentID, QuestName = "Error: Quest Data Missing", Description = "Contact support."};
            }

            var hunterForScaling = hunterContext ?? hunterQuest.Hunter; // Priorizar el hunter pasado explícitamente

            if (hunterForScaling == null)
            {
                _logger.LogWarning("ConvertToHunterDailyQuestDto: Hunter context is null for scaling XP for Quest {QuestID}. Defaulting to Level 1 scaling. This may result in incorrect XP display.", hunterQuest.QuestID);
                hunterForScaling = new Hunter { Level = 1 }; // Fallback MUY defensivo, el problema de carga debe resolverse.
            }

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
                TargetDescription = hunterQuest.Quest.GetTargetDescription(),
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
                ScaledXPReward = hunterQuest.Quest.GetScaledXPReward(hunterForScaling), // Usar el hunter provisto
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
                RelativeTime = GetRelativeTime(hunterQuest.AssignedAt), // Implementa este método
                IsFromToday = hunterQuest.QuestDate == DateTime.UtcNow.Date
            };
        }

        // Implementa los métodos estáticos GetTargetDescription, GetProgressMessage, GetMotivationalMessage, GetRelativeTime
        // que tenías, si no están ya en otro lugar accesible.
        private static string GetTargetDescription(DailyQuest quest) { /* ... tu lógica ... */ return ""; }
        private static string GetProgressMessage(List<HunterDailyQuestDto> quests) { /* ... tu lógica ... */ return ""; }
        private static string GetMotivationalMessage(List<HunterDailyQuestDto> quests) { /* ... tu lógica ... */ return ""; }
        private static string GetRelativeTime(DateTime dateTime) { /* ... tu lógica ... */ return ""; }
    }
}