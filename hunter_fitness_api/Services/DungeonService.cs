using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HunterFitness.API.Data;
using HunterFitness.API.Models;
using HunterFitness.API.DTOs;

namespace HunterFitness.API.Services
{
    public interface IDungeonService
    {
        Task<List<DungeonDto>> GetAvailableDungeonsAsync(Guid hunterId);
        Task<DungeonDto?> GetDungeonDetailsAsync(Guid dungeonId, Guid hunterId);
        Task<ApiResponseDto<DungeonRaidDto>> StartRaidAsync(Guid hunterId, StartRaidDto startRaidDto);
        Task<ApiResponseDto<DungeonRaidDto>> UpdateRaidProgressAsync(Guid hunterId, UpdateRaidProgressDto updateDto);
        Task<ApiResponseDto<DungeonRaidDto>> CompleteRaidAsync(Guid hunterId, CompleteRaidDto completeDto);
        Task<List<DungeonRaidDto>> GetActiveRaidsAsync(Guid hunterId);
        Task<List<DungeonRaidDto>> GetRaidHistoryAsync(Guid hunterId, int limit = 20);
        Task<ApiResponseDto<object>> AbandonRaidAsync(Guid hunterId, Guid raidId);
    }

    public class DungeonService : IDungeonService
    {
        private readonly HunterFitnessDbContext _context;
        private readonly ILogger<DungeonService> _logger;
        private readonly IHunterService _hunterService;

        public DungeonService(
            HunterFitnessDbContext context,
            ILogger<DungeonService> logger,
            IHunterService hunterService)
        {
            _context = context;
            _logger = logger;
            _hunterService = hunterService;
        }

        public async Task<List<DungeonDto>> GetAvailableDungeonsAsync(Guid hunterId)
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null) return new List<DungeonDto>();

