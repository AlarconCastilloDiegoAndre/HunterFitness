using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HunterFitness.API.Models
{
    public class QuestHistory
    {
        [Key]
        public Guid HistoryID { get; set; } = Guid.NewGuid();

        [Required]
        public Guid HunterID { get; set; }

        [Required] 
        public Guid QuestID { get; set; }

        // Detalles de la completación
        public DateTime CompletedAt { get; set; }
        public int XPEarned { get; set; }
        public int? CompletionTime { get; set; } // Tiempo en segundos
        public bool PerfectExecution { get; set; } = false;

        [Range(0.5, 5.0)]
        public decimal BonusMultiplier { get; set; } = 1.00m;

        // Estadísticas finales
        public int? FinalReps { get; set; }
        public int? FinalSets { get; set; }
        public int? FinalDuration { get; set; }
        public decimal? FinalDistance { get; set; }

        // Metadatos
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("HunterID")]
        public virtual Hunter Hunter { get; set; } = null!;

        [ForeignKey("QuestID")]
        public virtual DailyQuest Quest { get; set; } = null!;

        // Métodos Helper
        public string GetCompletionTimeFormatted()
        {
            if (!CompletionTime.HasValue) return "N/A";

            var timespan = TimeSpan.FromSeconds(CompletionTime.Value);

            if (timespan.TotalHours >= 1)
                return $"{timespan.Hours}h {timespan.Minutes}m {timespan.Seconds}s";
            else if (timespan.TotalMinutes >= 1)
                return $"{timespan.Minutes}m {timespan.Seconds}s";
            else
                return $"{timespan.Seconds}s";
        }

        public string GetPerformanceRating()
        {
            if (PerfectExecution && BonusMultiplier >= 1.5m)
                return "Legendary";
            else if (PerfectExecution)
                return "Perfect";
            else if (BonusMultiplier >= 1.3m)
                return "Excellent";
            else if (BonusMultiplier >= 1.1m)
                return "Good";
            else
                return "Completed";
        }

        public string GetPerformanceColor()
        {
            return GetPerformanceRating() switch
            {
                "Legendary" => "#FFD700",    // Dorado
                "Perfect" => "#9C27B0",      // Púrpura
                "Excellent" => "#4CAF50",    // Verde
                "Good" => "#2196F3",         // Azul
                "Completed" => "#757575",    // Gris
                _ => "#757575"
            };
        }

        public string GetStatsDescription()
        {
            var stats = new List<string>();

            if (FinalReps.HasValue && FinalReps > 0)
                stats.Add($"{FinalReps} reps");

            if (FinalSets.HasValue && FinalSets > 0)
                stats.Add($"{FinalSets} sets");

            if (FinalDuration.HasValue && FinalDuration > 0)
            {
                var duration = TimeSpan.FromSeconds(FinalDuration.Value);
                if (duration.TotalMinutes >= 1)
                    stats.Add($"{duration.Minutes}m {duration.Seconds}s");
                else
                    stats.Add($"{duration.Seconds}s");
            }

            if (FinalDistance.HasValue && FinalDistance > 0)
                stats.Add($"{FinalDistance:F1}m");

            return stats.Any() ? string.Join(", ", stats) : "No stats recorded";
        }

        public bool WasFasterThanEstimated()
        {
            if (!CompletionTime.HasValue || Quest == null) return false;

            var estimatedTime = Quest.GetEstimatedTimeMinutes() * 60; // Convertir a segundos
            return CompletionTime.Value < estimatedTime;
        }

        public decimal GetSpeedBonus()
        {
            if (!WasFasterThanEstimated() || !CompletionTime.HasValue || Quest == null) 
                return 0m;

            var estimatedTime = Quest.GetEstimatedTimeMinutes() * 60;
            var timeSaved = estimatedTime - CompletionTime.Value;
            var speedBonusPercentage = (decimal)timeSaved / estimatedTime;

            return Math.Min(speedBonusPercentage, 0.5m); // Máximo 50% de bonus por velocidad
        }

        public int GetStreakContribution()
        {
            // Un quest completado contribuye 1 día al streak si fue completado en fecha diferente al anterior
            return 1;
        }

        public bool IsPersonalBest(List<QuestHistory> previousCompletions)
        {
            if (!CompletionTime.HasValue) return false;

            // Verificar si este es el tiempo más rápido para este tipo de quest
            var sameQuestCompletions = previousCompletions
                .Where(h => h.QuestID == this.QuestID && h.CompletionTime.HasValue)
                .ToList();

            if (!sameQuestCompletions.Any()) return true;

            return CompletionTime.Value < sameQuestCompletions.Min(h => h.CompletionTime!.Value);
        }

        public string GetQuestTypeFromHistory()
        {
            return Quest?.QuestType ?? "Unknown";
        }

        public string GetDifficultyFromHistory()
        {
            return Quest?.Difficulty ?? "Unknown";
        }

        public TimeSpan GetTimeSinceCompletion()
        {
            return DateTime.UtcNow - CompletedAt;
        }

        public bool IsFromToday()
        {
            return CompletedAt.Date == DateTime.UtcNow.Date;
        }

        public bool IsFromThisWeek()
        {
            var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
            return CompletedAt.Date >= startOfWeek;
        }

        public bool IsFromThisMonth()
        {
            return CompletedAt.Year == DateTime.UtcNow.Year && 
                   CompletedAt.Month == DateTime.UtcNow.Month;
        }

        public string GetRelativeTimeDescription()
        {
            var timeSince = GetTimeSinceCompletion();

            if (timeSince.TotalDays < 1)
                return "Today";
            else if (timeSince.TotalDays < 2)
                return "Yesterday";
            else if (timeSince.TotalDays < 7)
                return $"{(int)timeSince.TotalDays} days ago";
            else if (timeSince.TotalDays < 30)
                return $"{(int)(timeSince.TotalDays / 7)} weeks ago";
            else
                return CompletedAt.ToString("MMM dd, yyyy");
        }

        public Dictionary<string, object> GetCompletionSummary()
        {
            return new Dictionary<string, object>
            {
                {"QuestName", Quest?.QuestName ?? "Unknown Quest"},
                {"CompletedAt", CompletedAt},
                {"XPEarned", XPEarned},
                {"Performance", GetPerformanceRating()},
                {"CompletionTime", GetCompletionTimeFormatted()},
                {"Stats", GetStatsDescription()},
                {"PerfectExecution", PerfectExecution},
                {"BonusMultiplier", BonusMultiplier},
                {"WasFaster", WasFasterThanEstimated()},
                {"SpeedBonus", GetSpeedBonus()}
            };
        }

        public string GetAchievementLevel()
        {
            return GetPerformanceRating() switch
            {
                "Legendary" => "🏆 Legendary Performance",
                "Perfect" => "👑 Perfect Execution",
                "Excellent" => "⭐ Excellent Work",
                "Good" => "💪 Good Job",
                "Completed" => "✅ Completed",
                _ => "✅ Completed"
            };
        }

        public bool ExceededExpectations()
        {
            return PerfectExecution || BonusMultiplier > 1.2m || WasFasterThanEstimated();
        }

        public string GetMotivationalMessage()
        {
            if (ExceededExpectations())
            {
                var messages = new[]
                {
                    "Outstanding work! You're becoming unstoppable! 🔥",
                    "Incredible performance! The Shadow Monarch would be proud! 👑",
                    "Perfect execution! Your dedication is paying off! ⭐",
                    "Amazing results! You're leveling up in real life! 💪"
                };
                return messages[new Random().Next(messages.Length)];
            }
            else
            {
                var messages = new[]
                {
                    "Great job completing this quest! Every step counts! 🌟",
                    "Well done! You're building strength and consistency! 💪",
                    "Nice work! Your fitness journey continues! 🏹",
                    "Quest completed! You're making progress every day! 📈"
                };
                return messages[new Random().Next(messages.Length)];
            }
        }

        public int GetDifficultyPoints()
        {
            if (Quest == null) return 0;

            return Quest.Difficulty switch
            {
                "Easy" => 1,
                "Medium" => 2,
                "Hard" => 3,
                "Extreme" => 5,
                _ => 1
            };
        }

        public decimal GetEfficiencyScore()
        {
            if (!CompletionTime.HasValue || Quest == null) return 1.0m;

            var estimatedTime = Quest.GetEstimatedTimeMinutes() * 60;
            if (estimatedTime <= 0) return 1.0m;

            var efficiency = (decimal)estimatedTime / CompletionTime.Value;
            return Math.Max(0.1m, Math.Min(3.0m, efficiency)); // Entre 0.1x y 3.0x eficiencia
        }

        public bool IsHighPerformance()
        {
            return GetPerformanceRating() is "Excellent" or "Perfect" or "Legendary";
        }

        public string GetComparisonToPrevious(QuestHistory? previousCompletion)
        {
            if (previousCompletion == null || !CompletionTime.HasValue || !previousCompletion.CompletionTime.HasValue)
                return "First completion";

            var timeDiff = CompletionTime.Value - previousCompletion.CompletionTime.Value;
            var xpDiff = XPEarned - previousCompletion.XPEarned;

            if (timeDiff < 0 && xpDiff >= 0)
                return $"🔥 Improved! {Math.Abs(timeDiff)}s faster, +{xpDiff} XP";
            else if (timeDiff < 0)
                return $"⚡ {Math.Abs(timeDiff)}s faster";
            else if (xpDiff > 0)
                return $"📈 +{xpDiff} more XP earned";
            else
                return "Similar performance";
        }

        // Validaciones
        public List<string> ValidateData()
        {
            var errors = new List<string>();

            if (XPEarned < 0)
                errors.Add("XP earned cannot be negative");

            if (CompletionTime.HasValue && CompletionTime <= 0)
                errors.Add("Completion time must be positive");

            if (BonusMultiplier < 0.5m || BonusMultiplier > 5.0m)
                errors.Add("Bonus multiplier must be between 0.5 and 5.0");

            if (FinalReps.HasValue && FinalReps < 0)
                errors.Add("Final reps cannot be negative");

            if (FinalSets.HasValue && FinalSets < 0)
                errors.Add("Final sets cannot be negative");

            if (FinalDuration.HasValue && FinalDuration < 0)
                errors.Add("Final duration cannot be negative");

            if (FinalDistance.HasValue && FinalDistance < 0)
                errors.Add("Final distance cannot be negative");

            if (CompletedAt > DateTime.UtcNow)
                errors.Add("Completion date cannot be in the future");

            return errors;
        }

        // Override para mejor debugging
        public override string ToString()
        {
            return $"{Quest?.QuestName ?? "Unknown"} - {GetPerformanceRating()} ({XPEarned} XP)";
        }

        public override bool Equals(object? obj)
        {
            if (obj is QuestHistory other)
            {
                return HistoryID == other.HistoryID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HistoryID.GetHashCode();
        }
    }
}