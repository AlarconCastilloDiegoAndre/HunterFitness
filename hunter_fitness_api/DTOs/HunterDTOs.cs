namespace HunterFitness.API.DTOs
{
    // DTO para registro de nuevo hunter
    public class RegisterHunterDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string HunterName { get; set; } = string.Empty;
    }

    // DTO para login
    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // DTO para respuesta de autenticación
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public HunterProfileDto? Hunter { get; set; }
    }

    // DTO completo del perfil del hunter
    public class HunterProfileDto
    {
        public Guid HunterID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string HunterName { get; set; } = string.Empty;
        
        // Stats del Hunter
        public int Level { get; set; }
        public int CurrentXP { get; set; }
        public int TotalXP { get; set; }
        public string HunterRank { get; set; } = string.Empty;
        public string RankDisplayName { get; set; } = string.Empty;
        
        // Stats principales
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Vitality { get; set; }
        public int Endurance { get; set; }
        public int TotalStats { get; set; }
        
        // Progreso
        public int DailyStreak { get; set; }
        public int LongestStreak { get; set; }
        public int TotalWorkouts { get; set; }
        
        // Progreso de level
        public int XPRequiredForNextLevel { get; set; }
        public decimal LevelProgressPercentage { get; set; }
        public bool CanLevelUp { get; set; }
        public string NextRankRequirement { get; set; } = string.Empty;
        
        // Metadatos
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? ProfilePictureUrl { get; set; }
        
        // Equipamiento actual
        public List<EquippedItemDto> EquippedItems { get; set; } = new();
        
        // Stats adicionales
        public Dictionary<string, object> AdditionalStats { get; set; } = new();
    }

    // DTO simplificado para listas y rankings
    public class HunterSummaryDto
    {
        public Guid HunterID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string HunterName { get; set; } = string.Empty;
        public int Level { get; set; }
        public int TotalXP { get; set; }
        public string HunterRank { get; set; } = string.Empty;
        public string RankDisplayName { get; set; } = string.Empty;
        public int DailyStreak { get; set; }
        public int TotalWorkouts { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsOnline { get; set; }
    }

    // DTO para actualizar perfil
    public class UpdateHunterProfileDto
    {
        public string? HunterName { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }

    // DTO para stats del hunter
    public class HunterStatsDto
    {
        public Guid HunterID { get; set; }
        public string HunterName { get; set; } = string.Empty;
        public int Level { get; set; }
        public string HunterRank { get; set; } = string.Empty;
        
        // Stats base
        public int BaseStrength { get; set; }
        public int BaseAgility { get; set; }  
        public int BaseVitality { get; set; }
        public int BaseEndurance { get; set; }
        
        // Stats con equipamiento
        public int TotalStrength { get; set; }
        public int TotalAgility { get; set; }
        public int TotalVitality { get; set; }
        public int TotalEndurance { get; set; }
        
        // Bonificaciones de equipamiento
        public int StrengthBonus { get; set; }
        public int AgilityBonus { get; set; }
        public int VitalityBonus { get; set; }
        public int EnduranceBonus { get; set; }
        
        public int TotalStatsBase { get; set; }
        public int TotalStatsWithEquipment { get; set; }
        public decimal XPMultiplier { get; set; }
    }

    // DTO para item equipado
    public class EquippedItemDto
    {
        public Guid EquipmentID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public string Rarity { get; set; } = string.Empty;
        public string RarityColor { get; set; } = string.Empty;
        public int StrengthBonus { get; set; }
        public int AgilityBonus { get; set; }
        public int VitalityBonus { get; set; }
        public int EnduranceBonus { get; set; }
        public decimal XPMultiplier { get; set; }
        public string StatBonusDescription { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public int PowerLevel { get; set; }
    }

    // DTO para ranking/leaderboard
    public class LeaderboardEntryDto
    {
        public int Rank { get; set; }
        public Guid HunterID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string HunterName { get; set; } = string.Empty;
        public int Level { get; set; }
        public int TotalXP { get; set; }
        public string HunterRank { get; set; } = string.Empty;
        public string RankDisplayName { get; set; } = string.Empty;
        public int DailyStreak { get; set; }
        public int LongestStreak { get; set; }
        public int TotalWorkouts { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsCurrentUser { get; set; }
        public string RankChange { get; set; } = "="; // ↑ ↓ =
    }

    // DTO para respuestas de operaciones de hunter
    public class HunterOperationResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public HunterProfileDto? Hunter { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }

    // DTO para cambio de contraseña
    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // DTO para progreso general del hunter
    public class HunterProgressDto
    {
        public Guid HunterID { get; set; }
        public string HunterName { get; set; } = string.Empty;
        
        // Progreso de nivel
        public int CurrentLevel { get; set; }
        public int CurrentXP { get; set; }
        public int XPRequiredForNextLevel { get; set; }
        public decimal LevelProgressPercentage { get; set; }
        
        // Progreso de rank
        public string CurrentRank { get; set; } = string.Empty;
        public string NextRank { get; set; } = string.Empty;
        public int LevelRequiredForNextRank { get; set; }
        
        // Estadísticas de actividad
        public int TodayWorkouts { get; set; }
        public int WeeklyWorkouts { get; set; }
        public int MonthlyWorkouts { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        
        // Logros recientes
        public List<RecentAchievementDto> RecentAchievements { get; set; } = new();
        
        // Progreso de stats
        public Dictionary<string, int> StatGrowthThisWeek { get; set; } = new();
        public Dictionary<string, int> StatGrowthThisMonth { get; set; } = new();
    }

    // DTO para logro reciente
    public class RecentAchievementDto
    {
        public Guid AchievementID { get; set; }
        public string AchievementName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int XPReward { get; set; }
        public string? TitleReward { get; set; }
        public DateTime UnlockedAt { get; set; }
        public string? IconUrl { get; set; }
        public string RarityColor { get; set; } = string.Empty;
        public bool IsNew { get; set; }
    }
}