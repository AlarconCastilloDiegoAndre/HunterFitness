using Microsoft.EntityFrameworkCore;
using hunter_fitness_api.Models;

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
                .WithMany(h => h.HunterDailyQuests)
                .HasForeignKey(hdq => hdq.HunterID);

            modelBuilder.Entity<HunterAchievement>()
                .HasIndex(ha => new { ha.HunterID, ha.AchievementID })
                .IsUnique();

            modelBuilder.Entity<HunterEquipment>()
                .HasIndex(he => new { he.HunterID, he.EquipmentID })
                .IsUnique();
        }
    }
}