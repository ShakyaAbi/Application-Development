using Microsoft.EntityFrameworkCore;
using Courcework.Entities;

namespace Courcework.Data
{
    
    /// Entity Framework Core DbContext for SQLite database
    /// Manages JournalEntry, Tag, and User entities
    
    public class JournalDbContext : DbContext
    {
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<User> Users { get; set; }

        private readonly string _dbPath;

        public JournalDbContext()
        {
            // Get platform-specific app data directory
            var dataDir = FileSystem.AppDataDirectory;
            _dbPath = Path.Combine(dataDir, "reflections.db");

            System.Diagnostics.Debug.WriteLine($"Database path: {_dbPath}");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // Configure SQLite with the database file path
            options.UseSqlite($"Data Source={_dbPath}");

            // Optional: Enable logging for debugging
#if DEBUG
            options.LogTo(Console.WriteLine);
#endif
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Username)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.Email)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.FullName)
                    .HasMaxLength(255)
                    .IsRequired(false);

                entity.Property(e => e.PasswordHash)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.PreferredTheme)
                    .HasDefaultValue("light");

                // Create indexes for quick lookups
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Configure JournalEntry entity
            modelBuilder.Entity<JournalEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.UserId)
                    .IsRequired();

                entity.Property(e => e.Title)
                    .HasMaxLength(500)
                    .IsRequired(false);

                entity.Property(e => e.Content)
                    .IsRequired();

                entity.Property(e => e.Date)
                    .IsRequired();

                entity.Property(e => e.Mood)
                    .HasMaxLength(100)
                    .IsRequired(false);

                entity.Property(e => e.SecondaryMood)
                    .HasMaxLength(100)
                    .IsRequired(false);

                entity.Property(e => e.Category)
                    .HasMaxLength(100)
                    .IsRequired(false);

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.Property(e => e.UpdatedAt)
                    .IsRequired();

                entity.Property(e => e.IsRichText)
                    .HasDefaultValue(false);

                // ? CREATE COMPOSITE INDEX: Each user can only have ONE entry per date
                entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique();
            });


            // Configure Tag entity
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.UserId)
                    .IsRequired();

                entity.Property(e => e.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.Color)
                    .HasMaxLength(7)
                    .IsRequired();

                // ? COMPOSITE UNIQUE INDEX: Each user can have unique tag names
                entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique();
            });
        }
    }
}
