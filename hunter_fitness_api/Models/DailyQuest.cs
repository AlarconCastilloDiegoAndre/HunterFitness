using System.ComponentModel.DataAnnotations;

namespace HunterFitness.API.Models
{
    public class DailyQuest
    {
        [Key]
        public Guid QuestID { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string QuestName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string QuestType { get; set; } = string.Empty; // Cardio, Strength, Flexibility, Endurance, Mixed

        // Configuración del ejercicio
        [Required]
        [StringLength(100)]
        public string ExerciseName { get; set; } = string.Empty;

        public int? TargetReps { get; set; }
        public int? TargetSets { get; set; }
        public int? TargetDuration { get; set; } // en segundos
        public decimal? TargetDistance { get; set; } // en metros

        // Recompensas y dificultad
        [Required]
        [StringLength(10)]
        public string Difficulty { get; set; } = "Easy"; // Easy, Medium, Hard, Extreme

        public int BaseXPReward { get; set; }
        public int StrengthBonus { get; set; } = 0;
        public int AgilityBonus { get; set; } = 0;
        public int VitalityBonus { get; set; } = 0;
        public int EnduranceBonus { get; set; } = 0;

        // Requisitos
        public int MinLevel { get; set; } = 1;

        [Required]
        [StringLength(10)]
        public string MinRank { get; set; } = "E";

        // Metadatos
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<HunterDailyQuest> HunterDailyQuests { get; set; } = new List<HunterDailyQuest>();
        public virtual ICollection<QuestHistory> QuestHistory { get; set; } = new List<QuestHistory>();

        // Métodos Helper
        public bool IsValidForHunter(Hunter hunter)
        {
            if (hunter.Level < MinLevel) return false;
            
            var rankOrder = new Dictionary<string, int>
            {
                {"E", 1}, {"D", 2}, {"C", 3}, {"B", 4}, 
                {"A", 5}, {"S", 6}, {"SS", 7}, {"SSS", 8}
            };

            if (rankOrder.TryGetValue(MinRank, out var requiredRankValue) && 
                rankOrder.TryGetValue(hunter.HunterRank, out var hunterRankValue))
            {
                return hunterRankValue >= requiredRankValue;
            }

            return true;
        }

        public int GetScaledXPReward(Hunter hunter)
        {
            // Escalar XP basado en el nivel del hunter
            var multiplier = 1.0 + (hunter.Level * 0.05); // 5% más XP por nivel
            return (int)(BaseXPReward * multiplier);
        }

        public string GetDifficultyColor()
        {
            return Difficulty switch
            {
                "Easy" => "#4CAF50",      // Verde
                "Medium" => "#FF9800",    // Naranja
                "Hard" => "#F44336",      // Rojo
                "Extreme" => "#9C27B0",   // Púrpura
                _ => "#757575"            // Gris
            };
        }

        public string GetQuestTypeIcon()
        {
            return QuestType switch
            {
                "Cardio" => "🏃‍♂️",
                "Strength" => "💪",
                "Flexibility" => "🤸‍♂️",
                "Endurance" => "⏱️",
                "Mixed" => "🔥",
                _ => "⚡"
            };
        }

        public int GetEstimatedTimeMinutes()
        {
            // Estimación básica basada en tipo de quest
            return QuestType switch
            {
                "Cardio" => TargetDuration.HasValue ? (TargetDuration.Value / 60) : 15,
                "Strength" => (TargetSets ?? 1) * 3, // 3 minutos por set aproximadamente
                "Flexibility" => 10,
                "Endurance" => TargetDuration.HasValue ? (TargetDuration.Value / 60) : 20,
                "Mixed" => 25,
                _ => 15
            };
        }

        // Métodos adicionales para análisis
        public bool IsCardioQuest() => QuestType == "Cardio";
        public bool IsStrengthQuest() => QuestType == "Strength";
        public bool IsFlexibilityQuest() => QuestType == "Flexibility";
        public bool IsEnduranceQuest() => QuestType == "Endurance";
        public bool IsMixedQuest() => QuestType == "Mixed";

        public bool IsEasyDifficulty() => Difficulty == "Easy";
        public bool IsMediumDifficulty() => Difficulty == "Medium";
        public bool IsHardDifficulty() => Difficulty == "Hard";
        public bool IsExtremeDifficulty() => Difficulty == "Extreme";

        public bool HasRepsTarget() => TargetReps.HasValue && TargetReps > 0;
        public bool HasSetsTarget() => TargetSets.HasValue && TargetSets > 0;
        public bool HasDurationTarget() => TargetDuration.HasValue && TargetDuration > 0;
        public bool HasDistanceTarget() => TargetDistance.HasValue && TargetDistance > 0;

        public string GetTargetDescription()
        {
            var targets = new List<string>();

            if (HasRepsTarget())
                targets.Add($"{TargetReps} reps");

            if (HasSetsTarget())
                targets.Add($"{TargetSets} sets");

            if (HasDurationTarget())
            {
                var duration = TimeSpan.FromSeconds(TargetDuration!.Value);
                if (duration.TotalMinutes >= 1)
                    targets.Add($"{duration.Minutes}m {duration.Seconds}s");
                else
                    targets.Add($"{duration.Seconds}s");
            }

            if (HasDistanceTarget())
                targets.Add($"{TargetDistance:F1}m");

            return targets.Any() ? string.Join(", ", targets) : "Complete exercise";
        }

        public string GetRequirementsText()
        {
            var requirements = new List<string>();

            if (MinLevel > 1)
                requirements.Add($"Level {MinLevel}+");

            if (MinRank != "E")
                requirements.Add($"{MinRank} Rank+");

            return requirements.Any() ? string.Join(", ", requirements) : "No requirements";
        }

        public int GetTotalStatBonus()
        {
            return StrengthBonus + AgilityBonus + VitalityBonus + EnduranceBonus;
        }

        public bool HasStatBonuses() => GetTotalStatBonus() > 0;

        public string GetStatBonusDescription()
        {
            var bonuses = new List<string>();

            if (StrengthBonus > 0)
                bonuses.Add($"+{StrengthBonus} STR");
            
            if (AgilityBonus > 0)
                bonuses.Add($"+{AgilityBonus} AGI");
            
            if (VitalityBonus > 0)
                bonuses.Add($"+{VitalityBonus} VIT");
            
            if (EnduranceBonus > 0)
                bonuses.Add($"+{EnduranceBonus} END");

            return bonuses.Any() ? string.Join(", ", bonuses) : "No stat bonuses";
        }

        public string GetDominantStat()
        {
            var stats = new Dictionary<string, int>
            {
                {"Strength", StrengthBonus},
                {"Agility", AgilityBonus},
                {"Vitality", VitalityBonus},
                {"Endurance", EnduranceBonus}
            };

            var maxStat = stats.OrderByDescending(s => s.Value).FirstOrDefault();
            return maxStat.Value > 0 ? maxStat.Key : "Balanced";
        }

        public int GetDifficultyValue()
        {
            return Difficulty switch
            {
                "Easy" => 1,
                "Medium" => 2,
                "Hard" => 3,
                "Extreme" => 4,
                _ => 0
            };
        }

        public string GetDifficultyStars()
        {
            return Difficulty switch
            {
                "Easy" => "⭐",
                "Medium" => "⭐⭐",
                "Hard" => "⭐⭐⭐",
                "Extreme" => "⭐⭐⭐⭐",
                _ => ""
            };
        }

        public string GetEstimatedTimeText()
        {
            var minutes = GetEstimatedTimeMinutes();
            return minutes >= 60 
                ? $"{minutes / 60}h {minutes % 60}m"
                : $"{minutes} min";
        }

        public bool IsHighXPReward() => BaseXPReward >= 500;
        public bool IsLowXPReward() => BaseXPReward < 100;
        public bool RequiresHighLevel() => MinLevel >= 25;
        public bool RequiresHighRank() => new[] { "A", "S", "SS", "SSS" }.Contains(MinRank);

        public string GetQuestClassification()
        {
            if (IsExtremeDifficulty() && RequiresHighRank())
                return "Elite Challenge";
            else if (IsHardDifficulty() && RequiresHighLevel())
                return "Advanced Quest";
            else if (IsMediumDifficulty())
                return "Standard Quest";
            else
                return "Beginner Quest";
        }

        public Dictionary<string, object> GetQuestSummary()
        {
            return new Dictionary<string, object>
            {
                {"Name", QuestName},
                {"Type", QuestType},
                {"Exercise", ExerciseName},
                {"Difficulty", Difficulty},
                {"BaseXP", BaseXPReward},
                {"EstimatedTime", GetEstimatedTimeText()},
                {"Requirements", GetRequirementsText()},
                {"Targets", GetTargetDescription()},
                {"StatBonuses", GetStatBonusDescription()},
                {"Classification", GetQuestClassification()}
            };
        }

        // Validaciones
        public List<string> ValidateData()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(QuestName))
                errors.Add("Quest name is required");

