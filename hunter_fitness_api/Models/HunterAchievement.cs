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

        public string GetRelativeTimeUnlocked()
        {
            if (!IsUnlocked || !UnlockedAt.HasValue)
                return "Not unlocked";

            var timeSince = DateTime.UtcNow - UnlockedAt.Value;

            if (timeSince.TotalMinutes < 1)
                return "Just unlocked";
            else if (timeSince.TotalHours < 1)
                return $"{(int)timeSince.TotalMinutes} minutes ago";
            else if (timeSince.TotalDays < 1)
                return $"{(int)timeSince.TotalHours} hours ago";
            else if (timeSince.TotalDays < 7)
                return $"{(int)timeSince.TotalDays} days ago";
            else
                return UnlockedAt.Value.ToString("MMM dd, yyyy");
        }

        public string GetMotivationalMessage()
        {
            if (IsUnlocked)
                return Achievement?.GetCompletionCelebrationMessage() ?? "Achievement unlocked!";

            var progressPercentage = GetProgressPercentage();
            return progressPercentage switch
            {
                >= 90 => "🔥 So close! You're almost there!",
                >= 75 => "💪 Great progress! Keep pushing!",
                >= 50 => "📈 Halfway there! You're doing amazing!",
                >= 25 => "🌟 Good start! Keep up the momentum!",
                _ => "🚀 Your journey begins! Every step counts!"
            };
        }

        public Dictionary<string, object> GetAchievementProgress()
        {
            return new Dictionary<string, object>
            {
                {"AchievementName", Achievement?.AchievementName ?? "Unknown"},
                {"Category", Achievement?.Category ?? "Unknown"},
                {"CurrentProgress", CurrentProgress},
                {"TargetValue", Achievement?.TargetValue ?? 0},
                {"ProgressPercentage", GetProgressPercentage()},
                {"RemainingProgress", GetRemainingProgress()},
                {"IsUnlocked", IsUnlocked},
                {"UnlockedAt", UnlockedAt},
                {"CanBeUnlocked", CanBeUnlocked()},
                {"XPReward", Achievement?.XPReward ?? 0},
                {"TitleReward", Achievement?.TitleReward},
                {"Difficulty", Achievement?.GetDifficultyLevel() ?? "Unknown"}
            };
        }

        public bool IsNearCompletion()
        {
            return !IsUnlocked && GetProgressPercentage() >= 75m;
        }

        public bool HasSignificantProgress()
        {
            return CurrentProgress > 0 || IsUnlocked;
        }

        public string GetProgressBar(int width = 20)
        {
            var percentage = GetProgressPercentage();
            var filledWidth = (int)(width * percentage / 100);
            var filled = new string('█', filledWidth);
            var empty = new string('░', width - filledWidth);
            return $"[{filled}{empty}] {percentage:F0}%";
        }

        public string GetEstimatedTimeToCompletion()
        {
            if (IsUnlocked) return "Completed";
            if (Achievement?.TargetValue == null) return "Unknown";

            var remaining = GetRemainingProgress();
            if (remaining <= 0) return "Ready to unlock";

            // Estimación básica basada en progreso actual
            var daysWithProgress = Math.Max(1, (DateTime.UtcNow - CreatedAt).Days);
            var progressPerDay = CurrentProgress > 0 ? (double)CurrentProgress / daysWithProgress : 0.5;
            
            if (progressPerDay <= 0) return "Unknown";

            var estimatedDays = (int)Math.Ceiling(remaining / progressPerDay);
            
            return estimatedDays switch
            {
                <= 1 => "Within a day",
                <= 7 => $"About {estimatedDays} days",
                <= 30 => $"About {estimatedDays / 7} weeks",
                _ => $"About {estimatedDays / 30} months"
            };
        }

        // Validaciones
        public List<string> ValidateProgress()
        {
            var errors = new List<string>();

            if (Achievement == null)
                errors.Add("Achievement reference is missing");

            if (Hunter == null)
                errors.Add("Hunter reference is missing");

            if (CurrentProgress < 0)
                errors.Add("Progress cannot be negative");

            if (IsUnlocked && !UnlockedAt.HasValue)
                errors.Add("Unlocked achievements must have unlock date");

            if (!IsUnlocked && UnlockedAt.HasValue)
                errors.Add("Non-unlocked achievements cannot have unlock date");

            if (Achievement?.TargetValue.HasValue == true && CurrentProgress > Achievement.TargetValue.Value)
                errors.Add("Progress cannot exceed target value");

            return errors;
        }

        // Override para mejor debugging
        public override string ToString()
        {
            var status = IsUnlocked ? "Unlocked" : $"{GetProgressPercentage():F0}%";
            return $"{Achievement?.AchievementName ?? "Unknown"} - {status}";
        }

        public override bool Equals(object? obj)
        {
            if (obj is HunterAchievement other)
            {
                return HunterAchievementID == other.HunterAchievementID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HunterAchievementID.GetHashCode();
        }
    }
}