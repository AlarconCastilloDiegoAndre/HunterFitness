using System.ComponentModel.DataAnnotations;

namespace HunterFitness.API.Models
{
    public class Dungeon
    {
        [Key]
        public Guid DungeonID { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string DungeonName { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [StringLength(30)]
        public string DungeonType { get; set; } = "Training Grounds"; // Training Grounds, Strength Trial, Endurance Test, Boss Raid

        // Configuración de dificultad
        [StringLength(10)]
        public string Difficulty { get; set; } = "Normal"; // Normal, Hard, Extreme, Nightmare

        public int MinLevel { get; set; }

        [StringLength(10)]
        public string MinRank { get; set; } = "E";

        public int EstimatedDuration { get; set; } // en minutos

        // Costos y cooldowns
        public int EnergyCost { get; set; } = 10;
        public int CooldownHours { get; set; } = 24;

        // Recompensas
        public int BaseXPReward { get; set; }
        public int BonusXPReward { get; set; } = 0;

        // Metadatos
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<DungeonExercise> Exercises { get; set; } = new List<DungeonExercise>();
        public virtual ICollection<DungeonRaid> Raids { get; set; } = new List<DungeonRaid>();

        // Métodos Helper
        public bool IsEligibleForHunter(Hunter hunter)
        {
            if (hunter.Level < MinLevel) return false;

            var rankOrder = new Dictionary<string, int>
            {
                {"E", 1}, {"D", 2}, {"C", 3}, {"B", 4}, 
                {"A", 5}, {"S", 6}, {"SS", 7}, {"SSS", 8}
            };

            if (rankOrder.ContainsKey(MinRank) && rankOrder.ContainsKey(hunter.HunterRank))
            {
                return rankOrder[hunter.HunterRank] >= rankOrder[MinRank];
            }

            return true;
        }

        public string GetDifficultyDisplayName()
        {
            return Difficulty switch
            {
                "Normal" => "Normal Difficulty",
                "Hard" => "Hard Difficulty",
                "Extreme" => "Extreme Difficulty", 
                "Nightmare" => "Nightmare Difficulty",
                _ => "Unknown Difficulty"
            };
        }

        public string GetDifficultyColor()
        {
            return Difficulty switch
            {
                "Normal" => "#4CAF50",      // Verde
                "Hard" => "#FF9800",        // Naranja
                "Extreme" => "#F44336",     // Rojo
                "Nightmare" => "#9C27B0",   // Púrpura
                _ => "#757575"              // Gris
            };
        }

        public string GetTypeIcon()
        {
            return DungeonType switch
            {
                "Training Grounds" => "🏟️",
                "Strength Trial" => "💪",
                "Endurance Test" => "🏃‍♂️",
                "Boss Raid" => "👹",
                _ => "⚔️"
            };
        }

        public string GetTypeDescription()
        {
            return DungeonType switch
            {
                "Training Grounds" => "Basic training facility for all-around fitness improvement",
                "Strength Trial" => "Specialized challenges focused on building raw power",
                "Endurance Test" => "Cardiovascular challenges to push your limits",
                "Boss Raid" => "Ultimate challenges with the highest rewards",
                _ => "Unknown dungeon type"
            };
        }

        public int GetTotalXPReward()
        {
            return BaseXPReward + BonusXPReward;
        }

        public int GetScaledXPReward(Hunter hunter)
        {
            var baseReward = GetTotalXPReward();
            var levelMultiplier = 1.0 + (hunter.Level * 0.02); // 2% más XP por nivel
            return (int)(baseReward * levelMultiplier);
        }

        public string GetRequirementsText()
        {
            var requirements = new List<string>();

            if (MinLevel > 1)
                requirements.Add($"Level {MinLevel}+");

            if (MinRank != "E")
                requirements.Add($"{MinRank} Rank+");

            if (EnergyCost > 0)
                requirements.Add($"{EnergyCost} Energy");

            return requirements.Any() ? string.Join(", ", requirements) : "No requirements";
        }

        public string GetCooldownText()
        {
            if (CooldownHours <= 0) return "No cooldown";

            if (CooldownHours >= 24)
            {
                var days = CooldownHours / 24;
                var remainingHours = CooldownHours % 24;
                
                if (remainingHours == 0)
                    return $"{days} day{(days > 1 ? "s" : "")}";
                else
                    return $"{days}d {remainingHours}h";
            }
            else
            {
                return $"{CooldownHours} hour{(CooldownHours > 1 ? "s" : "")}";
            }
        }

        public int GetExerciseCount()
        {
            return Exercises?.Count ?? 0;
        }

        public string GetEstimatedTimeText()
        {
            if (EstimatedDuration >= 60)
            {
                var hours = EstimatedDuration / 60;
                var minutes = EstimatedDuration % 60;
                
                if (minutes == 0)
                    return $"{hours} hour{(hours > 1 ? "s" : "")}";
                else
                    return $"{hours}h {minutes}m";
            }
            else
            {
                return $"{EstimatedDuration} minute{(EstimatedDuration > 1 ? "s" : "")}";
            }
        }

        public bool IsBossRaid()
        {
            return DungeonType == "Boss Raid";
        }

        public bool IsHighDifficulty()
        {
            return Difficulty == "Extreme" || Difficulty == "Nightmare";
        }

        public int GetDifficultyValue()
        {
            return Difficulty switch
            {
                "Normal" => 1,
                "Hard" => 2,
                "Extreme" => 3,
                "Nightmare" => 4,
                _ => 0
            };
        }

        public string GetRecommendedStats()
        {
            return DungeonType switch
            {
                "Strength Trial" => "High Strength recommended",
                "Endurance Test" => "High Vitality and Endurance recommended",
                "Training Grounds" => "Balanced stats recommended",
                "Boss Raid" => "Maximum stats in all areas recommended",
                _ => "Balanced approach recommended"
            };
        }

        public List<string> GetRewards()
        {
            var rewards = new List<string>
            {
                $"{GetTotalXPReward()} XP"
            };

            if (BonusXPReward > 0)
                rewards.Add($"Bonus XP: {BonusXPReward}");

            if (IsBossRaid())
                rewards.Add("Rare Equipment Drop");

            if (IsHighDifficulty())
                rewards.Add("Achievement Progress");

            return rewards;
        }

        public double GetSuccessRateEstimate(Hunter hunter)
        {
            if (!IsEligibleForHunter(hunter)) return 0.0;

            var levelAdvantage = (double)(hunter.Level - MinLevel) / MinLevel;
            var totalStats = hunter.Strength + hunter.Agility + hunter.Vitality + hunter.Endurance;
            var recommendedStats = MinLevel * 4 * 2.5; // Estimación de stats recomendados

            var statAdvantage = totalStats / recommendedStats;
            var baseSuccessRate = 0.6; // 60% base

            var finalRate = baseSuccessRate + (levelAdvantage * 0.2) + (statAdvantage * 0.2);
            return Math.Min(0.95, Math.Max(0.1, finalRate)); // Entre 10% y 95%
        }

        public string GetWarningText(Hunter hunter)
        {
            var successRate = GetSuccessRateEstimate(hunter);
            
            if (successRate < 0.3)
                return "⚠️ High risk of failure - Consider training more";
            else if (successRate < 0.5)
                return "⚠️ Challenging - Prepare carefully";
            else if (successRate < 0.7)
                return "💪 Good challenge level";
            else
                return "✅ You're well prepared for this";
        }

        public Dictionary<string, object> GetDungeonSummary()
        {
            return new Dictionary<string, object>
            {
                {"Name", DungeonName},
                {"Type", DungeonType},
                {"Difficulty", Difficulty},
                {"MinLevel", MinLevel},
                {"MinRank", MinRank},
                {"EstimatedDuration", GetEstimatedTimeText()},
                {"XPReward", GetTotalXPReward()},
                {"EnergyCost", EnergyCost},
                {"Cooldown", GetCooldownText()},
                {"ExerciseCount", GetExerciseCount()},
                {"Requirements", GetRequirementsText()}
            };
        }
    }
}