using apz_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace apz_backend.Data
{
    public class ApiDbContext : DbContext
    {
        public ApiDbContext(DbContextOptions<ApiDbContext> options)
            : base(options)
        {
        }

        // Налаштування відносин багато-до-одного та видалення по ланцюжку
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Soldier>()
                .HasMany(s => s.Sleeps)
                .WithOne(s => s.Soldier)
                .HasForeignKey(s => s.SoldierId)
                .OnDelete(DeleteBehavior.Cascade); // Видалення сну при видаленні солдата

            modelBuilder.Entity<Soldier>()
                .HasMany(r => r.Rotations)
                .WithOne(s => s.Soldier)
                .HasForeignKey(s => s.SoldierId)
                .OnDelete(DeleteBehavior.Cascade); // Видалення ротацій при видаленні солдата

            modelBuilder.Entity<Soldier>()
                .HasMany(r => r.Requests)
                .WithOne(s => s.Soldier)
                .HasForeignKey(s => s.SoldierId)
                .OnDelete(DeleteBehavior.Cascade); // Видалення запитів при видаленні солдата

            base.OnModelCreating(modelBuilder);
        }

        // DbSet для кожної моделі
        public DbSet<Unit> Units { get; set; } // Доступ до набору даних для військових частин
        public DbSet<Commander> Commanders { get; set; } // Доступ до набору даних для командирів
        public DbSet<Soldier> Soldiers { get; set; } // Доступ до набору даних для солдатів
        public DbSet<Sleep> Sleeps { get; set; } // Доступ до набору даних для сну
        public DbSet<Rotation> Rotations { get; set; } // Доступ до набору даних для ротацій
        public DbSet<Request> Requests { get; set; } // Доступ до набору даних для запитів
    }
}
