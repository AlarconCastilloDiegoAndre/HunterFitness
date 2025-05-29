using System.ComponentModel.DataAnnotations;

namespace HunterFitness.API.Models
{
    public class Achievement
    {
        [Key]
        public Guid AchievementID { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string AchievementName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Category { get; set; } = "Milestone"; // Consistency, Strength, Endurance, Social, Special, Milestone

        // Configuración del logro
        public int? TargetValue { get; set; } // Valor objetivo (ej: 100 workouts)

        [Required]
        [StringLength(20)]
        public string AchievementType { get; set; } = "Counter"; // Counter, Streak, Single, Progressive

        public bool IsHidden { get; set; } = false; // Para achievements secretos

        // Recompensas
        public int XPReward { get; set; } = 0;

        [StringLength(50)]
        public string? TitleReward { get; set; }

        // Metadatos
        [StringLength(500)]
        public string? IconUrl { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<HunterAchievement> HunterAchievements { get; set; } = new List<HunterAchievement>();

        // Métodos Helper
        public string GetCategoryDisplayName()
        {
            return Category switch
            {
                "Consistency" => "Consistency Master",
                "Strength" => "Strength Champion",
                "Endurance" => "Endurance Warrior",
                "Social" => "Community Leader",
                "Special" => "Special Hunter",
                "Milestone" => "Milestone Achiever",
                _ => "Unknown Category"
            };
        }

        public string GetCategoryDescription()
        {
            return Category switch
            {
                "Consistency" => "Achievements for maintaining workout streaks and regular training",
                "Strength" => "Achievements for strength-based exercises and power development",
                "Endurance" => "Achievements for cardiovascular and endurance challenges",
                "Social" => "Achievements for community engagement and social features",
                "Special" => "Rare and unique achievements with special requirements",
                "Milestone" => "Major milestones in your fitness journey",
                _ => "Various achievements"
            };
        }

        public string GetDifficultyLevel()
        {
            if (!TargetValue.HasValue) return "Special";

            return TargetValue.Value switch
            {
                <= 10 => "Novice",
                <= 50 => "Apprentice", 
                <= 100 => "Adept",
                <= 500 => "Expert",
                <= 1000 => "Master",
                _ => "Legendary"
            };
        }

        public string GetRarityColor()
        {
            return GetDifficultyLevel() switch
            {
                "Novice" => "#9E9E9E",      // Gris
                "Apprentice" => "#4CAF50",  // Verde
                "Adept" => "#2196F3",       // Azul
                "Expert" => "#9C27B0",      // Púrpura
                "Master" => "#FF9800",      // Naranja
                "Legendary" => "#F44336",   // Rojo
                "Special" => "#FFD700",     // Dorado
                _ => "#757575"
            };
        }

        public string GetTypeDescription()
        {
            return AchievementType switch
            {
                "Counter" => "Complete a specific number of actions",
                "Streak" => "Maintain consistency over consecutive days",
                "Single" => "Complete a one-time challenge",
                "Progressive" => "Multi-level achievement with increasing difficulty",
                _ => "Special achievement type"
            };
        }

        public bool IsEligibleForHunter(Hunter hunter)
        {
            // Verificar si el hunter es elegible para este achievement
            // Por ahora, todos los achievements están disponibles para todos
            return IsActive && hunter.IsActive;
        }

        public int GetEstimatedDaysToComplete(Hunter? hunter)
        {
            if (!TargetValue.HasValue) return 0;

            return AchievementType switch
            {
                "Counter" => Math.Max(1, TargetValue.Value / 3), // Asumiendo ~3 actividades por día
                "Streak" => TargetValue.Value, // Días directos para streaks
                "Single" => 1, // Achievements únicos
                "Progressive" => Math.Max(7, TargetValue.Value / 10), // Progresivos toman más tiempo
                _ => 7
            };
        }

        public string GetProgressHint()
        {
            return AchievementType switch
            {
                "Counter" when Category == "Consistency" => "Complete daily workouts to progress",
                "Counter" when Category == "Strength" => "Focus on strength-based exercises",
                "Counter" when Category == "Endurance" => "Complete cardio and endurance challenges",
                "Streak" => "Maintain daily workout consistency",
                "Single" => "Complete the specific challenge requirement",
                "Progressive" => "Progress through multiple levels of difficulty",
                _ => "Check the achievement description for specific requirements"
            };
        }

        public List<string> GetRelatedAchievements()
        {
            // Retornar achievements relacionados basados en categoría
            return Category switch
            {
                "Consistency" => new List<string> { "Streak Master", "Daily Warrior", "Habit Builder" },
                "Strength" => new List<string> { "Iron Will", "Power House", "Muscle Builder" },
                "Endurance" => new List<string> { "Marathon Runner", "Cardio King", "Stamina Master" },
                "Social" => new List<string> { "Team Player", "Community Helper", "Social Butterfly" },
                _ => new List<string>()
            };
        }

        public bool RequiresSpecialConditions()
        {
            return IsHidden || AchievementType == "Single" || Category == "Special";
        }

        public string GetCompletionCelebrationMessage()
        {
            return GetDifficultyLevel() switch
            {
                "Legendary" => "🏆 LEGENDARY ACHIEVEMENT UNLOCKED! You are among the elite!",
                "Master" => "🥇 MASTER LEVEL ACHIEVED! Your dedication is inspiring!",
                "Expert" => "⭐ EXPERT STATUS REACHED! You're becoming unstoppable!",
                "Adept" => "🎯 ADEPT LEVEL UNLOCKED! Your skills are growing!",
                "Apprentice" => "📈 APPRENTICE ACHIEVEMENT! You're on the right path!",
                "Novice" => "🌟 FIRST ACHIEVEMENT! Welcome to your fitness journey!",
                _ => "🎉 SPECIAL ACHIEVEMENT UNLOCKED! Something extraordinary happened!"
            };
        }

        public Dictionary<string, object> GetAchievementStats()
        {
            return new Dictionary<string, object>
            {
                {"Name", AchievementName},
                {"Category", Category},
                {"Type", AchievementType},
                {"Difficulty", GetDifficultyLevel()},
                {"XPReward", XPReward},
                {"TitleReward", TitleReward ?? "None"},
                {"TargetValue", TargetValue ?? 0},
                {"IsHidden", IsHidden},
                {"EstimatedDays", GetEstimatedDaysToComplete(null)}
            };
        }

        // Métodos adicionales para análisis
        public bool IsConsistencyAchievement()
        {
            return Category == "Consistency";
        }

        public bool IsStrengthAchievement()
        {
            return Category == "Strength";
        }

        public bool IsEnduranceAchievement()
        {
            return Category == "Endurance";
        }

        public bool IsSocialAchievement()
        {
            return Category == "Social";
        }

        public bool IsSpecialAchievement()
        {
            return Category == "Special";
        }

        public bool IsMilestoneAchievement()
        {
            return Category == "Milestone";
        }

        public bool IsCounterType()
        {
            return AchievementType == "Counter";
        }

        public bool IsStreakType()
        {
            return AchievementType == "Streak";
        }

        public bool IsSingleType()
        {
            return AchievementType == "Single";
        }

        public bool IsProgressiveType()
        {
            return AchievementType == "Progressive";
        }

        public bool HasTitleReward()
        {
            return !string.IsNullOrWhiteSpace(TitleReward);
        }

        public bool IsHighValue()
        {
            return XPReward >= 1000 || GetDifficultyLevel() is "Master" or "Legendary";
        }

        public bool IsEasyToComplete()
        {
            return GetDifficultyLevel() is "Novice" or "Apprentice";
        }

        public bool IsChallengingToComplete()
        {
            return GetDifficultyLevel() is "Expert" or "Master" or "Legendary";
        }

        public string GetCategoryIcon()
        {
            return Category switch
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

        public string GetDifficultyStars()
        {
            return GetDifficultyLevel() switch
            {
                "Novice" => "⭐",
                "Apprentice" => "⭐⭐",
                "Adept" => "⭐⭐⭐",
                "Expert" => "⭐⭐⭐⭐",
                "Master" => "⭐⭐⭐⭐⭐",
                "Legendary" => "⭐⭐⭐⭐⭐⭐",
                _ => "✨"
            };
        }

        public int GetDifficultyValue()
        {
            return GetDifficultyLevel() switch
            {
                "Novice" => 1,
                "Apprentice" => 2,
                "Adept" => 3,
                "Expert" => 4,
                "Master" => 5,
                "Legendary" => 6,
                _ => 0
            };
        }

        public string GetShortDescription()
        {
            if (Description.Length <= 50)
                return Description;
            
            return Description.Substring(0, 47) + "...";
        }

        public bool IsRecentlyCreated(int days = 30)
        {
            return DateTime.UtcNow - CreatedAt <= TimeSpan.FromDays(days);
        }

        // Validaciones
        public List<string> ValidateData()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(AchievementName))
                errors.Add("Achievement name is required");

            if (string.IsNullOrWhiteSpace(Description))
                errors.Add("Description is required");

            var validCategories = new[] { "Consistency", "Strength", "Endurance", "Social", "Special", "Milestone" };
            if (!validCategories.Contains(Category))
                errors.Add("Invalid category");

            var validTypes = new[] { "Counter", "Streak", "Single", "Progressive" };
            if (!validTypes.Contains(AchievementType))
                errors.Add("Invalid achievement type");

            if (XPReward < 0)
                errors.Add("XP reward cannot be negative");

            if (TargetValue.HasValue && TargetValue <= 0)
                errors.Add("Target value must be positive if specified");

            if (!string.IsNullOrWhiteSpace(TitleReward) && TitleReward.Length > 50)
                errors.Add("Title reward is too long");

            // Validaciones específicas por tipo
            if (AchievementType == "Streak" && (!TargetValue.HasValue || TargetValue <= 0))
                errors.Add("Streak achievements must have a positive target value");

            if (AchievementType == "Counter" && (!TargetValue.HasValue || TargetValue <= 0))
                errors.Add("Counter achievements must have a positive target value");

            return errors;
        }

        // Override para mejor debugging
        public override string ToString()
        {
            return $"{Category} Achievement: {AchievementName} ({GetDifficultyLevel()})";
        }

        public override bool Equals(object? obj)
        {
            if (obj is Achievement other)
            {
                return AchievementID == other.AchievementID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return AchievementID.GetHashCode();
        }
    }
}