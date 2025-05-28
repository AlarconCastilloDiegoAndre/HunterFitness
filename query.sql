-- ===================================================
-- HUNTER FITNESS - SETUP COMPLETO (TABLAS + DATOS)
-- ===================================================

PRINT '🏹 Iniciando setup completo de Hunter Fitness...'

-- ===================================================
-- CREAR TABLAS
-- ===================================================

-- 1. TABLA HUNTERS
CREATE TABLE Hunters (
    HunterID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    HunterName NVARCHAR(100) NOT NULL,
    
    -- Stats del Hunter
    Level INT DEFAULT 1,
    CurrentXP INT DEFAULT 0,
    TotalXP INT DEFAULT 0,
    HunterRank NVARCHAR(10) DEFAULT 'E',
    
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

-- 2. TABLA DAILY QUESTS
CREATE TABLE DailyQuests (
    QuestID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    QuestName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    QuestType NVARCHAR(20) NOT NULL,
    
    -- Configuración del ejercicio
    ExerciseName NVARCHAR(100) NOT NULL,
    TargetReps INT,
    TargetSets INT,
    TargetDuration INT, -- en segundos
    TargetDistance DECIMAL(10,2), -- en metros
    
    -- Recompensas y dificultad
    Difficulty NVARCHAR(10) DEFAULT 'Easy',
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

-- 3. TABLA HUNTER DAILY QUESTS
CREATE TABLE HunterDailyQuests (
    AssignmentID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    HunterID UNIQUEIDENTIFIER NOT NULL,
    QuestID UNIQUEIDENTIFIER NOT NULL,
    
    -- Estado y progreso
    Status NVARCHAR(20) DEFAULT 'Assigned',
    Progress DECIMAL(5,2) DEFAULT 0.00,
    
    -- Valores actuales del quest
    CurrentReps INT DEFAULT 0,
    CurrentSets INT DEFAULT 0,
    CurrentDuration INT DEFAULT 0,
    CurrentDistance DECIMAL(10,2) DEFAULT 0,
    
    -- Recompensas obtenidas
    XPEarned INT DEFAULT 0,
    BonusMultiplier DECIMAL(3,2) DEFAULT 1.00,
    
    -- Timestamps
    AssignedAt DATETIME2 DEFAULT GETUTCDATE(),
    StartedAt DATETIME2,
    CompletedAt DATETIME2,
    QuestDate DATE DEFAULT CAST(GETUTCDATE() AS DATE),
    
    FOREIGN KEY (HunterID) REFERENCES Hunters(HunterID),
    FOREIGN KEY (QuestID) REFERENCES DailyQuests(QuestID)
);

-- 4. TABLA DUNGEONS
CREATE TABLE Dungeons (
    DungeonID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DungeonName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    DungeonType NVARCHAR(30) DEFAULT 'Training Grounds',
    
    -- Configuración de dificultad
    Difficulty NVARCHAR(10) DEFAULT 'Normal',
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

-- 5. TABLA DUNGEON EXERCISES
CREATE TABLE DungeonExercises (
    ExerciseID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DungeonID UNIQUEIDENTIFIER NOT NULL,
    ExerciseOrder INT NOT NULL,
    
    -- Detalles del ejercicio
    ExerciseName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    TargetReps INT,
    TargetSets INT,
    TargetDuration INT,
    RestTimeSeconds INT DEFAULT 30,
    
    -- Metadatos
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    FOREIGN KEY (DungeonID) REFERENCES Dungeons(DungeonID)
);

-- 6. TABLA DUNGEON RAIDS
CREATE TABLE DungeonRaids (
    RaidID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    HunterID UNIQUEIDENTIFIER NOT NULL,
    DungeonID UNIQUEIDENTIFIER NOT NULL,
    
    -- Estado del raid
    Status NVARCHAR(20) DEFAULT 'Started',
    Progress DECIMAL(5,2) DEFAULT 0.00,
    
    -- Resultados
    TotalDuration INT, -- en segundos
    XPEarned INT DEFAULT 0,
    CompletionRate DECIMAL(5,2) DEFAULT 0.00,
    
    -- Timestamps
    StartedAt DATETIME2 DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2,
    NextAvailableAt DATETIME2,
    
    FOREIGN KEY (HunterID) REFERENCES Hunters(HunterID),
    FOREIGN KEY (DungeonID) REFERENCES Dungeons(DungeonID)
);

-- 7. TABLA ACHIEVEMENTS
CREATE TABLE Achievements (
    AchievementID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    AchievementName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    Category NVARCHAR(30) DEFAULT 'Milestone',
    
    -- Configuración del logro
    TargetValue INT,
    AchievementType NVARCHAR(20) DEFAULT 'Counter',
    IsHidden BIT DEFAULT 0,
    
    -- Recompensas
    XPReward INT DEFAULT 0,
    TitleReward NVARCHAR(50),
    
    -- Metadatos
    IconUrl NVARCHAR(500),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- 8. TABLA HUNTER ACHIEVEMENTS
CREATE TABLE HunterAchievements (
    HunterAchievementID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    HunterID UNIQUEIDENTIFIER NOT NULL,
    AchievementID UNIQUEIDENTIFIER NOT NULL,
    
    -- Progreso
    CurrentProgress INT DEFAULT 0,
    IsUnlocked BIT DEFAULT 0,
    UnlockedAt DATETIME2,
    
    -- Metadatos
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    FOREIGN KEY (HunterID) REFERENCES Hunters(HunterID),
    FOREIGN KEY (AchievementID) REFERENCES Achievements(AchievementID),
    UNIQUE(HunterID, AchievementID)
);

-- 9. TABLA EQUIPMENT
CREATE TABLE Equipment (
    EquipmentID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ItemName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    ItemType NVARCHAR(20) DEFAULT 'Weapon',
    Rarity NVARCHAR(15) DEFAULT 'Common',
    
    -- Bonificaciones de stats
    StrengthBonus INT DEFAULT 0,
    AgilityBonus INT DEFAULT 0,
    VitalityBonus INT DEFAULT 0,
    EnduranceBonus INT DEFAULT 0,
    XPMultiplier DECIMAL(3,2) DEFAULT 1.00,
    
    -- Requisitos para desbloquear
    UnlockLevel INT DEFAULT 1,
    UnlockRank NVARCHAR(10) DEFAULT 'E',
    UnlockCondition NVARCHAR(500),
    
    -- Metadatos
    IconUrl NVARCHAR(500),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- 10. TABLA HUNTER EQUIPMENT
CREATE TABLE HunterEquipment (
    HunterEquipmentID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    HunterID UNIQUEIDENTIFIER NOT NULL,
    EquipmentID UNIQUEIDENTIFIER NOT NULL,
    
    -- Estado del item
    IsEquipped BIT DEFAULT 0,
    UnlockedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    -- Metadatos
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    FOREIGN KEY (HunterID) REFERENCES Hunters(HunterID),
    FOREIGN KEY (EquipmentID) REFERENCES Equipment(EquipmentID),
    UNIQUE(HunterID, EquipmentID)
);

-- 11. TABLA QUEST HISTORY
CREATE TABLE QuestHistory (
    HistoryID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    HunterID UNIQUEIDENTIFIER NOT NULL,
    QuestID UNIQUEIDENTIFIER NOT NULL,
    
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
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    FOREIGN KEY (HunterID) REFERENCES Hunters(HunterID),
    FOREIGN KEY (QuestID) REFERENCES DailyQuests(QuestID)
);

-- Crear índices importantes
CREATE INDEX IX_Hunters_Username ON Hunters(Username);
CREATE INDEX IX_Hunters_Email ON Hunters(Email);
CREATE INDEX IX_HunterDailyQuests_HunterID_Date ON HunterDailyQuests(HunterID, QuestDate);
CREATE INDEX IX_DungeonRaids_HunterID ON DungeonRaids(HunterID);
CREATE INDEX IX_QuestHistory_HunterID_CompletedAt ON QuestHistory(HunterID, CompletedAt);

PRINT '✅ Tablas creadas exitosamente!'

-- ===================================================
-- INSERTAR DATOS SEMILLA
-- ===================================================

PRINT '🌱 Insertando datos semilla...'

-- Cardio Quests
INSERT INTO DailyQuests (QuestName, Description, QuestType, ExerciseName, TargetDuration, Difficulty, BaseXPReward, VitalityBonus, EnduranceBonus, MinLevel)
VALUES 
('Morning Run', 'Start your day with a energizing run!', 'Cardio', 'Running', 600, 'Easy', 50, 2, 1, 1),
('Cardio Blast', 'High intensity cardio to boost your heart rate!', 'Cardio', 'Jumping Jacks', 300, 'Medium', 75, 3, 2, 5),
('Endurance Challenge', 'Push your cardiovascular limits!', 'Cardio', 'Burpees', 480, 'Hard', 100, 4, 3, 10),
('Sprint Master', 'Short but intense sprinting session!', 'Cardio', 'Sprint Intervals', 240, 'Medium', 80, 3, 2, 8);

-- Strength Quests
INSERT INTO DailyQuests (QuestName, Description, QuestType, ExerciseName, TargetReps, TargetSets, Difficulty, BaseXPReward, StrengthBonus, MinLevel)
VALUES 
('Push-up Power', 'Build upper body strength with classic push-ups!', 'Strength', 'Push-ups', 20, 3, 'Easy', 60, 3, 1),
('Iron Arms', 'Strengthen your arms and shoulders!', 'Strength', 'Push-ups', 40, 4, 'Medium', 85, 4, 6),
('Squat Squad', 'Build powerful leg muscles!', 'Strength', 'Squats', 30, 3, 'Easy', 55, 2, 1),
('Beast Mode Squats', 'Ultimate leg strength challenge!', 'Strength', 'Squats', 60, 5, 'Hard', 110, 5, 12),
('Plank Warrior', 'Core strength and stability!', 'Strength', 'Plank Hold', NULL, NULL, 'Medium', 70, 2, 3);

-- Flexibility Quests
INSERT INTO DailyQuests (QuestName, Description, QuestType, ExerciseName, TargetDuration, Difficulty, BaseXPReward, AgilityBonus, VitalityBonus, MinLevel)
VALUES 
('Flexibility Flow', 'Improve your flexibility and mobility!', 'Flexibility', 'Stretching Routine', 600, 'Easy', 40, 2, 1, 1),
('Yoga Zen', 'Find balance and flexibility through yoga!', 'Flexibility', 'Yoga Session', 900, 'Medium', 65, 3, 2, 4),
('Dynamic Stretch', 'Active stretching to improve range of motion!', 'Flexibility', 'Dynamic Stretching', 420, 'Easy', 45, 2, 1, 2);

-- Mixed Quests
INSERT INTO DailyQuests (QuestName, Description, QuestType, ExerciseName, TargetReps, TargetSets, Difficulty, BaseXPReward, StrengthBonus, AgilityBonus, VitalityBonus, EnduranceBonus, MinLevel)
VALUES 
('Full Body Fusion', 'Complete workout targeting multiple muscle groups!', 'Mixed', 'Circuit Training', 45, 3, 'Medium', 95, 2, 2, 2, 2, 8),
('Hunter Training', 'Train like a true hunter with varied exercises!', 'Mixed', 'Functional Training', 30, 4, 'Hard', 130, 3, 3, 2, 3, 18);

-- DUNGEONS
INSERT INTO Dungeons (DungeonName, Description, DungeonType, Difficulty, MinLevel, MinRank, EstimatedDuration, EnergyCost, CooldownHours, BaseXPReward, BonusXPReward)
VALUES 
('Rookie Training Grounds', 'Perfect place for new hunters to test their skills!', 'Training Grounds', 'Normal', 1, 'E', 15, 5, 12, 100, 25),
('Strength Proving Ground', 'Demonstrate your raw power and determination!', 'Strength Trial', 'Normal', 5, 'D', 20, 8, 18, 150, 50),
('Cardio Gauntlet', 'Ultimate cardiovascular endurance test!', 'Endurance Test', 'Hard', 10, 'C', 25, 12, 24, 200, 75),
('Shadow Monarch Trial', 'Only the strongest hunters dare to enter!', 'Boss Raid', 'Extreme', 25, 'A', 45, 20, 48, 400, 200);

-- DUNGEON EXERCISES
INSERT INTO DungeonExercises (DungeonID, ExerciseOrder, ExerciseName, Description, TargetReps, TargetSets, RestTimeSeconds)
SELECT DungeonID, 1, 'Warm-up Jumping Jacks', 'Get your blood flowing!', 20, 1, 30
FROM Dungeons WHERE DungeonName = 'Rookie Training Grounds';

INSERT INTO DungeonExercises (DungeonID, ExerciseOrder, ExerciseName, Description, TargetReps, TargetSets, RestTimeSeconds)
SELECT DungeonID, 2, 'Basic Push-ups', 'Build upper body strength!', 10, 2, 60
FROM Dungeons WHERE DungeonName = 'Rookie Training Grounds';

-- ACHIEVEMENTS
INSERT INTO Achievements (AchievementName, Description, Category, TargetValue, AchievementType, XPReward, TitleReward)
VALUES 
('First Steps', 'Complete your first workout!', 'Consistency', 1, 'Counter', 50, 'Beginner Hunter'),
('Getting Started', 'Complete 5 workouts!', 'Consistency', 5, 'Counter', 100, NULL),
('Dedicated Trainee', 'Complete 25 workouts!', 'Consistency', 25, 'Counter', 250, 'Dedicated Hunter'),
('Fire Starter', 'Maintain a 3-day workout streak!', 'Consistency', 3, 'Streak', 150, 'Streak Starter'),
('Push-up Rookie', 'Complete 100 total push-ups!', 'Strength', 100, 'Counter', 150, NULL),
('Cardio Newbie', 'Complete 10 cardio exercises!', 'Endurance', 10, 'Counter', 120, NULL),
('Level Up!', 'Reach Level 5!', 'Milestone', 5, 'Counter', 200, 'Rising Hunter');

-- EQUIPMENT
INSERT INTO Equipment (ItemName, Description, ItemType, Rarity, StrengthBonus, UnlockLevel, UnlockRank)
VALUES 
('Training Gloves', 'Basic gloves for grip and protection during workouts.', 'Accessory', 'Common', 1, 1, 'E'),
('Workout Band', 'Simple resistance band for strength training.', 'Accessory', 'Common', 2, 2, 'E'),
('Hunter Wristbands', 'Professional wristbands that enhance performance.', 'Accessory', 'Rare', 2, 5, 'D'),
('Training Vest', 'Weighted vest for increased workout intensity.', 'Armor', 'Rare', 3, 8, 'D');

INSERT INTO Equipment (ItemName, Description, ItemType, Rarity, StrengthBonus, AgilityBonus, VitalityBonus, EnduranceBonus, XPMultiplier, UnlockLevel, UnlockRank)
VALUES 
('Hunter Gauntlets', 'Professional training gauntlets worn by elite hunters.', 'Accessory', 'Epic', 4, 2, 1, 1, 1.25, 12, 'B'),
('Power Suit', 'Advanced training gear that boosts all physical attributes.', 'Armor', 'Epic', 3, 3, 3, 3, 1.3, 18, 'B');

DECLARE @QuestCount INT = (SELECT COUNT(*) FROM DailyQuests);
DECLARE @DungeonCount INT = (SELECT COUNT(*) FROM Dungeons);
DECLARE @AchievementCount INT = (SELECT COUNT(*) FROM Achievements);
DECLARE @EquipmentCount INT = (SELECT COUNT(*) FROM Equipment);

PRINT '✅ Setup completo finalizado!'
PRINT '📊 Resumen:'
PRINT '   • 11 Tablas creadas'
PRINT '   • ' + CAST(@QuestCount AS NVARCHAR) + ' Daily Quests'
PRINT '   • ' + CAST(@DungeonCount AS NVARCHAR) + ' Dungeons'
PRINT '   • ' + CAST(@AchievementCount AS NVARCHAR) + ' Achievements'
PRINT '   • ' + CAST(@EquipmentCount AS NVARCHAR) + ' Equipment items'
PRINT ''
PRINT '🏹 ¡Hunter Fitness API lista para usar!'