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
                {"BonusMultiplier", BonusMultiplier}
            };
        }
    }
}