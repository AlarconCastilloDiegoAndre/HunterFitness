namespace HunterFitness.API.DTOs
{
    // DTO para quest diaria asignada al hunter
    public class HunterDailyQuestDto
    {
        public Guid AssignmentID { get; set; }
        public Guid QuestID { get; set; }
        public string QuestName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string QuestType { get; set; } = string.Empty;
        public string QuestTypeIcon { get; set; } = string.Empty;
        public string ExerciseName { get; set; } = string.Empty;
        
        // Objetivos
        public int? TargetReps { get; set; }
        public int? TargetSets { get; set; }
        public int? TargetDuration { get; set; }
        public decimal? TargetDistance { get; set; }
        public string TargetDescription { get; set; } = string.Empty;
        
        // Progreso actual
        public int CurrentReps { get; set; }
        public int CurrentSets { get; set; }
        public int CurrentDuration { get; set; }
        public decimal CurrentDistance { get; set; }
        public string ProgressDescription { get; set; } = string.Empty;
        
        // Estado
        public string Status { get; set; } = string.Empty;
        public decimal Progress { get; set; }
        public bool CanComplete { get; set; }
        public bool IsCompleted { get; set; }
        
        // Recompensas
        public string Difficulty { get; set; } = string.Empty;
        public string DifficultyColor { get; set; } = string.Empty;
        public int BaseXPReward { get; set; }
        public int ScaledXPReward { get; set; }
        public int XPEarned { get; set; }
        public decimal BonusMultiplier { get; set; }
        
        // Bonificaciones de stats
        public int StrengthBonus { get; set; }
        public int AgilityBonus { get; set; }
        public int VitalityBonus { get; set; }
        public int EnduranceBonus { get; set; }
        
        // Tiempo
        public DateTime AssignedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? CompletionTime { get; set; }
        public int EstimatedTimeMinutes { get; set; }
        
        // Metadatos
        public DateTime QuestDate { get; set; }
        public string RelativeTime { get; set; } = string.Empty;
        public bool IsFromToday { get; set; }
    }

    // DTO para iniciar un quest
    public class StartQuestDto
    {
        public Guid AssignmentID { get; set; }
    }

    // DTO para actualizar progreso de quest
    public class UpdateQuestProgressDto
    {
        public Guid AssignmentID { get; set; }
        public int? CurrentReps { get; set; }
        public int? CurrentSets { get; set; }
        public int? CurrentDuration { get; set; }
        public decimal? CurrentDistance { get; set; }
    }

    // DTO para completar quest
    public class CompleteQuestDto
    {
        public Guid AssignmentID { get; set; }
        public int? FinalReps { get; set; }
        public int? FinalSets { get; set; }
        public int? FinalDuration { get; set; }
        public decimal? FinalDistance { get; set; }
        public bool PerfectExecution { get; set; } = false;
    }

    // DTO para respuesta de operaciones de quest
    public class QuestOperationResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public HunterDailyQuestDto? Quest { get; set; } // Quest afectado
        
        // Información de XP y Nivel del Hunter
        public int? XPEarned { get; set; }
        public bool LeveledUp { get; set; } = false;
        public int? NewLevel { get; set; }
        public int? NewCurrentXP { get; set; }
        public int? NewXPRequiredForNextLevel { get; set; }
        public string? NewRank { get; set; }
        public decimal? NewLevelProgressPercentage { get; set; }

        public List<string> AchievementsUnlocked { get; set; } = new();
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    // DTO para resumen de quests diarias
    public class DailyQuestsSummaryDto
    {
        public DateTime QuestDate { get; set; }
        public int TotalQuests { get; set; }
        public int CompletedQuests { get; set; }
        public int InProgressQuests { get; set; }
        public int PendingQuests { get; set; }
        public decimal OverallProgress { get; set; }
        public int TotalXPEarned { get; set; }
        public int TotalXPAvailable { get; set; }
        public List<HunterDailyQuestDto> Quests { get; set; } = new();
        public bool CanGenerateNewQuests { get; set; }
        public string ProgressMessage { get; set; } = string.Empty;
        public string MotivationalMessage { get; set; } = string.Empty;
    }

    // DTO para historial de quest completada
    public class QuestHistoryDto
    {
        public Guid HistoryID { get; set; }
        public Guid QuestID { get; set; }
        public string QuestName { get; set; } = string.Empty;
        public string QuestType { get; set; } = string.Empty;
        public string ExerciseName { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string DifficultyColor { get; set; } = string.Empty;
        
        // Resultados
        public DateTime CompletedAt { get; set; }
        public int XPEarned { get; set; }
        public string CompletionTime { get; set; } = string.Empty;
        public bool PerfectExecution { get; set; }
        public decimal BonusMultiplier { get; set; }
        public string PerformanceRating { get; set; } = string.Empty;
        public string PerformanceColor { get; set; } = string.Empty;
        
        // Estadísticas finales
        public int? FinalReps { get; set; }
        public int? FinalSets { get; set; }
        public int? FinalDuration { get; set; }
        public decimal? FinalDistance { get; set; }
        public string StatsDescription { get; set; } = string.Empty;
        
        // Contexto temporal
        public string RelativeTime { get; set; } = string.Empty;
        public bool IsFromToday { get; set; }
        public bool IsFromThisWeek { get; set; }
        public bool IsPersonalBest { get; set; }
        public bool WasFasterThanEstimated { get; set; }
    }

    // DTO para generar nuevas quests diarias
    public class GenerateDailyQuestsDto
    {
        public Guid HunterID { get; set; }
        public DateTime QuestDate { get; set; }
        public int NumberOfQuests { get; set; } = 3;
        public List<string> PreferredTypes { get; set; } = new(); // Tipos preferidos
        public string? DifficultyPreference { get; set; } // Easy, Medium, Hard, Auto
    }

    // DTO para estadísticas de quest
    public class QuestStatsDto
    {
        public Guid HunterID { get; set; }
        
        // Estadísticas generales
        public int TotalQuestsCompleted { get; set; }
        public int TotalXPFromQuests { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        
        // Por período
        public int QuestsCompletedToday { get; set; }
        public int QuestsCompletedThisWeek { get; set; }
        public int QuestsCompletedThisMonth { get; set; }
        
        // Por tipo de quest
        public Dictionary<string, int> QuestsByType { get; set; } = new();
        public Dictionary<string, int> XPByType { get; set; } = new();
        public Dictionary<string, double> AverageTimeByType { get; set; } = new();
        
        // Por dificultad
        public Dictionary<string, int> QuestsByDifficulty { get; set; } = new();
        public Dictionary<string, decimal> AveragePerformanceByDifficulty { get; set; } = new();
        
        // Mejores marcas
        public List<PersonalBestDto> PersonalBests { get; set; } = new();
        
        // Tendencias
        public List<QuestTrendDto> WeeklyTrends { get; set; } = new();
        public double ProgressTrend { get; set; } // Positivo = mejorando, negativo = empeorando
    }

    // DTO para récord personal
    public class PersonalBestDto
    {
        public Guid QuestID { get; set; }
        public string QuestName { get; set; } = string.Empty;
        public string QuestType { get; set; } = string.Empty;
        public string Achievement { get; set; } = string.Empty; // "Fastest Time", "Most Reps", etc.
        public string Value { get; set; } = string.Empty;
        public DateTime AchievedAt { get; set; }
        public string RelativeTime { get; set; } = string.Empty;
    }

    // DTO para tendencia de quest
    public class QuestTrendDto
    {
        public DateTime WeekStart { get; set; }
        public int QuestsCompleted { get; set; }
        public int XPEarned { get; set; }
        public decimal AveragePerformance { get; set; }
        public string WeekLabel { get; set; } = string.Empty;
    }

    // DTO para quest disponible (template)
    public class AvailableQuestDto
    {
        public Guid QuestID { get; set; }
        public string QuestName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string QuestType { get; set; } = string.Empty;
        public string QuestTypeIcon { get; set; } = string.Empty;
        public string ExerciseName { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string DifficultyColor { get; set; } = string.Empty;
        
        // Objetivos
        public int? TargetReps { get; set; }
        public int? TargetSets { get; set; }
        public int? TargetDuration { get; set; }
        public decimal? TargetDistance { get; set; }
        
        // Recompensas
        public int BaseXPReward { get; set; }
        public int ScaledXPReward { get; set; }
        public int StrengthBonus { get; set; }
        public int AgilityBonus { get; set; }
        public int VitalityBonus { get; set; }
        public int EnduranceBonus { get; set; }
        
        // Requisitos
        public int MinLevel { get; set; }
        public string MinRank { get; set; } = string.Empty;
        public bool IsEligible { get; set; }
        public string? IneligibilityReason { get; set; }
        
        // Estimaciones
        public int EstimatedTimeMinutes { get; set; }
        public string EstimatedTimeText { get; set; } = string.Empty;
    }
}