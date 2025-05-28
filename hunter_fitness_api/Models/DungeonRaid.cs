using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HunterFitness.API.Models
{
    public class DungeonRaid
    {
        [Key]
        public Guid RaidID { get; set; } = Guid.NewGuid();

        [Required]
        public Guid HunterID { get; set; }

        [Required]
        public Guid DungeonID { get; set; }

        // Estado del raid
        [StringLength(20)]
        public string Status { get; set; } = "Started"; // Started, InProgress, Completed, Failed, Abandoned

        [Range(0, 100)]
        public decimal Progress { get; set; } = 0.00m;

        // Resultados
        public int? TotalDuration { get; set; } // en segundos
        public int XPEarned { get; set; } = 0;

        [Range(0, 100)]
        public decimal CompletionRate { get; set; } = 0.00m;

        // Timestamps
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? NextAvailableAt { get; set; }

        // Navigation Properties
        [ForeignKey("HunterID")]
        public virtual Hunter Hunter { get; set; } = null!;

        [ForeignKey("DungeonID")]
        public virtual Dungeon Dungeon { get; set; } = null!;

        // Métodos Helper
        public void StartRaid()
        {
            if (Status == "Started")
            {
                Status = "InProgress";
                StartedAt = DateTime.UtcNow;
            }
        }

        public void CompleteRaid(bool successful = true)
        {
            if (Status == "InProgress")
            {
                Status = successful ? "Completed" : "Failed";
                CompletedAt = DateTime.UtcNow;
                
                if (StartedAt != default)
                {
                    TotalDuration = (int)(CompletedAt.Value - StartedAt).TotalSeconds;
                }

                // Establecer cooldown
                if (Dungeon != null)
                {
                    NextAvailableAt = DateTime.UtcNow.AddHours(Dungeon.CooldownHours);
                    
                    if (successful)
                    {
                        XPEarned = CalculateXPReward();
                        CompletionRate = 100.00m;
                    }
                    else
                    {
                        // XP parcial por intento fallido
                        XPEarned = (int)(CalculateXPReward() * 0.25m);
                        CompletionRate = Progress;
                    }
                }
            }
        }

        public void AbandonRaid()
        {
            if (Status == "InProgress")
            {
                Status = "Abandoned";
                CompletedAt = DateTime.UtcNow;
                CompletionRate = Progress;
                XPEarned = 0; // Sin recompensa por abandonar
                
                // Cooldown reducido por abandono
                if (Dungeon != null)
                {
                    NextAvailableAt = DateTime.UtcNow.AddHours(Dungeon.CooldownHours / 2);
                }
            }
        }

        public bool CanStartNewRaid()
        {
            return !NextAvailableAt.HasValue || DateTime.UtcNow >= NextAvailableAt.Value;
        }

        public TimeSpan? GetRemainingCooldown()
        {
            if (!NextAvailableAt.HasValue) return null;
            
            var remaining = NextAvailableAt.Value - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        private int CalculateXPReward()
        {
            if (Dungeon == null || Hunter == null) return 0;

            var baseReward = Dungeon.BaseXPReward;
            
            // Bonus por nivel del hunter
            var levelMultiplier = 1.0 + (Hunter.Level * 0.02); // 2% más por nivel
            
            // Bonus por tiempo de completación
            var timeBonus = 1.0;
            if (TotalDuration.HasValue && Dungeon.EstimatedDuration > 0)
            {
                var estimatedSeconds = Dungeon.EstimatedDuration * 60;
                if (TotalDuration.Value < estimatedSeconds)
                {
                    // Bonus por completar más rápido
                    timeBonus = 1.0 + (0.5 * (estimatedSeconds - TotalDuration.Value) / estimatedSeconds);
                }
            }

            // Bonus adicional del dungeon
            var bonusReward = Dungeon.BonusXPReward;

            return (int)((baseReward * levelMultiplier * timeBonus) + bonusReward);
        }

        public string GetStatusDisplayName()
        {
            return Status switch
            {
                "Started" => "Ready to Begin",
                "InProgress" => "In Progress",
                "Completed" => "Successfully Completed",
                "Failed" => "Failed",
                "Abandoned" => "Abandoned",
                _ => "Unknown Status"
            };
        }

        public string GetStatusColor()
        {
            return Status switch
            {
                "Started" => "#2196F3",      // Azul
                "InProgress" => "#FF9800",   // Naranja
                "Completed" => "#4CAF50",    // Verde
                "Failed" => "#F44336",       // Rojo
                "Abandoned" => "#757575",    // Gris
                _ => "#757575"
            };
        }

        public double GetProgressPercentage()
        {
            return (double)Progress;
        }

        public string GetFormattedDuration()
        {
            if (!TotalDuration.HasValue) return "N/A";
            
            var timespan = TimeSpan.FromSeconds(TotalDuration.Value);
            
            if (timespan.TotalHours >= 1)
                return $"{timespan.Hours}h {timespan.Minutes}m {timespan.Seconds}s";
            else if (timespan.TotalMinutes >= 1)
                return $"{timespan.Minutes}m {timespan.Seconds}s";
            else
                return $"{timespan.Seconds}s";
        }

        public bool IsEligibleForBonusReward()
        {
            return Status == "Completed" && 
                   CompletionRate >= 95.0m && 
                   TotalDuration.HasValue && 
                   Dungeon != null &&
                   TotalDuration.Value <= (Dungeon.EstimatedDuration * 60 * 1.1); // Dentro del 110% del tiempo estimado
        }
    }
}