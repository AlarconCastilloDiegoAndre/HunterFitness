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
        [StringLength(10)]
        public string Difficulty { get; set; } = "Easy"; // Easy, Medium, Hard, Extreme

        public int BaseXPReward { get; set; }
        public int StrengthBonus { get; set; } = 0;
        public int AgilityBonus { get; set; } = 0;
        public int VitalityBonus { get; set; } = 0;
        public int EnduranceBonus { get; set; } = 0;

        // Requisitos
        public int MinLevel { get; set; } = 1;

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

            if (rankOrder.ContainsKey(MinRank) && rankOrder.ContainsKey(hunter.HunterRank))
            {
                return rankOrder[hunter.HunterRank] >= rankOrder[MinRank];
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
    }
}