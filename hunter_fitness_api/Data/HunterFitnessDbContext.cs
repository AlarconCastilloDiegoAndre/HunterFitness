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
        public DbSet<HunterEquipment> HunterEquipments { get; set; }
        public DbSet<QuestHistory> QuestHistories { get; set; }

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
                .HasOne<Hunter>()
                .WithMany(h => h.DailyQuests)
                .HasForeignKey(hdq => hdq.HunterID);

            modelBuilder.Entity<HunterAchievement>()
                .HasIndex(ha => new { ha.HunterID, ha.AchievementID })
                .IsUnique();

            modelBuilder.Entity<HunterEquipment>()
                .HasIndex(he => new { he.HunterID, he.EquipmentID })
                .IsUnique();

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
        }
    }
}