                var dungeons = await _context.Dungeons
                    .Include(d => d.Exercises)
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.MinLevel)
                    .ThenBy(d => d.DungeonType)
                    .ToListAsync();

                var dungeonDtos = new List<DungeonDto>();

                foreach (var dungeon in dungeons)
                {
                    // Verificar si hay un raid activo o en cooldown
                    var lastRaid = await _context.DungeonRaids
                        .Where(dr => dr.HunterID == hunterId && dr.DungeonID == dungeon.DungeonID)
                        .OrderByDescending(dr => dr.StartedAt)
                        .FirstOrDefaultAsync();

                    var dto = ConvertToDungeonDto(dungeon, hunter, lastRaid);
                    dungeonDtos.Add(dto);
                }

                return dungeonDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting available dungeons for hunter: {HunterID}", hunterId);
                return new List<DungeonDto>();
            }
        }

        public async Task<DungeonDto?> GetDungeonDetailsAsync(Guid dungeonId, Guid hunterId)
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null) return null;

                var dungeon = await _context.Dungeons
                    .Include(d => d.Exercises.OrderBy(e => e.ExerciseOrder))
                    .FirstOrDefaultAsync(d => d.DungeonID == dungeonId && d.IsActive);

                if (dungeon == null) return null;

                var lastRaid = await _context.DungeonRaids
                    .Where(dr => dr.HunterID == hunterId && dr.DungeonID == dungeonId)
                    .OrderByDescending(dr => dr.StartedAt)
                    .FirstOrDefaultAsync();

                return ConvertToDungeonDto(dungeon, hunter, lastRaid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting dungeon details: {DungeonID}", dungeonId);
                return null;
            }
        }

        public async Task<ApiResponseDto<DungeonRaidDto>> StartRaidAsync(Guid hunterId, StartRaidDto startRaidDto)
        {
            try
            {
                var hunter = await _context.Hunters.FirstOrDefaultAsync(h => h.HunterID == hunterId && h.IsActive);
                if (hunter == null)
                {
                    return new ApiResponseDto<DungeonRaidDto>
                    {
                        Success = false,
                        Message = "Hunter not found."
                    };
                }

                var dungeon = await _context.Dungeons
                    .Include(d => d.Exercises)
                    .FirstOrDefaultAsync(d => d.DungeonID == startRaidDto.DungeonID && d.IsActive);

                if (dungeon == null)
                {
                    return new ApiResponseDto<DungeonRaidDto>
                    {
                        Success = false,
                        Message = "Dungeon not found."
                    };
                }

                // Verificar elegibilidad
                if (!dungeon.IsEligibleForHunter(hunter))
                {
                    return new ApiResponseDto<DungeonRaidDto>
                    {
                        Success = false,
                        Message = $"You need Level {dungeon.MinLevel} and {dungeon.MinRank} Rank to enter this dungeon."
                    };
                }

                // Verificar cooldown
                var lastRaid = await _context.DungeonRaids
                    .Where(dr => dr.HunterID == hunterId && dr.DungeonID == startRaidDto.DungeonID)
                    .OrderByDescending(dr => dr.StartedAt)
                    .FirstOrDefaultAsync();

                if (lastRaid != null && !lastRaid.CanStartNewRaid())
                {
                    var cooldownRemaining = lastRaid.GetRemainingCooldown();
                    return new ApiResponseDto<DungeonRaidDto>
                    {
                        Success = false,
                        Message = $"Dungeon is on cooldown. Try again in {FormatTimeSpan(cooldownRemaining!.Value)}."
                    };
                }

                // Verificar raids activos
                var activeRaid = await _context.DungeonRaids
                    .Where(dr => dr.HunterID == hunterId && 
                               (dr.Status == "Started" || dr.Status == "InProgress"))
                    .FirstOrDefaultAsync();

                if (activeRaid != null)
                {
                    return new ApiResponseDto<DungeonRaidDto>
                    {
                        Success = false,
                        Message = "You already have an active raid. Complete or abandon it first."
                    };
                }

                // Crear nuevo raid
                var newRaid = new DungeonRaid
                {
                    HunterID = hunterId,
                    DungeonID = startRaidDto.DungeonID,
                    Status = "Started",
                    StartedAt = DateTime.UtcNow
                };

                _context.DungeonRaids.Add(newRaid);
                await _context.SaveChangesAsync();

                // Reload with navigation properties
                newRaid = await _context.DungeonRaids
                    .Include(dr => dr.Dungeon)
                    .Include(dr => dr.Hunter)
                    .FirstOrDefaultAsync(dr => dr.RaidID == newRaid.RaidID);

                _logger.LogInformation("🏰 Dungeon raid started: {DungeonName} by Hunter {HunterID}", 
                    dungeon.DungeonName, hunterId);

                var raidDto = ConvertToDungeonRaidDto(newRaid!);

                return new ApiResponseDto<DungeonRaidDto>
                {
                    Success = true,
                    Message = $"🏰 Raid started: {dungeon.DungeonName}! Prepare for battle!",
                    Data = raidDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error starting dungeon raid: {DungeonID}", startRaidDto.DungeonID);
                return new ApiResponseDto<DungeonRaidDto>
                {
                    Success = false,
                    Message = "An error occurred while starting the raid."
                };
            }
        }

        public async Task<ApiResponseDto<DungeonRaidDto>> UpdateRaidProgressAsync(Guid hunterId, UpdateRaidProgressDto updateDto)
        {
            try
            {
                var raid = await _context.DungeonRaids
                    .Include(dr => dr.Dungeon)
                    .Include(dr => dr.Hunter)
                    .FirstOrDefaultAsync(dr => dr.RaidID == updateDto.RaidID && dr.HunterID == hunterId);

                if (raid == null)
                {
                    return new ApiResponseDto<DungeonRaidDto>
                    {
                        Success = false,
                        Message = "Raid not found."
                    };
                }

                if (raid.Status != "Started" && raid.Status != "InProgress")
                {
                    return new ApiResponseDto<DungeonRaidDto>
                    {
                        Success = false,
                        Message = "This raid is not active."
                    };
                }

                // Actualizar progreso
                raid.Progress = Math.Min(100.00m, Math.Max(0.00m, updateDto.Progress));
                
                if (raid.Status == "Started")
                {
                    raid.StartRaid();
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("📈 Raid progress updated: {Progress}% for {DungeonName}", 
                    raid.Progress, raid.Dungeon.DungeonName);

                var raidDto = ConvertToDungeonRaidDto(raid);

                return new ApiResponseDto<DungeonRaidDto>
                {
                    Success = true,
                    Message = $"Progress updated: {raid.Progress:F1}% complete!",
                    Data = raidDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error updating raid progress: {RaidID}", updateDto.RaidID);
                return new ApiResponseDto<DungeonRaidDto>
                {
                    Success = false,
                    Message = "An error occurred while updating raid progress."
                };
            }
        }

        public async Task<ApiResponseDto<DungeonRaidDto>> CompleteRaidAsync(Guid hunterId, CompleteRaidDto completeDto)
        {
            try
            {
                var raid = await _context.DungeonRaids
                    .Include(dr => dr.Dungeon)
                    .Include(dr => dr.Hunter)
                    .FirstOrDefaultAsync(dr => dr.RaidID == completeDto.RaidID && dr.HunterID == hunterId);

                if (raid == null)
                {
                    return new ApiResponseDto<DungeonRaidDto>
                    {
                        Success = false,
                        Message = "Raid not found."
                    };
                }

                if (raid.Status != "InProgress")
                {
                    return new ApiResponseDto<DungeonRaidDto>
                    {
                        Success = false,
                        Message = "This raid is not in progress."
                    };
                }

                // Completar raid
                raid.CompleteRaid(completeDto.Successful);

                // Si fue exitoso, otorgar recompensas
                if (completeDto.Successful)
                {
                    await _hunterService.AddXPAsync(hunterId, raid.XPEarned, $"Dungeon: {raid.Dungeon.DungeonName}");
                    await _hunterService.IncrementWorkoutCountAsync(hunterId);

                    // TODO: Posible drop de equipment
                    // TODO: Actualizar achievements
                }

                await _context.SaveChangesAsync();

                var status = completeDto.Successful ? "completed" : "failed";
                _logger.LogInformation("🏰 Dungeon raid {Status}: {DungeonName} by Hunter {HunterID} - XP: {XP}", 
                    status, raid.Dungeon.DungeonName, hunterId, raid.XPEarned);

                var raidDto = ConvertToDungeonRaidDto(raid);
                var message = completeDto.Successful 
                    ? $"🎉 Raid completed! {raid.Dungeon.DungeonName} conquered! +{raid.XPEarned} XP!"
                    : "💀 Raid failed, but you gain experience from the attempt.";

                return new ApiResponseDto<DungeonRaidDto>
                {
                    Success = true,
                    Message = message,
                    Data = raidDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error completing raid: {RaidID}", completeDto.RaidID);
                return new ApiResponseDto<DungeonRaidDto>
                {
                    Success = false,
                    Message = "An error occurred while completing the raid."
                };
            }
        }

        public async Task<List<DungeonRaidDto>> GetActiveRaidsAsync(Guid hunterId)
        {
            try
            {
                var activeRaids = await _context.DungeonRaids
                    .Include(dr => dr.Dungeon)
                    .Include(dr => dr.Hunter)
                    .Where(dr => dr.HunterID == hunterId && 
                               (dr.Status == "Started" || dr.Status == "InProgress"))
                    .OrderBy(dr => dr.StartedAt)
                    .ToListAsync();

                return activeRaids.Select(ConvertToDungeonRaidDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting active raids for hunter: {HunterID}", hunterId);
                return new List<DungeonRaidDto>();
            }
        }

        public async Task<List<DungeonRaidDto>> GetRaidHistoryAsync(Guid hunterId, int limit = 20)
        {
            try
            {
                var raidHistory = await _context.DungeonRaids
                    .Include(dr => dr.Dungeon)
                    .Include(dr => dr.Hunter)
                    .Where(dr => dr.HunterID == hunterId && 
                               (dr.Status == "Completed" || dr.Status == "Failed" || dr.Status == "Abandoned"))
                    .OrderByDescending(dr => dr.CompletedAt)
                    .Take(limit)
                    .ToListAsync();

                return raidHistory.Select(ConvertToDungeonRaidDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error getting raid history for hunter: {HunterID}", hunterId);
                return new List<DungeonRaidDto>();
            }
        }

        public async Task<ApiResponseDto<object>> AbandonRaidAsync(Guid hunterId, Guid raidId)
        {
            try
            {
                var raid = await _context.DungeonRaids
                    .Include(dr => dr.Dungeon)
                    .FirstOrDefaultAsync(dr => dr.RaidID == raidId && dr.HunterID == hunterId);

                if (raid == null)
                {
                    return new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Raid not found."
                    };
                }

                if (raid.Status != "Started" && raid.Status != "InProgress")
                {
                    return new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "This raid cannot be abandoned."
                    };
                }

                raid.AbandonRaid();
                await _context.SaveChangesAsync();

                _logger.LogInformation("🏃‍♂️ Raid abandoned: {DungeonName} by Hunter {HunterID}", 
                    raid.Dungeon.DungeonName, hunterId);

                return new ApiResponseDto<object>
                {
                    Success = true,
                    Message = $"Raid abandoned. You can try {raid.Dungeon.DungeonName} again in {raid.Dungeon.GetCooldownText()}."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💀 Error abandoning raid: {RaidID}", raidId);
                return new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred while abandoning the raid."
                };
            }
        }

        // Helper methods
        private DungeonDto ConvertToDungeonDto(Dungeon dungeon, Hunter hunter, DungeonRaid? lastRaid)
        {
            var exercises = dungeon.Exercises?.OrderBy(e => e.ExerciseOrder)
                .Select(e => new DungeonExerciseDto
                {
                    ExerciseID = e.ExerciseID,
                    ExerciseOrder = e.ExerciseOrder,
                    ExerciseName = e.ExerciseName,
                    Description = e.Description,
                    TargetReps = e.TargetReps,
                    TargetSets = e.TargetSets,
                    TargetDuration = e.TargetDuration,
                    RestTimeSeconds = e.RestTimeSeconds,
                    TargetDescription = e.GetTargetDescription(),
                    RestTimeDescription = e.GetRestTimeDescription(),
                    EstimatedTimeDescription = e.GetEstimatedTimeDescription(),
                    ExerciseType = e.GetExerciseTypeGuess(),
                    DifficultyEstimate = e.GetDifficultyEstimate(),
                    DifficultyColor = e.GetDifficultyColor(),
                    Instructions = e.GetInstructions()
                }).ToList() ?? new List<DungeonExerciseDto>();

            var isEligible = dungeon.IsEligibleForHunter(hunter);
            var canStart = isEligible && (lastRaid == null || lastRaid.CanStartNewRaid());
            var cooldownRemaining = lastRaid?.GetRemainingCooldown();

            return new DungeonDto
            {
                DungeonID = dungeon.DungeonID,
                DungeonName = dungeon.DungeonName,
                Description = dungeon.Description,
                DungeonType = dungeon.DungeonType,
                TypeIcon = dungeon.GetTypeIcon(),
                TypeDescription = dungeon.GetTypeDescription(),
                Difficulty = dungeon.Difficulty,
                DifficultyColor = dungeon.GetDifficultyColor(),
                DifficultyDisplayName = dungeon.GetDifficultyDisplayName(),
                DifficultyValue = dungeon.GetDifficultyValue(),
                MinLevel = dungeon.MinLevel,
                MinRank = dungeon.MinRank,
                RequirementsText = dungeon.GetRequirementsText(),
                IsEligible = isEligible,
                IneligibilityReason = !isEligible 
                    ? $"Requires Level {dungeon.MinLevel} and {dungeon.MinRank} Rank"
                    : null,
                EstimatedDuration = dungeon.EstimatedDuration,
                EstimatedTimeText = dungeon.GetEstimatedTimeText(),
                EnergyCost = dungeon.EnergyCost,
                CooldownHours = dungeon.CooldownHours,
                CooldownText = dungeon.GetCooldownText(),
                BaseXPReward = dungeon.BaseXPReward,
                BonusXPReward = dungeon.BonusXPReward,
                TotalXPReward = dungeon.GetTotalXPReward(),
                ScaledXPReward = dungeon.GetScaledXPReward(hunter),
                Rewards = dungeon.GetRewards(),
                ExerciseCount = dungeon.GetExerciseCount(),
                IsBossRaid = dungeon.IsBossRaid(),
                IsHighDifficulty = dungeon.IsHighDifficulty(),
                RecommendedStats = dungeon.GetRecommendedStats(),
                CanStart = canStart,
                NextAvailableAt = lastRaid?.NextAvailableAt,
                CooldownRemaining = cooldownRemaining.HasValue ? FormatTimeSpan(cooldownRemaining.Value) : null,
                SuccessRateEstimate = dungeon.GetSuccessRateEstimate(hunter),
                WarningText = dungeon.GetWarningText(hunter),
                Exercises = exercises
            };
        }

        private DungeonRaidDto ConvertToDungeonRaidDto(DungeonRaid raid)
        {
            return new DungeonRaidDto
            {
                RaidID = raid.RaidID,
                DungeonID = raid.DungeonID,
                DungeonName = raid.Dungeon.DungeonName,
                DungeonType = raid.Dungeon.DungeonType,
                Difficulty = raid.Dungeon.Difficulty,
                Status = raid.Status,
                StatusDisplayName = raid.GetStatusDisplayName(),
                StatusColor = raid.GetStatusColor(),
                Progress = raid.Progress,
                ProgressPercentage = raid.GetProgressPercentage(),
                TotalDuration = raid.TotalDuration,
                FormattedDuration = raid.GetFormattedDuration(),
                XPEarned = raid.XPEarned,
                CompletionRate = raid.CompletionRate,
                StartedAt = raid.StartedAt,
                CompletedAt = raid.CompletedAt,
                NextAvailableAt = raid.NextAvailableAt,
                RemainingCooldown = raid.GetRemainingCooldown().HasValue 
                    ? FormatTimeSpan(raid.GetRemainingCooldown()!.Value) : null,
                CanRestart = raid.CanStartNewRaid(),
                IsEligibleForBonusReward = raid.IsEligibleForBonusReward(),
                RelativeTime = GetRelativeTime(raid.StartedAt)
            };
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h {timeSpan.Minutes}m";
            else if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            else if (timeSpan.TotalMinutes >= 1)
                return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
            else
                return $"{timeSpan.Seconds}s";
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