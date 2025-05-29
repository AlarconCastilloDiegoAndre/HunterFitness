using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HunterFitness.API.Models
{
    public class Hunter
    {
        [Key]
        public Guid HunterID { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string HunterName { get; set; } = string.Empty;

        // Stats del Hunter (Solo Leveling style)
        public int Level { get; set; } = 1;
        public int CurrentXP { get; set; } = 0;
        public int TotalXP { get; set; } = 0;

        [StringLength(10)]
        public string HunterRank { get; set; } = "E";

        // Stats principales
        public int Strength { get; set; } = 10;
        public int Agility { get; set; } = 10;
        public int Vitality { get; set; } = 10;
        public int Endurance { get; set; } = 10;

        // Progreso y streaks
        public int DailyStreak { get; set; } = 0;
        public int LongestStreak { get; set; } = 0;
        public int TotalWorkouts { get; set; } = 0;

        // Metadatos
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? ProfilePictureUrl { get; set; }

        // Navigation Properties
        public virtual ICollection<HunterDailyQuest> DailyQuests { get; set; } = new List<HunterDailyQuest>();
        public virtual ICollection<DungeonRaid> DungeonRaids { get; set; } = new List<DungeonRaid>();
        public virtual ICollection<HunterAchievement> Achievements { get; set; } = new List<HunterAchievement>();
        public virtual ICollection<HunterEquipment> Equipment { get; set; } = new List<HunterEquipment>();
        public virtual ICollection<QuestHistory> QuestHistory { get; set; } = new List<QuestHistory>();

        // Métodos Helper
        public int GetTotalStatsWithEquipment()
        {
            var baseStats = Strength + Agility + Vitality + Endurance;
            
            if (Equipment?.Any() != true)
                return baseStats;

            var equipmentBonus = Equipment
                .Where(e => e.IsEquipped && e.Equipment != null)
                .Sum(e => e.Equipment!.StrengthBonus + e.Equipment.AgilityBonus + 
                         e.Equipment.VitalityBonus + e.Equipment.EnduranceBonus);
            
            return baseStats + equipmentBonus;
        }

        public string GetRankDisplayName()
        {
            return HunterRank switch
            {
                "E" => "Rookie Hunter",
                "D" => "Bronze Hunter", 
                "C" => "Silver Hunter",
                "B" => "Gold Hunter",
                "A" => "Elite Hunter",
                "S" => "Master Hunter",
                "SS" => "Legendary Hunter",
                "SSS" => "Shadow Monarch",
                _ => "Unknown Rank"
            };
        }

        public int GetXPRequiredForNextLevel()
        {
            // Curva exponencial para leveling
            return (int)(100 * Math.Pow(1.5, Level - 1));
        }

        public bool CanLevelUp()
        {
            return CurrentXP >= GetXPRequiredForNextLevel();
        }

        public string GetNextRankRequirement()
        {
            return HunterRank switch
            {
                "E" => "Reach Level 11 to become Bronze Hunter",
                "D" => "Reach Level 21 to become Silver Hunter",
                "C" => "Reach Level 36 to become Gold Hunter", 
                "B" => "Reach Level 51 to become Elite Hunter",
                "A" => "Reach Level 71 to become Master Hunter",
                "S" => "Reach Level 86 to become Legendary Hunter",
                "SS" => "Reach Level 96 to become Shadow Monarch",
                "SSS" => "Maximum rank achieved!",
                _ => "Unknown rank progression"
            };
        }

        public void UpdateRankBasedOnLevel()
        {
            HunterRank = Level switch
            {
                >= 96 => "SSS",
                >= 86 => "SS", 
                >= 71 => "S",
                >= 51 => "A",
                >= 36 => "B",
                >= 21 => "C",
                >= 11 => "D",
                _ => "E"
            };
        }

        public void ProcessLevelUp()
        {
            while (CanLevelUp())
            {
                var xpRequired = GetXPRequiredForNextLevel();
                CurrentXP -= xpRequired;
                Level++;
                
                // Actualizar rank automáticamente
                UpdateRankBasedOnLevel();
            }
        }

        public decimal GetLevelProgressPercentage()
        {
            var xpRequired = GetXPRequiredForNextLevel();
            return xpRequired > 0 ? (decimal)CurrentXP / xpRequired * 100 : 0;
        }

        public Dictionary<string, object> GetHunterSummary()
        {
            return new Dictionary<string, object>
            {
                {"HunterID", HunterID},
                {"Username", Username},
                {"HunterName", HunterName},
                {"Level", Level},
                {"Rank", HunterRank},
                {"RankDisplay", GetRankDisplayName()},
                {"TotalXP", TotalXP},
                {"CurrentXP", CurrentXP},
                {"XPForNextLevel", GetXPRequiredForNextLevel()},
                {"TotalStats", Strength + Agility + Vitality + Endurance},
                {"TotalStatsWithEquipment", GetTotalStatsWithEquipment()},
                {"DailyStreak", DailyStreak},
                {"LongestStreak", LongestStreak},
                {"TotalWorkouts", TotalWorkouts},
                {"JoinedDaysAgo", (DateTime.UtcNow - CreatedAt).Days},
                {"EquippedItemsCount", Equipment?.Count(e => e.IsEquipped) ?? 0}
            };
        }

        // Métodos adicionales para estadísticas
        public int GetEquippedItemsCount()
        {
            return Equipment?.Count(e => e.IsEquipped) ?? 0;
        }

        public decimal GetTotalXPMultiplier()
        {
            if (Equipment?.Any() != true)
                return 1.0m;

            var multiplier = Equipment
                .Where(e => e.IsEquipped && e.Equipment != null)
                .Sum(e => e.Equipment!.XPMultiplier - 1.0m);

            return 1.0m + multiplier;
        }

        public int GetTotalPowerLevel()
        {
            if (Equipment?.Any() != true)
                return 0;

            return Equipment
                .Where(e => e.IsEquipped && e.Equipment != null)
                .Sum(e => e.Equipment!.GetPowerLevel());
        }

        // Métodos para bonificaciones de equipment
        public int GetEquipmentStrengthBonus()
        {
            return Equipment?
                .Where(e => e.IsEquipped && e.Equipment != null)
                .Sum(e => e.Equipment!.StrengthBonus) ?? 0;
        }

        public int GetEquipmentAgilityBonus()
        {
            return Equipment?
                .Where(e => e.IsEquipped && e.Equipment != null)
                .Sum(e => e.Equipment!.AgilityBonus) ?? 0;
        }

        public int GetEquipmentVitalityBonus()
        {
            return Equipment?
                .Where(e => e.IsEquipped && e.Equipment != null)
                .Sum(e => e.Equipment!.VitalityBonus) ?? 0;
        }

        public int GetEquipmentEnduranceBonus()
        {
            return Equipment?
                .Where(e => e.IsEquipped && e.Equipment != null)
                .Sum(e => e.Equipment!.EnduranceBonus) ?? 0;
        }

        // Stats totales con equipment
        public int GetTotalStrength()
        {
            return Strength + GetEquipmentStrengthBonus();
        }

        public int GetTotalAgility()
        {
            return Agility + GetEquipmentAgilityBonus();
        }

        public int GetTotalVitality()
        {
            return Vitality + GetEquipmentVitalityBonus();
        }

        public int GetTotalEndurance()
        {
            return Endurance + GetEquipmentEnduranceBonus();
        }

        // Validaciones
        public bool IsEligibleForRankUp()
        {
            return HunterRank switch
            {
                "E" => Level >= 11,
                "D" => Level >= 21,
                "C" => Level >= 36,
                "B" => Level >= 51,
                "A" => Level >= 71,
                "S" => Level >= 86,
                "SS" => Level >= 96,
                "SSS" => false, // Máximo rank
                _ => false
            };
        }

        public int GetDaysSinceJoining()
        {
            return Math.Max(1, (DateTime.UtcNow - CreatedAt).Days);
        }

        public decimal GetAverageXPPerDay()
        {
            var days = GetDaysSinceJoining();
            return days > 0 ? (decimal)TotalXP / days : 0;
        }

        public bool IsNewHunter()
        {
            return GetDaysSinceJoining() <= 7;
        }

        public bool IsVeteranHunter()
        {
            return GetDaysSinceJoining() >= 365;
        }

        public string GetHunterTitle()
        {
            if (IsNewHunter())
                return "Rookie";
            else if (Level >= 50 && DailyStreak >= 30)
                return "Dedicated Warrior";
            else if (LongestStreak >= 100)
                return "Streak Master";
            else if (TotalWorkouts >= 1000)
                return "Workout Legend";
            else if (IsVeteranHunter())
                return "Veteran Hunter";
            else
                return GetRankDisplayName();
        }

        // Override para mejor debugging
        public override string ToString()
        {
            return $"Hunter: {HunterName} (Level {Level} {HunterRank}) - {Username}";
        }

        // Método para validar integridad de datos
        public List<string> ValidateData()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Username))
                errors.Add("Username is required");

            if (string.IsNullOrWhiteSpace(Email))
                errors.Add("Email is required");

            if (string.IsNullOrWhiteSpace(HunterName))
                errors.Add("Hunter name is required");

            if (Level < 1)
                errors.Add("Level must be at least 1");

            if (CurrentXP < 0)
                errors.Add("Current XP cannot be negative");

            if (TotalXP < CurrentXP)
                errors.Add("Total XP cannot be less than current XP");

            if (Strength < 1 || Agility < 1 || Vitality < 1 || Endurance < 1)
                errors.Add("All stats must be at least 1");

            if (DailyStreak > LongestStreak)
                errors.Add("Daily streak cannot be longer than longest streak");

            var validRanks = new[] { "E", "D", "C", "B", "A", "S", "SS", "SSS" };
            if (!validRanks.Contains(HunterRank))
                errors.Add("Invalid hunter rank");

            return errors;
        }
    }
}