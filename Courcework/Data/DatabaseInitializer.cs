using Microsoft.EntityFrameworkCore;
using Courcework.Entities;

namespace Courcework.Data
{
    /// <summary>
    /// Helper class for database initialization and migrations
    /// Ensures database is created and seed data is populated
    /// </summary>
    public class DatabaseInitializer
    {
        private readonly JournalDbContext _context;

        public DatabaseInitializer(JournalDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Initialize the database - create tables and seed default data
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting database initialization...");

                // Create database and tables if they don't exist
                await _context.Database.EnsureCreatedAsync();

                // Apply any pending migrations
                await _context.Database.MigrateAsync();

                System.Diagnostics.Debug.WriteLine("Database tables created/verified.");

                // Handle schema updates for existing databases
                await EnsureSchemaAsync();

                System.Diagnostics.Debug.WriteLine("Database schema verified and updated.");

                // Seed default tags if none exist
                if (!await _context.Tags.AnyAsync())
                {
                    await SeedDefaultTagsAsync();
                    System.Diagnostics.Debug.WriteLine("Default tags seeded.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Tags already exist, skipping seed.");
                }

                System.Diagnostics.Debug.WriteLine("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Ensure database schema is up to date (add missing columns)
        /// </summary>
        private async Task EnsureSchemaAsync()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                
                // Check if IsRichText column exists, if not add it
                command.CommandText = @"
                    PRAGMA table_info(JournalEntries);
                ";

                var reader = await command.ExecuteReaderAsync();
                var columnNames = new List<string>();
                
                while (await reader.ReadAsync())
                {
                    columnNames.Add(reader["name"].ToString());
                }
                await reader.CloseAsync();

                // Add missing columns
                if (!columnNames.Contains("IsRichText"))
                {
                    System.Diagnostics.Debug.WriteLine("Adding IsRichText column...");
                    command.CommandText = @"
                        ALTER TABLE JournalEntries 
                        ADD COLUMN IsRichText INTEGER DEFAULT 0;
                    ";
                    try
                    {
                        await command.ExecuteNonQueryAsync();
                        System.Diagnostics.Debug.WriteLine("IsRichText column added successfully.");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"IsRichText column may already exist: {ex.Message}");
                    }
                }

                if (!columnNames.Contains("SecondaryMood"))
                {
                    System.Diagnostics.Debug.WriteLine("Adding SecondaryMood column...");
                    command.CommandText = @"
                        ALTER TABLE JournalEntries 
                        ADD COLUMN SecondaryMood TEXT;
                    ";
                    try
                    {
                        await command.ExecuteNonQueryAsync();
                        System.Diagnostics.Debug.WriteLine("SecondaryMood column added successfully.");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"SecondaryMood column may already exist: {ex.Message}");
                    }
                }

                await connection.CloseAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Schema update warning (may not be critical): {ex.Message}");
                // Don't throw here as this is a best-effort update
            }
        }

        /// <summary>
        /// Seed default tags on first run
        /// </summary>
        private async Task SeedDefaultTagsAsync()
        {
            var defaultTags = new[]
            {
                new Tag { Id = Guid.NewGuid(), Name = "Reflection", Color = "#1976d2" },
                new Tag { Id = Guid.NewGuid(), Name = "Work", Color = "#64b5f6" },
                new Tag { Id = Guid.NewGuid(), Name = "Gratitude", Color = "#4caf50" },
                new Tag { Id = Guid.NewGuid(), Name = "Ideas", Color = "#ff9800" },
                new Tag { Id = Guid.NewGuid(), Name = "Health", Color = "#f44336" }
            };

            await _context.Tags.AddRangeAsync(defaultTags);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Migrate JSON data to SQLite (one-time migration)
        /// </summary>
        public async Task MigrateFromJsonAsync(StorageService jsonStorage)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting JSON to SQLite migration...");

                // Get all entries from JSON
                var entries = await jsonStorage.GetAllEntriesAsync();

                if (!entries.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No JSON entries to migrate.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Found {entries.Count} entries to migrate.");

                // Add entries to SQLite
                foreach (var entry in entries)
                {
                    // Check if entry already exists
                    if (await _context.JournalEntries.AnyAsync(e => e.Id == entry.Id))
                    {
                        continue; // Skip duplicates
                    }

                    _context.JournalEntries.Add(entry);
                }

                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"Successfully migrated {entries.Count} entries to SQLite.");
                System.Diagnostics.Debug.WriteLine("You can now delete the JSON files if migration was successful.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Migration error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Reset database (delete all data and recreate tables)
        /// WARNING: This is destructive!
        /// </summary>
        public async Task ResetDatabaseAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("WARNING: Resetting database...");

                await _context.Database.EnsureDeletedAsync();
                await _context.Database.EnsureCreatedAsync();
                await SeedDefaultTagsAsync();

                System.Diagnostics.Debug.WriteLine("Database reset complete.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Reset error: {ex.Message}");
                throw;
            }
        }
    }
}
