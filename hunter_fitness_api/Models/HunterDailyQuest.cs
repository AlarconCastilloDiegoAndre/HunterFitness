using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HunterFitness.API.Models
{
    public class HunterDailyQuest
    {
        [Key]
        public Guid AssignmentID { get; set; } = Guid.NewGuid();

        [Required]
        public Guid HunterID { get; set; }

        [Required] 
        public Guid QuestID { get; set; }

        // Estado y progreso
        [StringLength(20)]
        public string Status { get; set; } = "Assigned"; // Assigned, InProgress, Completed, Failed

        [Range(0, 100)]
        public decimal Progress { get; set; } = 0.00m; // Porcentaje 0-100

        // Valores actuales del quest
        public int CurrentReps { get; set; } = 0;
        public int CurrentSets { get; set; } = 0;
        public int CurrentDuration { get; set; } = 0; // en segundos
        public decimal CurrentDistance { get; set; } = 0m; // en metros

        // Recompensas obtenidas
        public int XPEarned { get; set; } = 0;

        [Range(0.5, 5.0)]
        public decimal BonusMultiplier { get; set; } = 1.00m; // Para bonificaciones por velocidad/perfección

        // Timestamps
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        [Column(TypeName = "date")]
        public DateTime QuestDate { get; set; } = DateTime.UtcNow.Date;

        // Navigation Properties
        [ForeignKey("HunterID")]
        public virtual Hunter Hunter { get; set; } = null!;

        [ForeignKey("QuestID")]
        public virtual DailyQuest Quest { get; set; } = null!;

        // Métodos Helper
        public void StartQuest()
        {
            if (Status == "Assigned")
            {
                Status = "InProgress";
                StartedAt = DateTime.UtcNow;
            }
        }

        public bool CanComplete()
        {
            if (Quest == null) return false;

            // Verificar si se cumplieron los objetivos
            bool repsCompleted = !Quest.TargetReps.HasValue || CurrentReps >= Quest.TargetReps.Value;
            bool setsCompleted = !Quest.TargetSets.HasValue || CurrentSets >= Quest.TargetSets.Value;
            bool durationCompleted = !Quest.TargetDuration.HasValue || CurrentDuration >= Quest.TargetDuration.Value;
            bool distanceCompleted = !Quest.TargetDistance.HasValue || CurrentDistance >= Quest.TargetDistance.Value;

            return repsCompleted && setsCompleted && durationCompleted && distanceCompleted;
        }

        public void CompleteQuest()
        {
            // Asumimos que CanComplete() ya validó que Quest != null,
            // pero añadimos defensas aquí también.
            // Permitir completar desde "Assigned" si se quiere marcar directamente sin pasar por "InProgress"
            if (Status == "InProgress" || Status == "Assigned")
            {
                Status = "Completed";
                CompletedAt = DateTime.UtcNow;
                Progress = 100.00m;

                if (Quest == null)
                {
                    XPEarned = 0;
                    // Log para depuración (reemplaza con ILogger si lo inyectas/pasas)
                    System.Diagnostics.Debug.WriteLine($"Warning: HunterDailyQuest.CompleteQuest - Quest object is null for AssignmentID {AssignmentID}. XP set to 0.");
                    return;
                }
                if (Hunter == null)
                {
                    XPEarned = 0;
                    System.Diagnostics.Debug.WriteLine($"Warning: HunterDailyQuest.CompleteQuest - Hunter object is null for AssignmentID {AssignmentID}. XP set to 0.");
                    return;
                }

                // Log de valores antes del cálculo
                System.Diagnostics.Debug.WriteLine($"Debug HunterDailyQuest.CompleteQuest for AssignmentID {AssignmentID}:");
                System.Diagnostics.Debug.WriteLine($"  Quest.QuestName: {Quest.QuestName}, Quest.BaseXPReward: {Quest.BaseXPReward}");
                System.Diagnostics.Debug.WriteLine($"  Hunter.Level: {Hunter.Level}");
                int scaledReward = Quest.GetScaledXPReward(Hunter); // Calcular una vez
                System.Diagnostics.Debug.WriteLine($"  ScaledXPReward before bonus: {scaledReward}");
                System.Diagnostics.Debug.WriteLine($"  BonusMultiplier: {BonusMultiplier}");

                XPEarned = (int)(scaledReward * BonusMultiplier);

                System.Diagnostics.Debug.WriteLine($"  Calculated XPEarned: {XPEarned}");
            }
        }

        public void UpdateProgress()
        {
            if (Quest == null) return;

            decimal totalProgress = 0;
            int completedTargets = 0;
            int totalTargets = 0;

            // Calcular progreso de reps
            if (Quest.TargetReps.HasValue)
            {
                totalTargets++;
                var repsProgress = Math.Min(100, (CurrentReps * 100.0m) / Quest.TargetReps.Value);
                totalProgress += repsProgress;
                if (repsProgress >= 100) completedTargets++;
            }

            // Calcular progreso de sets
            if (Quest.TargetSets.HasValue)
            {
                totalTargets++;
                var setsProgress = Math.Min(100, (CurrentSets * 100.0m) / Quest.TargetSets.Value);
                totalProgress += setsProgress;
                if (setsProgress >= 100) completedTargets++;
            }

            // Calcular progreso de duración
            if (Quest.TargetDuration.HasValue)
            {
                totalTargets++;
                var durationProgress = Math.Min(100, (CurrentDuration * 100.0m) / Quest.TargetDuration.Value);
                totalProgress += durationProgress;
                if (durationProgress >= 100) completedTargets++;
            }

            // Calcular progreso de distancia
            if (Quest.TargetDistance.HasValue)
            {
                totalTargets++;
                var distanceProgress = Math.Min(100, (CurrentDistance * 100.0m) / Quest.TargetDistance.Value);
                totalProgress += distanceProgress;
                if (distanceProgress >= 100) completedTargets++;
            }

            // Actualizar progreso general
            Progress = totalTargets > 0 ? totalProgress / totalTargets : 0;

            // Si se completaron todos los objetivos, marcar como completable
            if (completedTargets == totalTargets && totalTargets > 0)
            {
                CompleteQuest();
            }
        }

        public TimeSpan? GetCompletionTime()
        {
            if (StartedAt.HasValue && CompletedAt.HasValue)
            {
                return CompletedAt.Value - StartedAt.Value;
            }
            return null;
        }

        public string GetProgressDescription()
        {
            if (Quest == null) return "No quest data";

            var descriptions = new List<string>();

            if (Quest.TargetReps.HasValue)
                descriptions.Add($"{CurrentReps}/{Quest.TargetReps} reps");

            if (Quest.TargetSets.HasValue)
                descriptions.Add($"{CurrentSets}/{Quest.TargetSets} sets");

            if (Quest.TargetDuration.HasValue)
                descriptions.Add($"{CurrentDuration}/{Quest.TargetDuration}s");

            if (Quest.TargetDistance.HasValue)
                descriptions.Add($"{CurrentDistance:F1}/{Quest.TargetDistance:F1}m");

            return string.Join(", ", descriptions);
        }

        public decimal GetBonusMultiplierForCompletion()
        {
            if (!CanComplete()) return 1.0m;

            decimal bonus = 1.0m;

            // Bonus por velocidad (si se completa en menos tiempo del estimado)
            if (StartedAt.HasValue && CompletedAt.HasValue && Quest != null)
            {
                var completionTime = CompletedAt.Value - StartedAt.Value;
                var estimatedTime = TimeSpan.FromMinutes(Quest.GetEstimatedTimeMinutes());

                if (completionTime < estimatedTime)
                {
                    bonus += 0.25m; // 25% bonus por velocidad
                }
            }

            // Bonus por ejecución perfecta (completar exactamente los objetivos)
            if (Progress >= 100.0m)
            {
                bonus += 0.15m; // 15% bonus por ejecución perfecta
            }

            return Math.Min(bonus, 2.0m); // Máximo 200% del XP base
        }

        // Validaciones
        public List<string> ValidateData()
        {
            var errors = new List<string>();

            var validStatuses = new[] { "Assigned", "InProgress", "Completed", "Failed" };
            if (!validStatuses.Contains(Status))
                errors.Add("Invalid status");

            if (Progress < 0 || Progress > 100)
                errors.Add("Progress must be between 0 and 100");

            if (CurrentReps < 0 || CurrentSets < 0 || CurrentDuration < 0 || CurrentDistance < 0)
                errors.Add("Progress values cannot be negative");

            if (XPEarned < 0)
                errors.Add("XP earned cannot be negative");

            if (BonusMultiplier < 0.5m || BonusMultiplier > 5.0m)
                errors.Add("Bonus multiplier must be between 0.5 and 5.0");

            if (CompletedAt.HasValue && StartedAt.HasValue && CompletedAt < StartedAt)
                errors.Add("Completion time cannot be before start time");

            return errors;
        }

        // Override para debugging
        public override string ToString()
        {
            return $"{Quest?.QuestName ?? "Unknown Quest"} - {Status} ({Progress:F1}%)";
        }

        public override bool Equals(object? obj)
        {
            if (obj is HunterDailyQuest other)
            {
                return AssignmentID == other.AssignmentID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return AssignmentID.GetHashCode();
        }
    }
}