-- ===================================================
-- HUNTER FITNESS - SCHEMA COMPLETO DE BASE DE DATOS
-- Inspirado en Solo Leveling
-- ===================================================

-- 1. TABLA HUNTERS (Usuarios/Perfiles principales)
CREATE TABLE Hunters (
    HunterID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    HunterName NVARCHAR(100) NOT NULL,
    
    -- Stats del Hunter (Solo Leveling style)
    Level INT DEFAULT 1,
    CurrentXP INT DEFAULT 0,
    TotalXP INT DEFAULT 0,
    HunterRank NVARCHAR(10) DEFAULT 'E' CHECK (HunterRank IN ('E', 'D', 'C', 'B', 'A', 'S', 'SS', 'SSS')),
    
    -- Stats principales
    Strength INT DEFAULT 10,
    Agility INT DEFAULT 10,
    Vitality INT DEFAULT 10,
    Endurance INT DEFAULT 10,
    
    -- Progreso y streaks
    DailyStreak INT DEFAULT 0,
    LongestStreak INT DEFAULT 0,
    TotalWorkouts INT DEFAULT 0,
    
    -- Metadatos
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    LastLoginAt DATETIME2,
    IsActive BIT DEFAULT 1,
    ProfilePictureUrl NVARCHAR(500)
);

