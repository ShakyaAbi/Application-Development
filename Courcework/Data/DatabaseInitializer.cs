using Microsoft.EntityFrameworkCore;
using Courcework.Entities;

namespace Courcework.Data
{
    
    /// Helper class for database initialization and migrations
    /// Ensures database is created and seed data is populated
    
    public class DatabaseInitializer
    {
        private readonly JournalDbContext _context;

        public DatabaseInitializer(JournalDbContext context)
        {
            _context = context;
        }

        
        /// Initialize the database - create tables and seed default data
        
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

                // ? Ensure Users table exists
                await EnsureUsersTableAsync();

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

        
        /// Ensure database schema is up to date (add missing columns)
        
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

                // ? Add UserId column if missing
                if (!columnNames.Contains("UserId"))
                {
                    System.Diagnostics.Debug.WriteLine("Adding UserId column to JournalEntries...");
                    command.CommandText = @"
                        ALTER TABLE JournalEntries 
                        ADD COLUMN UserId TEXT DEFAULT '';
                    ";
                    try
                    {
                        await command.ExecuteNonQueryAsync();
                        System.Diagnostics.Debug.WriteLine("UserId column added successfully to JournalEntries.");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"UserId column may already exist in JournalEntries: {ex.Message}");
                    }
                }

                // ? ADD USERID COLUMN TO TAGS TABLE
                command.CommandText = @"
                    PRAGMA table_info(Tags);
                ";

                reader = await command.ExecuteReaderAsync();
                var tagColumnNames = new List<string>();
                
                while (await reader.ReadAsync())
                {
                    tagColumnNames.Add(reader["name"].ToString());
                }
                await reader.CloseAsync();

