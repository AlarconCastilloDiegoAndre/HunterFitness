namespace HunterFitness.API.DTOs
{
    // ========================
    // EQUIPMENT DTOs (PRIMERO)
    // ========================

    // DTO para item del hunter
    public class HunterEquipmentDto
    {
        public Guid HunterEquipmentID { get; set; }
        public Guid EquipmentID { get; set; }
        
        // Información del item
        public string ItemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public string ItemTypeIcon { get; set; } = string.Empty;
        public string Rarity { get; set; } = string.Empty;
        public string RarityColor { get; set; } = string.Empty;
        public string RarityStars { get; set; } = string.Empty;
        
        // Stats
        public int StrengthBonus { get; set; }
        public int AgilityBonus { get; set; }
        public int VitalityBonus { get; set; }
        public int EnduranceBonus { get; set; }
        public int TotalStatBonus { get; set; }
        public decimal XPMultiplier { get; set; }
        public string StatBonusDescription { get; set; } = string.Empty;
        public int PowerScore { get; set; }
        
        // Estado
        public bool IsEquipped { get; set; }
        public bool CanBeEquipped { get; set; }
        public string EquipmentStatusText { get; set; } = string.Empty;
        public DateTime UnlockedAt { get; set; }
        public string TimeOwned { get; set; } = string.Empty;
        public bool IsNewlyUnlocked { get; set; }
        public bool ShouldShowEquipmentGlow { get; set; }
        
        // Metadatos
        public string? IconUrl { get; set; }
        public List<string> SpecialEffects { get; set; } = new();
        public string FlavorText { get; set; } = string.Empty;
    }

    // DTO para item de equipamiento
    public class EquipmentDto
    {
        public Guid EquipmentID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public string ItemTypeIcon { get; set; } = string.Empty;
        public string ItemTypeDisplayName { get; set; } = string.Empty;
        
        // Rareza
        public string Rarity { get; set; } = string.Empty;
        public string RarityDisplayName { get; set; } = string.Empty;
        public string RarityColor { get; set; } = string.Empty;
        public string RarityStars { get; set; } = string.Empty;
        public int RarityValue { get; set; }
        
        // Stats
        public int StrengthBonus { get; set; }
        public int AgilityBonus { get; set; }
        public int VitalityBonus { get; set; }
        public int EnduranceBonus { get; set; }
        public int TotalStatBonus { get; set; }
        public decimal XPMultiplier { get; set; }
        public string StatBonusDescription { get; set; } = string.Empty;
        
        // Requisitos
        public int UnlockLevel { get; set; }
        public string UnlockRank { get; set; } = string.Empty;
        public string? UnlockCondition { get; set; }
        public string UnlockRequirementText { get; set; } = string.Empty;
        public bool IsEligible { get; set; }
        public string? IneligibilityReason { get; set; }
        
        // Información adicional
        public int PowerLevel { get; set; }
        public bool HasXPBonus { get; set; }
        public bool HasStatBonuses { get; set; }
        public bool IsHighTier { get; set; }
        public string FlavorText { get; set; } = string.Empty;
        public List<string> SpecialEffects { get; set; } = new();
        
        // Estado para el hunter
        public bool IsOwned { get; set; }
        public bool IsEquipped { get; set; }
        public DateTime? UnlockedAt { get; set; }
        public bool IsNewlyUnlocked { get; set; }
        public bool ShouldShowGlow { get; set; }
        
        // Metadatos
        public string? IconUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // DTO para inventario del hunter
    public class HunterInventoryDto
    {
        public Guid HunterID { get; set; }
        public string HunterName { get; set; } = string.Empty;
        
        // Equipamiento actual
        public List<HunterEquipmentDto> EquippedItems { get; set; } = new();
        
        // Inventario completo
        public List<HunterEquipmentDto> AllItems { get; set; } = new();
        
        // Por tipo
        public List<HunterEquipmentDto> Weapons { get; set; } = new();
        public List<HunterEquipmentDto> Armor { get; set; } = new();
        public List<HunterEquipmentDto> Accessories { get; set; } = new();
        
        // Estadísticas del inventario
        public int TotalItems { get; set; }
        public int EquippedItemsCount { get; set; }
        public int UnequippedItems { get; set; }
        public Dictionary<string, int> ItemsByRarity { get; set; } = new();
        public Dictionary<string, int> ItemsByType { get; set; } = new();
        
        // Bonificaciones totales del equipamiento
        public int TotalStrengthBonus { get; set; }
        public int TotalAgilityBonus { get; set; }
        public int TotalVitalityBonus { get; set; }
        public int TotalEnduranceBonus { get; set; }
        public decimal TotalXPMultiplier { get; set; }
        public int TotalPowerLevel { get; set; }
        
        // Items recientes
        public List<HunterEquipmentDto> RecentlyUnlocked { get; set; } = new();
    }

    // DTO para equipar/desequipar item
    public class EquipItemDto
    {
        public Guid HunterEquipmentID { get; set; }
        public bool Equip { get; set; } = true; // true = equipar, false = desequipar
    }

    // DTO para respuesta de operaciones de equipment
    public class EquipmentOperationResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public HunterEquipmentDto? Equipment { get; set; }
        public HunterStatsDto? UpdatedStats { get; set; }
        public List<HunterEquipmentDto> AffectedItems { get; set; } = new(); // Items que se desequiparon
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    // ========================
    // DUNGEON DTOs
    // ========================

    // DTO para ejercicio de dungeon
    public class DungeonExerciseDto
    {
        public Guid ExerciseID { get; set; }
        public int ExerciseOrder { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        // Objetivos
        public int? TargetReps { get; set; }
        public int? TargetSets { get; set; }
        public int? TargetDuration { get; set; }
        public int RestTimeSeconds { get; set; }
        
        // Información calculada
        public string TargetDescription { get; set; } = string.Empty;
        public string RestTimeDescription { get; set; } = string.Empty;
        public string EstimatedTimeDescription { get; set; } = string.Empty;
        public string ExerciseType { get; set; } = string.Empty;
        public string DifficultyEstimate { get; set; } = string.Empty;
        public string DifficultyColor { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
    }

    // DTO para dungeon disponible
    public class DungeonDto
    {
        public Guid DungeonID { get; set; }
        public string DungeonName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DungeonType { get; set; } = string.Empty;
        public string TypeIcon { get; set; } = string.Empty;
        public string TypeDescription { get; set; } = string.Empty;
        
        // Dificultad
        public string Difficulty { get; set; } = string.Empty;
        public string DifficultyColor { get; set; } = string.Empty;
        public string DifficultyDisplayName { get; set; } = string.Empty;
        public int DifficultyValue { get; set; }
        
        // Requisitos
        public int MinLevel { get; set; }
        public string MinRank { get; set; } = string.Empty;
        public string RequirementsText { get; set; } = string.Empty;
        public bool IsEligible { get; set; }
        public string? IneligibilityReason { get; set; }
        
        // Configuración
        public int EstimatedDuration { get; set; }
        public string EstimatedTimeText { get; set; } = string.Empty;
        public int EnergyCost { get; set; }
        public int CooldownHours { get; set; }
        public string CooldownText { get; set; } = string.Empty;
        
        // Recompensas
        public int BaseXPReward { get; set; }
        public int BonusXPReward { get; set; }
        public int TotalXPReward { get; set; }
        public int ScaledXPReward { get; set; }
        public List<string> Rewards { get; set; } = new();
        
        // Estadísticas
        public int ExerciseCount { get; set; }
        public bool IsBossRaid { get; set; }
        public bool IsHighDifficulty { get; set; }
        public string RecommendedStats { get; set; } = string.Empty;
        
        // Estado para el hunter
        public bool CanStart { get; set; }
        public DateTime? NextAvailableAt { get; set; }
        public string? CooldownRemaining { get; set; }
        public double SuccessRateEstimate { get; set; }
        public string WarningText { get; set; } = string.Empty;
        
        // Ejercicios
        public List<DungeonExerciseDto> Exercises { get; set; } = new();
    }

    // DTO para raid de dungeon
    public class DungeonRaidDto
    {
        public Guid RaidID { get; set; }
        public Guid DungeonID { get; set; }
        public string DungeonName { get; set; } = string.Empty;
        public string DungeonType { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        
        // Estado
        public string Status { get; set; } = string.Empty;
        public string StatusDisplayName { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public decimal Progress { get; set; }
        public double ProgressPercentage { get; set; }
        
        // Resultados
        public int? TotalDuration { get; set; }
        public string? FormattedDuration { get; set; }
        public int XPEarned { get; set; }
        public decimal CompletionRate { get; set; }
        
        // Tiempo
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? NextAvailableAt { get; set; }
        public string? RemainingCooldown { get; set; }
        
        // Estado actual
        public bool CanRestart { get; set; }
        public bool IsEligibleForBonusReward { get; set; }
        public string RelativeTime { get; set; } = string.Empty;
    }

    // DTO para iniciar raid
    public class StartRaidDto
    {
        public Guid DungeonID { get; set; }
    }

    // DTO para actualizar progreso de raid
    public class UpdateRaidProgressDto
    {
        public Guid RaidID { get; set; }
        public decimal Progress { get; set; }
        public Dictionary<string, object> ExerciseProgress { get; set; } = new();
    }

    // DTO para completar raid
    public class CompleteRaidDto
    {
        public Guid RaidID { get; set; }
        public bool Successful { get; set; } = true;
        public Dictionary<string, object> FinalResults { get; set; } = new();
    }

    // ========================
    // ACHIEVEMENT DTOs
    // ========================

    // DTO para achievement
    public class AchievementDto
    {
        public Guid AchievementID { get; set; }
        public string AchievementName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string CategoryDisplayName { get; set; } = string.Empty;
        public string CategoryDescription { get; set; } = string.Empty;
        public string CategoryIcon { get; set; } = string.Empty;
        
        // Configuración
        public int? TargetValue { get; set; }
        public string AchievementType { get; set; } = string.Empty;
        public string TypeDescription { get; set; } = string.Empty;
        public bool IsHidden { get; set; }
        
        // Recompensas
        public int XPReward { get; set; }
        public string? TitleReward { get; set; }
        
        // Dificultad
        public string DifficultyLevel { get; set; } = string.Empty;
        public string RarityColor { get; set; } = string.Empty;
        public int EstimatedDaysToComplete { get; set; }
        public string ProgressHint { get; set; } = string.Empty;
        
        // Estado para el hunter
        public int CurrentProgress { get; set; }
        public bool IsUnlocked { get; set; }
        public DateTime? UnlockedAt { get; set; }
        public decimal ProgressPercentage { get; set; }
        public int RemainingProgress { get; set; }
        public string ProgressDescription { get; set; } = string.Empty;
        public string UnlockStatusText { get; set; } = string.Empty;
        public bool IsRecentlyUnlocked { get; set; }
        public bool CanBeUnlocked { get; set; }
        
        // Metadatos
        public string? IconUrl { get; set; }
        public bool RequiresSpecialConditions { get; set; }
        public List<string> RelatedAchievements { get; set; } = new();
        public string CompletionCelebrationMessage { get; set; } = string.Empty;
    }

    // DTO para listado de achievements del hunter
    public class HunterAchievementsDto
    {
        public Guid HunterID { get; set; }
        public string HunterName { get; set; } = string.Empty;
        
        // Achievements por estado
        public List<AchievementDto> UnlockedAchievements { get; set; } = new();
        public List<AchievementDto> InProgressAchievements { get; set; } = new();
        public List<AchievementDto> AvailableAchievements { get; set; } = new();
        public List<AchievementDto> HiddenAchievements { get; set; } = new();
        
        // Por categoría
        public Dictionary<string, List<AchievementDto>> AchievementsByCategory { get; set; } = new();
        
        // Estadísticas
        public int TotalAchievements { get; set; }
        public int UnlockedCount { get; set; }
        public int InProgressCount { get; set; }
        public decimal CompletionPercentage { get; set; }
        public int TotalXPFromAchievements { get; set; }
        
        // Recientes
        public List<AchievementDto> RecentlyUnlocked { get; set; } = new();
        public List<AchievementDto> NearCompletion { get; set; } = new();
        
        // Títulos desbloqueados
        public List<string> UnlockedTitles { get; set; } = new();
    }

    // ========================
    // RESPONSE DTOs GENERALES
    // ========================

    // DTO para respuesta de API estándar
    public class ApiResponseDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // DTO para respuesta paginada
    public class PaginatedResponseDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    // DTO para parámetros de paginación
    public class PaginationDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; } = "asc";
        public string? Filter { get; set; }
    }
}