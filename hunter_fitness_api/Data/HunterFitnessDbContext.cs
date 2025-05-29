using Microsoft.EntityFrameworkCore;
using HunterFitness.API.Models;

namespace HunterFitness.API.Data
{
    public class HunterFitnessDbContext : DbContext
    {
        public HunterFitnessDbContext(DbContextOptions<HunterFitnessDbContext> options)
            : base(options)
        {
        }

        // DbSets - Tablas principales
        public DbSet<Hunter> Hunters { get; set; } = null!;
        public DbSet<DailyQuest> DailyQuests { get; set; } = null!;
        public DbSet<HunterDailyQuest> HunterDailyQuests { get; set; } = null!;
        public DbSet<Dungeon> Dungeons { get; set; } = null!;
        public DbSet<DungeonExercise> DungeonExercises { get; set; } = null!;
        public DbSet<DungeonRaid> DungeonRaids { get; set; } = null!;
        public DbSet<Achievement> Achievements { get; set; } = null!;
        public DbSet<HunterAchievement> HunterAchievements { get; set; } = null!;
        public DbSet<Equipment> Equipment { get; set; } = null!;
        public DbSet<HunterEquipment> HunterEquipment { get; set; } = null!;
        public DbSet<QuestHistory> QuestHistory { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Hunter
            modelBuilder.Entity<Hunter>(entity =>
            {
                entity.HasKey(e => e.HunterID);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Level);
                entity.HasIndex(e => e.HunterRank);
                
                entity.Property(e => e.HunterRank)
                    .HasDefaultValue("E");
                
                entity.Property(e => e.Level)
                    .HasDefaultValue(1);
                
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Configurar check constraints
                entity.HasCheckConstraint("CK_Hunter_HunterRank", 
                    "HunterRank IN ('E', 'D', 'C', 'B', 'A', 'S', 'SS', 'SSS')");
                
                entity.HasCheckConstraint("CK_Hunter_Level", "Level >= 1");
                entity.HasCheckConstraint("CK_Hunter_Stats", 
                    "Strength >= 0 AND Agility >= 0 AND Vitality >= 0 AND Endurance >= 0");
            });

            // Configuración de DailyQuest
            modelBuilder.Entity<DailyQuest>(entity =>
            {
                entity.HasKey(e => e.QuestID);
                entity.HasIndex(e => e.QuestType);
                entity.HasIndex(e => e.Difficulty);
                entity.HasIndex(e => e.IsActive);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasCheckConstraint("CK_DailyQuest_QuestType",
                    "QuestType IN ('Cardio', 'Strength', 'Flexibility', 'Endurance', 'Mixed')");
                
                entity.HasCheckConstraint("CK_DailyQuest_Difficulty",
                    "Difficulty IN ('Easy', 'Medium', 'Hard', 'Extreme')");

                // Configurar columna decimal para TargetDistance
                entity.Property(e => e.TargetDistance)
                    .HasColumnType("decimal(10,2)");
            });

