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

        public string GetPerformanceRating()
        {
            if (Status != "Completed") return Status;

            if (IsEligibleForBonusReward())
                return "Exceptional";
            else if (CompletionRate >= 90.0m)
                return "Excellent";
            else if (CompletionRate >= 75.0m)
                return "Good";
            else if (CompletionRate >= 50.0m)
                return "Average";
            else
                return "Below Average";
        }

        public string GetPerformanceColor()
        {
            return GetPerformanceRating() switch
            {
                "Exceptional" => "#FFD700",   // Dorado
                "Excellent" => "#4CAF50",     // Verde
                "Good" => "#2196F3",          // Azul
                "Average" => "#FF9800",       // Naranja
                "Below Average" => "#F44336", // Rojo
                _ => "#757575"                // Gris
            };
        }

        public TimeSpan GetElapsedTime()
        {
            if (CompletedAt.HasValue)
                return CompletedAt.Value - StartedAt;
            else
                return DateTime.UtcNow - StartedAt;
        }

        public string GetRelativeTime()
        {
            var elapsed = GetElapsedTime();
            
            if (elapsed.TotalMinutes < 1)
                return "Just started";
            else if (elapsed.TotalHours < 1)
                return $"{(int)elapsed.TotalMinutes} minutes ago";
            else if (elapsed.TotalDays < 1)
                return $"{(int)elapsed.TotalHours} hours ago";
            else
                return StartedAt.ToString("MMM dd");
        }

        public bool IsActive()
        {
            return Status == "Started" || Status == "InProgress";
        }

        public bool IsCompleted()
        {
            return Status == "Completed";
        }

        public bool IsFailed()
        {
            return Status == "Failed";
        }

        public bool IsAbandoned()
        {
            return Status == "Abandoned";
        }

        public string GetCooldownDescription()
        {
            var remaining = GetRemainingCooldown();
            if (!remaining.HasValue || remaining.Value <= TimeSpan.Zero)
                return "Available now";

            if (remaining.Value.TotalDays >= 1)
                return $"{(int)remaining.Value.TotalDays}d {remaining.Value.Hours}h";
            else if (remaining.Value.TotalHours >= 1)
                return $"{remaining.Value.Hours}h {remaining.Value.Minutes}m";
            else
                return $"{remaining.Value.Minutes}m {remaining.Value.Seconds}s";
        }

        public decimal GetEfficiencyScore()
        {
            if (!TotalDuration.HasValue || Dungeon == null || Dungeon.EstimatedDuration <= 0)
                return 1.0m;

            var estimatedSeconds = Dungeon.EstimatedDuration * 60;
            var efficiency = (decimal)estimatedSeconds / TotalDuration.Value;
            
            return Math.Max(0.1m, Math.Min(3.0m, efficiency)); // Entre 0.1x y 3.0x
        }

        public string GetMotivationalMessage()
        {
            return Status switch
            {
                "Started" => "🏰 Ready to conquer this dungeon! Let's begin your raid!",
                "InProgress" => GetProgressPercentage() switch
                {
                    >= 75 => "🔥 Almost there! Victory is within reach!",
                    >= 50 => "💪 Halfway through! Keep pushing forward!",
                    >= 25 => "⚡ Good progress! Stay focused, Hunter!",
                    _ => "🚀 The raid has begun! Show them your power!"
                },
                "Completed" => GetPerformanceRating() switch
                {
                    "Exceptional" => "🏆 LEGENDARY RAID! The Shadow Monarch himself would be impressed!",
                    "Excellent" => "⭐ EXCELLENT RAID! Outstanding performance, Hunter!",
                    "Good" => "💪 GOOD RAID! Solid work conquering this dungeon!",
                    _ => "✅ RAID COMPLETED! Well done, Hunter!"
                },
                "Failed" => "💀 Raid failed, but every hunter learns from defeat. Train harder and return stronger!",
                "Abandoned" => "🏃‍♂️ Sometimes retreat is the wisest choice. Regroup and try again when ready!",
                _ => "🏹 Your dungeon raid adventure awaits!"
            };
        }

        public Dictionary<string, object> GetRaidSummary()
        {
            return new Dictionary<string, object>
            {
                {"RaidID", RaidID},
                {"DungeonName", Dungeon?.DungeonName ?? "Unknown"},
                {"Status", Status},
                {"StatusDisplay", GetStatusDisplayName()},
                {"Progress", Progress},
                {"CompletionRate", CompletionRate},
                {"XPEarned", XPEarned},
                {"Duration", GetFormattedDuration()},
                {"Performance", GetPerformanceRating()},
                {"EfficiencyScore", GetEfficiencyScore()},
                {"StartedAt", StartedAt},
                {"CompletedAt", CompletedAt},
                {"NextAvailable", NextAvailableAt},
                {"CooldownRemaining", GetCooldownDescription()}
            };
        }

        public bool WasFasterThanEstimated()
        {
            if (!TotalDuration.HasValue || Dungeon == null) return false;
            
            var estimatedSeconds = Dungeon.EstimatedDuration * 60;
            return TotalDuration.Value < estimatedSeconds;
        }

        public string GetComparisonToPrevious(DungeonRaid? previousRaid)
        {
            if (previousRaid == null || !IsCompleted() || !previousRaid.IsCompleted())
                return "No comparison available";

            if (!TotalDuration.HasValue || !previousRaid.TotalDuration.HasValue)
                return "Duration comparison not available";

            var timeDiff = TotalDuration.Value - previousRaid.TotalDuration.Value;
            var xpDiff = XPEarned - previousRaid.XPEarned;

            if (timeDiff < 0 && xpDiff >= 0)
                return $"🔥 Improved! {Math.Abs(timeDiff)}s faster, +{xpDiff} XP";
            else if (timeDiff < 0)
                return $"⚡ {Math.Abs(timeDiff)}s faster";
            else if (xpDiff > 0)
                return $"📈 +{xpDiff} more XP earned";
            else
                return "Similar performance";
        }

        // Validaciones
        public List<string> ValidateRaid()
        {
            var errors = new List<string>();

            var validStatuses = new[] { "Started", "InProgress", "Completed", "Failed", "Abandoned" };
            if (!validStatuses.Contains(Status))
                errors.Add("Invalid raid status");

            if (Progress < 0 || Progress > 100)
                errors.Add("Progress must be between 0 and 100");

            if (CompletionRate < 0 || CompletionRate > 100)
                errors.Add("Completion rate must be between 0 and 100");

            if (XPEarned < 0)
                errors.Add("XP earned cannot be negative");

            if (TotalDuration.HasValue && TotalDuration <= 0)
                errors.Add("Total duration must be positive");

            if (CompletedAt.HasValue && CompletedAt < StartedAt)
                errors.Add("Completion time cannot be before start time");

            if (IsCompleted() && !CompletedAt.HasValue)
                errors.Add("Completed raids must have completion time");

            return errors;
        }

        // Override para mejor debugging
        public override string ToString()
        {
            return $"{Dungeon?.DungeonName ?? "Unknown"} Raid - {Status} ({Progress:F1}%)";
        }

        public override bool Equals(object? obj)
        {
            if (obj is DungeonRaid other)
            {
                return RaidID == other.RaidID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return RaidID.GetHashCode();
        }
    }
}