using Microsoft.EntityFrameworkCore;
using HunterFitness.API.Models;

namespace HunterFitness.API.Data
{
    public class HunterFitnessDbContext : DbContext
    {
        public HunterFitnessDbContext(DbContextOptions<HunterFitnessDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<Hunter> Hunters { get; set; }
        public DbSet<DailyQuest> DailyQuests { get; set; }
        public DbSet<HunterDailyQuest> HunterDailyQuests { get; set; }
        public DbSet<Dungeon> Dungeons { get; set; }
        public DbSet<DungeonExercise> DungeonExercises { get; set; }
        public DbSet<DungeonRaid> DungeonRaids { get; set; }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<HunterAchievement> HunterAchievements { get; set; }
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<HunterEquipment> HunterEquipment { get; set; } // Corregido: nombre singular
        public DbSet<QuestHistory> QuestHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuraciones de índices únicos
            modelBuilder.Entity<Hunter>()
                .HasIndex(h => h.Username)
                .IsUnique();

            modelBuilder.Entity<Hunter>()
                .HasIndex(h => h.Email)
                .IsUnique();

            // Configuración de relaciones
            modelBuilder.Entity<HunterDailyQuest>()
                .HasOne(hdq => hdq.Hunter)
                .WithMany(h => h.DailyQuests)
                .HasForeignKey(hdq => hdq.HunterID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HunterDailyQuest>()
                .HasOne(hdq => hdq.Quest)
                .WithMany(q => q.HunterDailyQuests)
                .HasForeignKey(hdq => hdq.QuestID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HunterAchievement>()
                .HasIndex(ha => new { ha.HunterID, ha.AchievementID })
                .IsUnique();

            modelBuilder.Entity<HunterAchievement>()
                .HasOne(ha => ha.Hunter)
                .WithMany(h => h.Achievements)
                .HasForeignKey(ha => ha.HunterID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HunterAchievement>()
                .HasOne(ha => ha.Achievement)
                .WithMany(a => a.HunterAchievements)
                .HasForeignKey(ha => ha.AchievementID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HunterEquipment>()
                .HasIndex(he => new { he.HunterID, he.EquipmentID })
                .IsUnique();

            modelBuilder.Entity<HunterEquipment>()
                .HasOne(he => he.Hunter)
                .WithMany(h => h.Equipment)
                .HasForeignKey(he => he.HunterID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HunterEquipment>()
                .HasOne(he => he.Equipment)
                .WithMany(e => e.HunterEquipment)
                .HasForeignKey(he => he.EquipmentID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DungeonRaid>()
                .HasOne(dr => dr.Hunter)
                .WithMany(h => h.DungeonRaids)
                .HasForeignKey(dr => dr.HunterID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DungeonRaid>()
                .HasOne(dr => dr.Dungeon)
                .WithMany(d => d.Raids)
                .HasForeignKey(dr => dr.DungeonID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DungeonExercise>()
                .HasOne(de => de.Dungeon)
                .WithMany(d => d.Exercises)
                .HasForeignKey(de => de.DungeonID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuestHistory>()
                .HasOne(qh => qh.Hunter)
                .WithMany(h => h.QuestHistory)
                .HasForeignKey(qh => qh.HunterID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuestHistory>()
                .HasOne(qh => qh.Quest)
                .WithMany(q => q.QuestHistory)
                .HasForeignKey(qh => qh.QuestID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configurar precision para decimales
            modelBuilder.Entity<HunterDailyQuest>()
                .Property(h => h.Progress)
                .HasPrecision(5, 2);

            modelBuilder.Entity<HunterDailyQuest>()
                .Property(h => h.BonusMultiplier)
                .HasPrecision(3, 2);

            modelBuilder.Entity<HunterDailyQuest>()
                .Property(h => h.CurrentDistance)
                .HasPrecision(10, 2);

            modelBuilder.Entity<DungeonRaid>()
                .Property(d => d.Progress)
                .HasPrecision(5, 2);

            modelBuilder.Entity<DungeonRaid>()
                .Property(d => d.CompletionRate)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Equipment>()
                .Property(e => e.XPMultiplier)
                .HasPrecision(3, 2);

            modelBuilder.Entity<QuestHistory>()
                .Property(q => q.BonusMultiplier)
                .HasPrecision(3, 2);

            modelBuilder.Entity<QuestHistory>()
                .Property(q => q.FinalDistance)
                .HasPrecision(10, 2);

            modelBuilder.Entity<DailyQuest>()
                .Property(d => d.TargetDistance)
                .HasPrecision(10, 2);

            // Configuraciones de índices para rendimiento
            modelBuilder.Entity<HunterDailyQuest>()
                .HasIndex(hq => new { hq.HunterID, hq.QuestDate })
                .HasDatabaseName("IX_HunterDailyQuests_HunterID_Date");

            modelBuilder.Entity<DungeonRaid>()
                .HasIndex(dr => dr.HunterID)
                .HasDatabaseName("IX_DungeonRaids_HunterID");

            modelBuilder.Entity<QuestHistory>()
                .HasIndex(qh => new { qh.HunterID, qh.CompletedAt })
                .HasDatabaseName("IX_QuestHistory_HunterID_CompletedAt");

            // Configurar columnas de fecha
            modelBuilder.Entity<HunterDailyQuest>()
                .Property(hq => hq.QuestDate)
                .HasColumnType("date");
        }
    }
}