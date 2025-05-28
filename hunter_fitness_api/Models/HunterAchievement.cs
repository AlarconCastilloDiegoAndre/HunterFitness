using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HunterFitness.API.Models
{
    public class HunterAchievement
    {
        [Key]
        public Guid HunterAchievementID { get; set; } = Guid.NewGuid();

        [Required]
        public Guid HunterID { get; set; }

        [Required]
        public Guid AchievementID { get; set; }

        // Progreso
        public int CurrentProgress { get; set; } = 0;
        public bool IsUnlocked { get; set; } = false;
        public DateTime? UnlockedAt { get; set; }

        // Metadatos
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("HunterID")]
        public virtual Hunter Hunter { get; set; } = null!;

        [ForeignKey("AchievementID")]
        public virtual Achievement Achievement { get; set; } = null!;

        // Métodos Helper
        public void UpdateProgress(int newProgress)
        {
            CurrentProgress = Math.Max(CurrentProgress, newProgress);
            
            if (!IsUnlocked && Achievement != null && 
                Achievement.TargetValue.HasValue && 
                CurrentProgress >= Achievement.TargetValue.Value)
            {
                UnlockAchievement();
            }
        }

        public void IncrementProgress(int increment = 1)
        {
            UpdateProgress(CurrentProgress + increment);
        }

        public void UnlockAchievement()
        {
            if (!IsUnlocked)
            {
                IsUnlocked = true;
                UnlockedAt = DateTime.UtcNow;
                
                if (Achievement?.TargetValue.HasValue == true)
                {
                    CurrentProgress = Achievement.TargetValue.Value;
                }
            }
        }

        public decimal GetProgressPercentage()
        {
            if (Achievement?.TargetValue == null || Achievement.TargetValue <= 0)
                return IsUnlocked ? 100m : 0m;

            var percentage = (decimal)CurrentProgress / Achievement.TargetValue.Value * 100m;
            return Math.Min(percentage, 100m);
        }

        public int GetRemainingProgress()
        {
            if (Achievement?.TargetValue == null || IsUnlocked)
                return 0;

            return Math.Max(0, Achievement.TargetValue.Value - CurrentProgress);
        }

        public string GetProgressDescription()
        {
            if (Achievement == null) return "No achievement data";

            return Achievement.AchievementType switch
            {
                "Counter" => $"{CurrentProgress}/{Achievement.TargetValue ?? 0}",
                "Streak" => $"{CurrentProgress} day streak",
                "Single" => IsUnlocked ? "Completed" : "Not completed",
                "Progressive" => $"Level {GetProgressLevel()}/10",
                _ => $"{CurrentProgress}"
            };
        }

        public int GetProgressLevel()
        {
            if (Achievement?.TargetValue == null) return 0;
            
            // Para achievements progresivos, calcular nivel basado en el progreso
            var progressRatio = (double)CurrentProgress / Achievement.TargetValue.Value;
            return Math.Min(10, (int)(progressRatio * 10) + 1);
        }

        public bool CanBeUnlocked()
        {
            return !IsUnlocked && 
                   Achievement != null && 
                   Achievement.TargetValue.HasValue && 
                   CurrentProgress >= Achievement.TargetValue.Value;
        }

        public TimeSpan? GetTimeSinceUnlocked()
        {
            if (!IsUnlocked || !UnlockedAt.HasValue)
                return null;

            return DateTime.UtcNow - UnlockedAt.Value;
        }

        public string GetRarityColor()
        {
            if (Achievement == null) return "#757575";

            return Achievement.Category switch
            {
                "Consistency" => "#4CAF50",    // Verde
                "Strength" => "#F44336",       // Rojo
                "Endurance" => "#2196F3",      // Azul
                "Social" => "#FF9800",         // Naranja
                "Special" => "#9C27B0",        // Púrpura
                "Milestone" => "#FFD700",      // Dorado
                _ => "#757575"                 // Gris
            };
        }

        public string GetUnlockStatusText()
        {
            if (IsUnlocked)
            {
                return UnlockedAt.HasValue 
                    ? $"Unlocked on {UnlockedAt.Value:MMM dd, yyyy}"
                    : "Unlocked";
            }

            if (Achievement?.TargetValue.HasValue == true)
            {
                var remaining = GetRemainingProgress();
                return remaining > 0 
                    ? $"{remaining} more to unlock"
                    : "Ready to unlock!";
            }

            return "In progress";
        }

        public bool IsRecentlyUnlocked(int hours = 24)
        {
            return IsUnlocked && 
                   UnlockedAt.HasValue && 
                   DateTime.UtcNow - UnlockedAt.Value <= TimeSpan.FromHours(hours);
        }

        public string GetCategoryIcon()
        {
            if (Achievement == null) return "🏆";

            return Achievement.Category switch
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
    }
}