                if (!tagColumnNames.Contains("UserId"))
                {
                    System.Diagnostics.Debug.WriteLine("Adding UserId column to Tags...");
                    command.CommandText = @"
                        ALTER TABLE Tags 
                        ADD COLUMN UserId TEXT DEFAULT '';
                    ";
                    try
                    {
                        await command.ExecuteNonQueryAsync();
                        System.Diagnostics.Debug.WriteLine("UserId column added successfully to Tags.");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"UserId column may already exist in Tags: {ex.Message}");
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

        
        /// Ensure Users table exists and has all required columns
        
        private async Task EnsureUsersTableAsync()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();

                // Check if Users table exists
                command.CommandText = @"
                    SELECT name FROM sqlite_master 
                    WHERE type='table' AND name='Users';
                ";

                var result = await command.ExecuteScalarAsync();
                
                if (result == null)
                {
                    System.Diagnostics.Debug.WriteLine("? Creating Users table...");
                    
                    // Create Users table
                    command.CommandText = @"
                        CREATE TABLE Users (
                            Id TEXT PRIMARY KEY,
                            Username TEXT NOT NULL UNIQUE,
                            Email TEXT NOT NULL UNIQUE,
                            FullName TEXT NOT NULL,
                            PasswordHash TEXT NOT NULL,
                            CreatedAt TEXT NOT NULL,
                            LastLoginAt TEXT,
                            IsActive INTEGER NOT NULL DEFAULT 1,
                            PreferredTheme TEXT DEFAULT 'light'
                        );
                    ";
                    
                    await command.ExecuteNonQueryAsync();

                    // Create indexes for faster queries
                    command.CommandText = @"
                        CREATE INDEX idx_users_username ON Users(Username);
                        CREATE INDEX idx_users_email ON Users(Email);
                    ";
                    
                    await command.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine("? Users table created successfully with indexes");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("? Users table already exists");
                }

                await connection.CloseAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Users table error: {ex.Message}");
                throw;
            }
        }

        
        /// Seed pre-built default tags on first run
        
        private async Task SeedDefaultTagsAsync()
        {
            // ? COMPREHENSIVE PRE-BUILT TAGS LIST
            var defaultTags = new[]
            {
                // Work & Career
                new Tag { Id = Guid.NewGuid(), Name = "Work", Color = "#1976d2" },
                new Tag { Id = Guid.NewGuid(), Name = "Career", Color = "#1565c0" },
                new Tag { Id = Guid.NewGuid(), Name = "Studies", Color = "#0d47a1" },
                new Tag { Id = Guid.NewGuid(), Name = "Projects", Color = "#2196f3" },
                new Tag { Id = Guid.NewGuid(), Name = "Planning", Color = "#42a5f5" },
                
                // Relationships
                new Tag { Id = Guid.NewGuid(), Name = "Family", Color = "#e91e63" },
                new Tag { Id = Guid.NewGuid(), Name = "Friends", Color = "#ec407a" },
                new Tag { Id = Guid.NewGuid(), Name = "Relationships", Color = "#f06292" },
                new Tag { Id = Guid.NewGuid(), Name = "Parenting", Color = "#f48fb1" },
                
                // Health & Fitness
                new Tag { Id = Guid.NewGuid(), Name = "Health", Color = "#f44336" },
                new Tag { Id = Guid.NewGuid(), Name = "Fitness", Color = "#e53935" },
                new Tag { Id = Guid.NewGuid(), Name = "Exercise", Color = "#c62828" },
                new Tag { Id = Guid.NewGuid(), Name = "Yoga", Color = "#d32f2f" },
                new Tag { Id = Guid.NewGuid(), Name = "Meditation", Color = "#ef5350" },
                new Tag { Id = Guid.NewGuid(), Name = "Self-care", Color = "#f44336" },
                
                // Personal Development
                new Tag { Id = Guid.NewGuid(), Name = "Personal Growth", Color = "#7b1fa2" },
                new Tag { Id = Guid.NewGuid(), Name = "Spirituality", Color = "#6a1b9a" },
                new Tag { Id = Guid.NewGuid(), Name = "Reflection", Color = "#512da8" },
                new Tag { Id = Guid.NewGuid(), Name = "Reading", Color = "#7e57c2" },
                new Tag { Id = Guid.NewGuid(), Name = "Writing", Color = "#9575cd" },
                
                // Hobbies & Leisure
                new Tag { Id = Guid.NewGuid(), Name = "Hobbies", Color = "#ff6f00" },
                new Tag { Id = Guid.NewGuid(), Name = "Music", Color = "#e65100" },
                new Tag { Id = Guid.NewGuid(), Name = "Cooking", Color = "#bf360c" },
                new Tag { Id = Guid.NewGuid(), Name = "Shopping", Color = "#ff6f00" },
                
                // Travel & Nature
                new Tag { Id = Guid.NewGuid(), Name = "Travel", Color = "#00897b" },
                new Tag { Id = Guid.NewGuid(), Name = "Nature", Color = "#009688" },
                new Tag { Id = Guid.NewGuid(), Name = "Vacation", Color = "#26a69a" },
                
                // Finance
                new Tag { Id = Guid.NewGuid(), Name = "Finance", Color = "#fbc02d" },
                
                // Special Events
                new Tag { Id = Guid.NewGuid(), Name = "Birthday", Color = "#ff4081" },
                new Tag { Id = Guid.NewGuid(), Name = "Holiday", Color = "#d81b60" },
                new Tag { Id = Guid.NewGuid(), Name = "Celebration", Color = "#f50057" },
                
                // Gratitude & Ideas
                new Tag { Id = Guid.NewGuid(), Name = "Gratitude", Color = "#4caf50" },
                new Tag { Id = Guid.NewGuid(), Name = "Ideas", Color = "#ff9800" }
            };

            await _context.Tags.AddRangeAsync(defaultTags);
            await _context.SaveChangesAsync();
            
            System.Diagnostics.Debug.WriteLine($"? Seeded {defaultTags.Length} pre-built tags");
        }

        
        /// Migrate JSON data to SQLite (one-time migration)
        
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

        
        /// Reset database (delete all data and recreate tables)
        /// WARNING: This is destructive!
        
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