            // Configuración de HunterDailyQuest
            modelBuilder.Entity<HunterDailyQuest>(entity =>
            {
                entity.HasKey(e => e.AssignmentID);
                entity.HasIndex(e => new { e.HunterID, e.QuestDate });
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.QuestDate);

                entity.Property(e => e.AssignedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                
                entity.Property(e => e.QuestDate)
                    .HasDefaultValueSql("CAST(GETUTCDATE() AS DATE)");

                entity.HasCheckConstraint("CK_HunterDailyQuest_Status",
                    "Status IN ('Assigned', 'InProgress', 'Completed', 'Failed')");
                
                entity.HasCheckConstraint("CK_HunterDailyQuest_Progress",
                    "Progress >= 0 AND Progress <= 100");

                // Configurar columnas decimales
                entity.Property(e => e.Progress)
                    .HasColumnType("decimal(5,2)");

                entity.Property(e => e.BonusMultiplier)
                    .HasColumnType("decimal(3,2)");

                entity.Property(e => e.CurrentDistance)
                    .HasColumnType("decimal(10,2)");

                // Relaciones
                entity.HasOne(d => d.Hunter)
                    .WithMany(p => p.DailyQuests)
                    .HasForeignKey(d => d.HunterID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Quest)
                    .WithMany(p => p.HunterDailyQuests)
                    .HasForeignKey(d => d.QuestID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de Dungeon
            modelBuilder.Entity<Dungeon>(entity =>
            {
                entity.HasKey(e => e.DungeonID);
                entity.HasIndex(e => e.DungeonType);
                entity.HasIndex(e => e.Difficulty);
                entity.HasIndex(e => e.IsActive);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasCheckConstraint("CK_Dungeon_DungeonType",
                    "DungeonType IN ('Training Grounds', 'Strength Trial', 'Endurance Test', 'Boss Raid')");
                
                entity.HasCheckConstraint("CK_Dungeon_Difficulty",
                    "Difficulty IN ('Normal', 'Hard', 'Extreme', 'Nightmare')");
            });

            // Configuración de DungeonExercise
            modelBuilder.Entity<DungeonExercise>(entity =>
            {
                entity.HasKey(e => e.ExerciseID);
                entity.HasIndex(e => new { e.DungeonID, e.ExerciseOrder });

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Relación con Dungeon
                entity.HasOne(d => d.Dungeon)
                    .WithMany(p => p.Exercises)
                    .HasForeignKey(d => d.DungeonID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de DungeonRaid
            modelBuilder.Entity<DungeonRaid>(entity =>
            {
                entity.HasKey(e => e.RaidID);
                entity.HasIndex(e => e.HunterID);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.StartedAt);

                entity.Property(e => e.StartedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasCheckConstraint("CK_DungeonRaid_Status",
                    "Status IN ('Started', 'InProgress', 'Completed', 'Failed', 'Abandoned')");
                
                entity.HasCheckConstraint("CK_DungeonRaid_Progress",
                    "Progress >= 0 AND Progress <= 100");

                // Configurar columnas decimales
                entity.Property(e => e.Progress)
                    .HasColumnType("decimal(5,2)");

                entity.Property(e => e.CompletionRate)
                    .HasColumnType("decimal(5,2)");

                // Relaciones
                entity.HasOne(d => d.Hunter)
                    .WithMany(p => p.DungeonRaids)
                    .HasForeignKey(d => d.HunterID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Dungeon)
                    .WithMany(p => p.Raids)
                    .HasForeignKey(d => d.DungeonID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de Achievement
            modelBuilder.Entity<Achievement>(entity =>
            {
                entity.HasKey(e => e.AchievementID);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.AchievementType);
                entity.HasIndex(e => e.IsActive);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasCheckConstraint("CK_Achievement_Category",
                    "Category IN ('Consistency', 'Strength', 'Endurance', 'Social', 'Special', 'Milestone')");
                
                entity.HasCheckConstraint("CK_Achievement_Type",
                    "AchievementType IN ('Counter', 'Streak', 'Single', 'Progressive')");
            });

            // Configuración de HunterAchievement
            modelBuilder.Entity<HunterAchievement>(entity =>
            {
                entity.HasKey(e => e.HunterAchievementID);
                entity.HasIndex(e => new { e.HunterID, e.AchievementID }).IsUnique();
                entity.HasIndex(e => e.IsUnlocked);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Relaciones
                entity.HasOne(d => d.Hunter)
                    .WithMany(p => p.Achievements)
                    .HasForeignKey(d => d.HunterID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Achievement)
                    .WithMany(p => p.HunterAchievements)
                    .HasForeignKey(d => d.AchievementID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de Equipment
            modelBuilder.Entity<Equipment>(entity =>
            {
                entity.HasKey(e => e.EquipmentID);
                entity.HasIndex(e => e.ItemType);
                entity.HasIndex(e => e.Rarity);
                entity.HasIndex(e => e.IsActive);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasCheckConstraint("CK_Equipment_ItemType",
                    "ItemType IN ('Weapon', 'Armor', 'Accessory')");
                
                entity.HasCheckConstraint("CK_Equipment_Rarity",
                    "Rarity IN ('Common', 'Rare', 'Epic', 'Legendary', 'Mythic')");
                
                entity.HasCheckConstraint("CK_Equipment_XPMultiplier",
                    "XPMultiplier >= 0.5 AND XPMultiplier <= 3.0");

                // Configurar columna decimal para XPMultiplier
                entity.Property(e => e.XPMultiplier)
                    .HasColumnType("decimal(3,2)");
            });

            // Configuración de HunterEquipment
            modelBuilder.Entity<HunterEquipment>(entity =>
            {
                entity.HasKey(e => e.HunterEquipmentID);
                entity.HasIndex(e => new { e.HunterID, e.EquipmentID }).IsUnique();
                entity.HasIndex(e => e.IsEquipped);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                
                entity.Property(e => e.UnlockedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Relaciones
                entity.HasOne(d => d.Hunter)
                    .WithMany(p => p.Equipment)
                    .HasForeignKey(d => d.HunterID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Equipment)
                    .WithMany(p => p.HunterEquipment)
                    .HasForeignKey(d => d.EquipmentID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de QuestHistory
            modelBuilder.Entity<QuestHistory>(entity =>
            {
                entity.HasKey(e => e.HistoryID);
                entity.HasIndex(e => new { e.HunterID, e.CompletedAt });
                entity.HasIndex(e => e.CompletedAt);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasCheckConstraint("CK_QuestHistory_BonusMultiplier",
                    "BonusMultiplier >= 0.5 AND BonusMultiplier <= 5.0");

                // Configurar columnas decimales
                entity.Property(e => e.BonusMultiplier)
                    .HasColumnType("decimal(3,2)");

                entity.Property(e => e.FinalDistance)
                    .HasColumnType("decimal(10,2)");

                // Relaciones
                entity.HasOne(d => d.Hunter)
                    .WithMany(p => p.QuestHistory)
                    .HasForeignKey(d => d.HunterID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Quest)
                    .WithMany(p => p.QuestHistory)
                    .HasForeignKey(d => d.QuestID)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        // Método helper para obtener la cadena de conexión
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Fallback en caso de que no se configure externamente
                var connectionString = Environment.GetEnvironmentVariable("HunterFitnessDB");
                if (!string.IsNullOrEmpty(connectionString))
                {
                    optionsBuilder.UseSqlServer(connectionString);
                }
            }
        }

        // Métodos helper para operaciones comunes
        public async Task<Hunter?> GetHunterByUsernameAsync(string username)
        {
            return await Hunters
                .Include(h => h.DailyQuests.Where(dq => dq.QuestDate == DateTime.UtcNow.Date))
                .ThenInclude(dq => dq.Quest)
                .Include(h => h.Equipment.Where(e => e.IsEquipped))
                .ThenInclude(e => e.Equipment)
                .FirstOrDefaultAsync(h => h.Username == username && h.IsActive);
        }

        public async Task<List<HunterDailyQuest>> GetTodayQuestsAsync(Guid hunterId)
        {
            return await HunterDailyQuests
                .Include(hq => hq.Quest)
                .Where(hq => hq.HunterID == hunterId && hq.QuestDate == DateTime.UtcNow.Date)
                .OrderBy(hq => hq.AssignedAt)
                .ToListAsync();
        }

        public async Task<List<DungeonRaid>> GetActiveRaidsAsync(Guid hunterId)
        {
            return await DungeonRaids
                .Include(dr => dr.Dungeon)
                .Where(dr => dr.HunterID == hunterId && 
                           (dr.Status == "Started" || dr.Status == "InProgress"))
                .OrderBy(dr => dr.StartedAt)
                .ToListAsync();
        }

        public async Task<List<Hunter>> GetLeaderboardAsync(int limit = 100)
        {
            return await Hunters
                .Where(h => h.IsActive)
                .OrderByDescending(h => h.TotalXP)
                .ThenByDescending(h => h.Level)
                .ThenByDescending(h => h.LongestStreak)
                .Take(limit)
                .ToListAsync();
        }

        // Métodos adicionales para facilitar consultas comunes
        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await Hunters.AnyAsync(h => h.Username == username && h.IsActive);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await Hunters.AnyAsync(h => h.Email == email && h.IsActive);
        }

        public async Task<int> GetActiveHuntersCountAsync()
        {
            return await Hunters.CountAsync(h => h.IsActive);
        }

        public async Task<List<Achievement>> GetAchievementsByCategoryAsync(string category)
        {
            return await Achievements
                .Where(a => a.Category == category && a.IsActive)
                .OrderBy(a => a.AchievementName)
                .ToListAsync();
        }

        public async Task<List<Equipment>> GetEquipmentByTypeAsync(string itemType)
        {
            return await Equipment
                .Where(e => e.ItemType == itemType && e.IsActive)
                .OrderBy(e => e.UnlockLevel)
                .ThenBy(e => e.ItemName)
                .ToListAsync();
        }

        public async Task<List<DailyQuest>> GetQuestsByDifficultyAsync(string difficulty)
        {
            return await DailyQuests
                .Where(q => q.Difficulty == difficulty && q.IsActive)
                .OrderBy(q => q.MinLevel)
                .ThenBy(q => q.QuestName)
                .ToListAsync();
        }

        // Método para verificar la salud de la base de datos
        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                return await Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        // Método para aplicar migraciones pendientes
        public async Task<bool> ApplyPendingMigrationsAsync()
        {
            try
            {
                var pendingMigrations = await Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    await Database.MigrateAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        // Override del Dispose para limpieza
        public override void Dispose()
        {
            base.Dispose();
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
        }
    }
}