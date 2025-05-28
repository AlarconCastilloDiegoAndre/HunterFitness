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
            var equipmentBonus = Equipment?
                .Where(e => e.IsEquipped && e.Equipment != null)
                .Sum(e => e.Equipment.StrengthBonus + e.Equipment.AgilityBonus + 
                         e.Equipment.VitalityBonus + e.Equipment.EnduranceBonus) ?? 0;
            
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
    }
}