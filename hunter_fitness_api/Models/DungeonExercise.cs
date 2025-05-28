using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HunterFitness.API.Models
{
    public class DungeonExercise
    {
        [Key]
        public Guid ExerciseID { get; set; } = Guid.NewGuid();

        [Required]
        public Guid DungeonID { get; set; }

        public int ExerciseOrder { get; set; }

        // Detalles del ejercicio
        [Required]
        [StringLength(100)]
        public string ExerciseName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int? TargetReps { get; set; }
        public int? TargetSets { get; set; }
        public int? TargetDuration { get; set; } // en segundos
        public int RestTimeSeconds { get; set; } = 30;

        // Metadatos
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("DungeonID")]
        public virtual Dungeon Dungeon { get; set; } = null!;

        // Métodos Helper
        public string GetTargetDescription()
        {
            var targets = new List<string>();

            if (TargetReps.HasValue)
                targets.Add($"{TargetReps} reps");

            if (TargetSets.HasValue)
                targets.Add($"{TargetSets} sets");

            if (TargetDuration.HasValue)
            {
                var duration = TimeSpan.FromSeconds(TargetDuration.Value);
                if (duration.TotalMinutes >= 1)
                    targets.Add($"{duration.Minutes}m {duration.Seconds}s");
                else
                    targets.Add($"{duration.Seconds}s");
            }

            return targets.Any() ? string.Join(", ", targets) : "Complete exercise";
        }

        public string GetRestTimeDescription()
        {
            if (RestTimeSeconds <= 0) return "No rest";

            if (RestTimeSeconds >= 60)
            {
                var minutes = RestTimeSeconds / 60;
                var seconds = RestTimeSeconds % 60;
                
                if (seconds == 0)
                    return $"{minutes} minute{(minutes > 1 ? "s" : "")} rest";
                else
                    return $"{minutes}m {seconds}s rest";
            }
            else
            {
                return $"{RestTimeSeconds} second{(RestTimeSeconds > 1 ? "s" : "")} rest";
            }
        }

        public int GetEstimatedTimeSeconds()
        {
            var exerciseTime = 0;

            // Tiempo estimado basado en el tipo de ejercicio
            if (TargetDuration.HasValue)
            {
                exerciseTime = TargetDuration.Value;
            }
            else if (TargetReps.HasValue && TargetSets.HasValue)
            {
                // Estimación: ~2 segundos por rep
                exerciseTime = TargetReps.Value * TargetSets.Value * 2;
            }
            else if (TargetReps.HasValue)
            {
                exerciseTime = TargetReps.Value * 2;
            }
            else
            {
                // Tiempo por defecto para ejercicios sin tiempo específico
                exerciseTime = 60;
            }

            return exerciseTime + RestTimeSeconds;
        }

        public string GetEstimatedTimeDescription()
        {
            var totalSeconds = GetEstimatedTimeSeconds();
            var timespan = TimeSpan.FromSeconds(totalSeconds);

            if (timespan.TotalMinutes >= 1)
                return $"~{timespan.Minutes}m {timespan.Seconds}s";
            else
                return $"~{timespan.Seconds}s";
        }

        public string GetExerciseTypeGuess()
        {
            var name = ExerciseName.ToLower();

            if (name.Contains("push") || name.Contains("press") || name.Contains("lift"))
                return "Strength";
            else if (name.Contains("run") || name.Contains("cardio") || name.Contains("bike"))
                return "Cardio";
            else if (name.Contains("stretch") || name.Contains("yoga") || name.Contains("flexibility"))
                return "Flexibility";
            else if (name.Contains("plank") || name.Contains("hold") || name.Contains("endurance"))
                return "Endurance";
            else
                return "Mixed";
        }

        public string GetDifficultyEstimate()
        {
            var totalWork = 0;

            if (TargetReps.HasValue && TargetSets.HasValue)
                totalWork = TargetReps.Value * TargetSets.Value;
            else if (TargetReps.HasValue)
                totalWork = TargetReps.Value;
            else if (TargetDuration.HasValue)
                totalWork = TargetDuration.Value / 30; // 30 segundos = 1 unidad de trabajo

            return totalWork switch
            {
                <= 20 => "Easy",
                <= 50 => "Medium", 
                <= 100 => "Hard",
                _ => "Extreme"
            };
        }

        public string GetDifficultyColor()
        {
            return GetDifficultyEstimate() switch
            {
                "Easy" => "#4CAF50",      // Verde
                "Medium" => "#FF9800",    // Naranja
                "Hard" => "#F44336",      // Rojo
                "Extreme" => "#9C27B0",   // Púrpura
                _ => "#757575"            // Gris
            };
        }

        public bool HasTimeTarget()
        {
            return TargetDuration.HasValue;
        }

        public bool HasRepTarget()
        {
            return TargetReps.HasValue;
        }

        public bool HasSetTarget()
        {
            return TargetSets.HasValue;
        }

        public string GetInstructions()
        {
            if (!string.IsNullOrEmpty(Description))
                return Description;

            // Generar instrucciones básicas basadas en los targets
            var instructions = new List<string>();

            if (HasSetTarget() && HasRepTarget())
                instructions.Add($"Complete {TargetSets} sets of {TargetReps} {ExerciseName.ToLower()}");
            else if (HasRepTarget())
                instructions.Add($"Complete {TargetReps} {ExerciseName.ToLower()}");
            else if (HasTimeTarget())
                instructions.Add($"Perform {ExerciseName.ToLower()} for {GetEstimatedTimeDescription()}");
            else
                instructions.Add($"Complete the {ExerciseName.ToLower()} exercise");

            if (RestTimeSeconds > 0)
                instructions.Add($"Rest {GetRestTimeDescription()} before next exercise");

            return string.Join(". ", instructions);
        }

        public Dictionary<string, object> GetProgressRequirements()
        {
            return new Dictionary<string, object>
            {
                {"ExerciseName", ExerciseName},
                {"TargetReps", TargetReps ?? 0},
                {"TargetSets", TargetSets ?? 0}, 
                {"TargetDuration", TargetDuration ?? 0},
                {"RestTime", RestTimeSeconds},
                {"EstimatedTime", GetEstimatedTimeSeconds()},
                {"Instructions", GetInstructions()}
            };
        }

        public bool IsCompleted(int currentReps, int currentSets, int currentDuration)
        {
            bool repsCompleted = !TargetReps.HasValue || currentReps >= TargetReps.Value;
            bool setsCompleted = !TargetSets.HasValue || currentSets >= TargetSets.Value;
            bool durationCompleted = !TargetDuration.HasValue || currentDuration >= TargetDuration.Value;

            return repsCompleted && setsCompleted && durationCompleted;
        }

        public decimal GetCompletionPercentage(int currentReps, int currentSets, int currentDuration)
        {
            var completedTargets = 0;
            var totalTargets = 0;
            decimal totalProgress = 0;

            if (TargetReps.HasValue)
            {
                totalTargets++;
                var progress = Math.Min(100, (currentReps * 100.0m) / TargetReps.Value);
                totalProgress += progress;
                if (progress >= 100) completedTargets++;
            }

            if (TargetSets.HasValue)
            {
                totalTargets++;
                var progress = Math.Min(100, (currentSets * 100.0m) / TargetSets.Value);
                totalProgress += progress;
                if (progress >= 100) completedTargets++;
            }

            if (TargetDuration.HasValue)
            {
                totalTargets++;
                var progress = Math.Min(100, (currentDuration * 100.0m) / TargetDuration.Value);
                totalProgress += progress;
                if (progress >= 100) completedTargets++;
            }

            return totalTargets > 0 ? totalProgress / totalTargets : 0;
        }

        public string GetProgressDescription(int currentReps, int currentSets, int currentDuration)
        {
            var descriptions = new List<string>();

            if (TargetReps.HasValue)
                descriptions.Add($"Reps: {currentReps}/{TargetReps}");

            if (TargetSets.HasValue)
                descriptions.Add($"Sets: {currentSets}/{TargetSets}");

            if (TargetDuration.HasValue)
                descriptions.Add($"Time: {currentDuration}s/{TargetDuration}s");

            return descriptions.Any() ? string.Join(", ", descriptions) : "No progress tracked";
        }

        public string GetMotivationalMessage(decimal completionPercentage)
        {
            return completionPercentage switch
            {
                >= 100 => "🎉 Exercise completed! Great work, Hunter!",
                >= 75 => "💪 Almost there! Push through!",
                >= 50 => "🔥 Halfway done! Keep the momentum!",
                >= 25 => "📈 Good progress! Stay focused!",
                _ => "⚡ Let's begin! You've got this!"
            };
        }
    }
}