-- 2. TABLA DAILY QUESTS (Templates de quests diarias)
CREATE TABLE DailyQuests (
    QuestID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    QuestName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    QuestType NVARCHAR(20) NOT NULL CHECK (QuestType IN ('Cardio', 'Strength', 'Flexibility', 'Endurance', 'Mixed')),
    
    -- Configuración del ejercicio
    ExerciseName NVARCHAR(100) NOT NULL,
    TargetReps INT,
    TargetSets INT,
    TargetDuration INT, -- en segundos
    TargetDistance DECIMAL(10,2), -- en metros
    
    -- Recompensas y dificultad
    Difficulty NVARCHAR(10) CHECK (Difficulty IN ('Easy', 'Medium', 'Hard', 'Extreme')),
    BaseXPReward INT NOT NULL,
    StrengthBonus INT DEFAULT 0,
    AgilityBonus INT DEFAULT 0,
    VitalityBonus INT DEFAULT 0,
    EnduranceBonus INT DEFAULT 0,
    
    -- Requisitos
    MinLevel INT DEFAULT 1,
    MinRank NVARCHAR(10) DEFAULT 'E',
    
    -- Metadatos
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- 3. TABLA HUNTER DAILY QUESTS (Asignaciones específicas de quests)
CREATE TABLE HunterDailyQuests (
    AssignmentID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    HunterID UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Hunters(HunterID),
    QuestID UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES DailyQuests(QuestID),
    
    -- Estado y progreso
    Status NVARCHAR(20) DEFAULT 'Assigned' CHECK (Status IN ('Assigned', 'InProgress', 'Completed', 'Failed')),
    Progress DECIMAL(5,2) DEFAULT 0.00, -- Porcentaje 0-100
    
    -- Valores actuales del quest
    CurrentReps INT DEFAULT 0,
    CurrentSets INT DEFAULT 0,
    CurrentDuration INT DEFAULT 0,
    CurrentDistance DECIMAL(10,2) DEFAULT 0,
    
    -- Recompensas obtenidas
    XPEarned INT DEFAULT 0,
    BonusMultiplier DECIMAL(3,2) DEFAULT 1.00, -- Para bonificaciones por velocidad/perfección
    
    -- Timestamps
    AssignedAt DATETIME2 DEFAULT GETUTCDATE(),
    StartedAt DATETIME2,
    CompletedAt DATETIME2,
    QuestDate DATE DEFAULT CAST(GETUTCDATE() AS DATE)
);

-- 4. TABLA DUNGEONS (Mazmorras/Raids)
CREATE TABLE Dungeons (
    DungeonID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DungeonName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    DungeonType NVARCHAR(30) CHECK (DungeonType IN ('Training Grounds', 'Strength Trial', 'Endurance Test', 'Boss Raid')),
    
    -- Configuración de dificultad
    Difficulty NVARCHAR(10) CHECK (Difficulty IN ('Normal', 'Hard', 'Extreme', 'Nightmare')),
    MinLevel INT NOT NULL,
    MinRank NVARCHAR(10) NOT NULL,
    EstimatedDuration INT NOT NULL, -- en minutos
    
    -- Costos y cooldowns
    EnergyCost INT DEFAULT 10,
    CooldownHours INT DEFAULT 24,
    
    -- Recompensas
    BaseXPReward INT NOT NULL,
    BonusXPReward INT DEFAULT 0,
    
    -- Metadatos
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- 5. TABLA DUNGEON EXERCISES (Ejercicios dentro de dungeons)
CREATE TABLE DungeonExercises (
    ExerciseID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DungeonID UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Dungeons(DungeonID),
    ExerciseOrder INT NOT NULL,
    
    -- Detalles del ejercicio
    ExerciseName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    TargetReps INT,
    TargetSets INT,
    TargetDuration INT,
    RestTimeSeconds INT DEFAULT 30,
    
    -- Metadatos
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- 6. TABLA DUNGEON RAIDS (Intentos de dungeons por hunters)
CREATE TABLE DungeonRaids (
    RaidID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    HunterID UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Hunters(HunterID),
    DungeonID UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Dungeons(DungeonID),
    
    -- Estado del raid
    Status NVARCHAR(20) DEFAULT 'Started' CHECK (Status IN ('Started', 'InProgress', 'Completed', 'Failed', 'Abandoned')),
    Progress DECIMAL(5,2) DEFAULT 0.00,
    
    -- Resultados
    TotalDuration INT, -- en segundos
    XPEarned INT DEFAULT 0,
    CompletionRate DECIMAL(5,2) DEFAULT 0.00,
    
    -- Timestamps
    StartedAt DATETIME2 DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2,
    NextAvailableAt DATETIME2
);

-- 7. TABLA ACHIEVEMENTS (Logros del sistema)
CREATE TABLE Achievements (
    AchievementID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    AchievementName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    Category NVARCHAR(30) CHECK (Category IN ('Consistency', 'Strength', 'Endurance', 'Social', 'Special', 'Milestone')),
    
    -- Configuración del logro
    TargetValue INT, -- Valor objetivo (ej: 100 workouts)
    AchievementType NVARCHAR(20) CHECK (AchievementType IN ('Counter', 'Streak', 'Single', 'Progressive')),
    IsHidden BIT DEFAULT 0, -- Para achievements secretos
    
    -- Recompensas
    XPReward INT DEFAULT 0,
    TitleReward NVARCHAR(50),
    
    -- Metadatos
    IconUrl NVARCHAR(500),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- 8. TABLA HUNTER ACHIEVEMENTS (Logros desbloqueados)
CREATE TABLE HunterAchievements (
    HunterAchievementID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    HunterID UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Hunters(HunterID),
    AchievementID UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Achievements(AchievementID),
    
    -- Progreso
    CurrentProgress INT DEFAULT 0,
    IsUnlocked BIT DEFAULT 0,
    UnlockedAt DATETIME2,
    
    -- Metadatos
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    UNIQUE(HunterID, AchievementID)
);

-- 9. TABLA EQUIPMENT (Catálogo de equipamiento)
CREATE TABLE Equipment (
    EquipmentID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ItemName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    ItemType NVARCHAR(20) CHECK (ItemType IN ('Weapon', 'Armor', 'Accessory')),
    Rarity NVARCHAR(15) CHECK (Rarity IN ('Common', 'Rare', 'Epic', 'Legendary', 'Mythic')),
    
    -- Bonificaciones de stats
    StrengthBonus INT DEFAULT 0,
    AgilityBonus INT DEFAULT 0,
    VitalityBonus INT DEFAULT 0,
    EnduranceBonus INT DEFAULT 0,
    XPMultiplier DECIMAL(3,2) DEFAULT 1.00,
    
    -- Requisitos para desbloquear
    UnlockLevel INT DEFAULT 1,
    UnlockRank NVARCHAR(10) DEFAULT 'E',
    UnlockCondition NVARCHAR(500), -- Descripción de cómo desbloquear
    
    -- Metadatos
    IconUrl NVARCHAR(500),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- 10. TABLA HUNTER EQUIPMENT (Inventario y equipamiento)
CREATE TABLE HunterEquipment (
    HunterEquipmentID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    HunterID UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Hunters(HunterID),
    EquipmentID UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Equipment(EquipmentID),
    
    -- Estado del item
    IsEquipped BIT DEFAULT 0,
    UnlockedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    -- Metadatos
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    UNIQUE(HunterID, EquipmentID)
);

-- 11. TABLA GUILDS (Gremios/Grupos)
CREATE TABLE Guilds (
    GuildID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    GuildName NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(1000),
    GuildMotto NVARCHAR(200),
    
    -- Configuración
    MaxMembers INT DEFAULT 50,
    CurrentMembers INT DEFAULT 0,
    IsPublic BIT DEFAULT 1,
    JoinRequiresApproval BIT DEFAULT 0,
    
    -- Stats del guild
    TotalGuildXP BIGINT DEFAULT 0,
    GuildLevel INT DEFAULT 1,
    GuildRank INT, -- Ranking global
    
    -- Metadatos
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Hunters(HunterID),
    IconUrl NVARCHAR(500),
    IsActive BIT DEFAULT 1
);

-- 12. TABLA GUILD MEMBERS (Miembros de gremios)
CREATE TABLE GuildMembers (
    MembershipID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    GuildID UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Guilds(GuildID),
    HunterID UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Hunters(HunterID),
    
    -- Rol en el guild
    Role NVARCHAR(20) DEFAULT 'Member' CHECK (Role IN ('Member', 'Officer', 'Leader')),
    
    -- Contribuciones
    ContributedXP BIGINT DEFAULT 0,
    JoinedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    -- Estado
    IsActive BIT DEFAULT 1,
    
    UNIQUE(HunterID) -- Un hunter solo puede estar en un guild
);

-- 13. TABLA QUEST HISTORY (Historial de quests completadas)
CREATE TABLE QuestHistory (
    HistoryID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    HunterID UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Hunters(HunterID),
    QuestID UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES DailyQuests(QuestID),
    
    -- Detalles de la completación
    CompletedAt DATETIME2 NOT NULL,
    XPEarned INT NOT NULL,
    CompletionTime INT, -- Tiempo en segundos
    PerfectExecution BIT DEFAULT 0,
    BonusMultiplier DECIMAL(3,2) DEFAULT 1.00,
    
    -- Estadísticas
    FinalReps INT,
    FinalSets INT,
    FinalDuration INT,
    FinalDistance DECIMAL(10,2),
    
    -- Metadatos
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- 14. TABLA LEADERBOARD CACHE (Cache de rankings)
CREATE TABLE LeaderboardCache (
    CacheID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    LeaderboardType NVARCHAR(30) CHECK (LeaderboardType IN ('Global', 'Weekly', 'Monthly', 'Guild')),
    HunterID UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Hunters(HunterID),
    
    -- Posición y stats
    Rank INT NOT NULL,
    Score BIGINT NOT NULL, -- XP, streaks, etc.
    
    -- Período
    PeriodStart DATE,
    PeriodEnd DATE,
    
    -- Metadatos
    LastUpdated DATETIME2 DEFAULT GETUTCDATE(),
    
    INDEX IX_LeaderboardCache_Type_Rank (LeaderboardType, Rank)
);

-- ===================================================
-- ÍNDICES PARA OPTIMIZACIÓN
-- ===================================================

-- Índices para Hunters
CREATE INDEX IX_Hunters_Username ON Hunters(Username);
CREATE INDEX IX_Hunters_Email ON Hunters(Email);
CREATE INDEX IX_Hunters_Level ON Hunters(Level);
CREATE INDEX IX_Hunters_HunterRank ON Hunters(HunterRank);

-- Índices para HunterDailyQuests
CREATE INDEX IX_HunterDailyQuests_HunterID_Date ON HunterDailyQuests(HunterID, QuestDate);
CREATE INDEX IX_HunterDailyQuests_Status ON HunterDailyQuests(Status);

-- Índices para DungeonRaids
CREATE INDEX IX_DungeonRaids_HunterID ON DungeonRaids(HunterID);
CREATE INDEX IX_DungeonRaids_Status ON DungeonRaids(Status);

-- Índices para QuestHistory
CREATE INDEX IX_QuestHistory_HunterID_CompletedAt ON QuestHistory(HunterID, CompletedAt);

-- ===================================================
-- FIN DEL SCHEMA
-- ===================================================

PRINT 'Hunter Fitness Database Schema creado exitosamente! 🏹⚔️';
PRINT 'Total de tablas creadas: 14';
PRINT 'Listo para comenzar tu aventura como Hunter!';