            if (string.IsNullOrWhiteSpace(Description))
                errors.Add("Description is required");

            if (string.IsNullOrWhiteSpace(ExerciseName))
                errors.Add("Exercise name is required");

            var validTypes = new[] { "Cardio", "Strength", "Flexibility", "Endurance", "Mixed" };
            if (!validTypes.Contains(QuestType))
                errors.Add("Invalid quest type");

            var validDifficulties = new[] { "Easy", "Medium", "Hard", "Extreme" };
            if (!validDifficulties.Contains(Difficulty))
                errors.Add("Invalid difficulty");

            var validRanks = new[] { "E", "D", "C", "B", "A", "S", "SS", "SSS" };
            if (!validRanks.Contains(MinRank))
                errors.Add("Invalid minimum rank");

            if (MinLevel < 1)
                errors.Add("Minimum level must be at least 1");

            if (BaseXPReward < 0)
                errors.Add("Base XP reward cannot be negative");

            if (TargetReps.HasValue && TargetReps <= 0)
                errors.Add("Target reps must be positive if specified");

            if (TargetSets.HasValue && TargetSets <= 0)
                errors.Add("Target sets must be positive if specified");

            if (TargetDuration.HasValue && TargetDuration <= 0)
                errors.Add("Target duration must be positive if specified");

            if (TargetDistance.HasValue && TargetDistance <= 0)
                errors.Add("Target distance must be positive if specified");

            if (StrengthBonus < 0 || AgilityBonus < 0 || VitalityBonus < 0 || EnduranceBonus < 0)
                errors.Add("Stat bonuses cannot be negative");

            // Validar que al menos un target esté definido
            if (!HasRepsTarget() && !HasSetsTarget() && !HasDurationTarget() && !HasDistanceTarget())
                errors.Add("At least one target (reps, sets, duration, or distance) must be specified");

            return errors;
        }

        public bool IsRecentlyCreated(int days = 30)
        {
            return DateTime.UtcNow - CreatedAt <= TimeSpan.FromDays(days);
        }

        // Override para mejor debugging
        public override string ToString()
        {
            return $"{QuestType} Quest: {QuestName} ({Difficulty}) - {GetTargetDescription()}";
        }

        public override bool Equals(object? obj)
        {
            if (obj is DailyQuest other)
            {
                return QuestID == other.QuestID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return QuestID.GetHashCode();
        }
    }
}