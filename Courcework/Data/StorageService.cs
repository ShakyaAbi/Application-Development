using System.Text.Json;
using Courcework.Entities;

namespace Courcework.Data
{
    
    /// Simple data storage service using JSON files
    /// This provides persistence without requiring EF Core or SQLite packages
    
    public class StorageService
    {
        private readonly string _dataDir;
        private readonly string _entriesFile;
        private readonly string _tagsFile;
        private List<JournalEntry> _entries = new();
        private List<Tag> _tags = new();
        private bool _initialized = false;

        public StorageService()
        {
            _dataDir = FileSystem.AppDataDirectory;
            _entriesFile = Path.Combine(_dataDir, "entries.json");
            _tagsFile = Path.Combine(_dataDir, "tags.json");
        }

        
        /// Initialize storage (load from files)
        
        public async Task InitializeAsync()
        {
            if (_initialized) return;

            try
            {
                await LoadEntriesAsync();
                await LoadTagsAsync();
                await SeedDefaultTagsAsync();
                _initialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Storage initialization error: {ex}");
            }
        }

        private async Task LoadEntriesAsync()
        {
            try
            {
                if (File.Exists(_entriesFile))
                {
                    var json = await File.ReadAllTextAsync(_entriesFile);
                    _entries = JsonSerializer.Deserialize<List<JournalEntry>>(json) ?? new();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading entries: {ex}");
                _entries = new();
            }
        }

        private async Task LoadTagsAsync()
        {
            try
            {
                if (File.Exists(_tagsFile))
                {
                    var json = await File.ReadAllTextAsync(_tagsFile);
                    _tags = JsonSerializer.Deserialize<List<Tag>>(json) ?? new();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading tags: {ex}");
                _tags = new();
            }
        }

        private async Task SeedDefaultTagsAsync()
        {
            if (_tags.Any()) return;

            _tags = new()
            {
                new Tag { Name = "Reflection", Color = "#1976d2" },
                new Tag { Name = "Work", Color = "#64b5f6" },
                new Tag { Name = "Gratitude", Color = "#4caf50" },
                new Tag { Name = "Ideas", Color = "#ff9800" },
                new Tag { Name = "Health", Color = "#f44336" }
            };

            await SaveTagsAsync();
        }

        
        /// Get entry by date
        
        public Task<JournalEntry?> GetEntryByDateAsync(DateOnly date)
        {
            return Task.FromResult(_entries.FirstOrDefault(e => e.Date == date));
        }

        
        /// Get all entries
        
        public Task<List<JournalEntry>> GetAllEntriesAsync()
        {
            return Task.FromResult(_entries.OrderByDescending(e => e.Date).ToList());
        }

        
        /// Get entries by month
        
        public Task<List<JournalEntry>> GetEntriesByMonthAsync(int year, int month)
        {
            return Task.FromResult(
                _entries.Where(e => e.Date.Year == year && e.Date.Month == month)
                    .OrderByDescending(e => e.Date)
                    .ToList()
            );
        }

        
        /// Save or update entry
        
        public async Task SaveEntryAsync(JournalEntry entry)
        {
            try
            {
                var existing = _entries.FirstOrDefault(e => e.Id == entry.Id);
                if (existing != null)
                {
                    _entries.Remove(existing);
                }

                entry.UpdatedAt = DateTime.Now;
                _entries.Add(entry);
                await SaveEntriesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving entry: {ex}");
                throw;
            }
        }

        
        /// Delete entry
        
        public async Task DeleteEntryAsync(Guid entryId)
        {
            try
            {
                var entry = _entries.FirstOrDefault(e => e.Id == entryId);
                if (entry != null)
                {
                    _entries.Remove(entry);
                    await SaveEntriesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting entry: {ex}");
                throw;
            }
        }

        
        /// Get all tags
        
        public Task<List<Tag>> GetAllTagsAsync()
        {
            return Task.FromResult(_tags.OrderBy(t => t.Name).ToList());
        }

        
        /// Get statistics
        
        public Task<(int TotalEntries, int CurrentStreak, int LongestStreak)> GetStatisticsAsync()
        {
            int totalEntries = _entries.Count;
            int currentStreak = CalculateCurrentStreak();
            int longestStreak = CalculateLongestStreak();

            return Task.FromResult((totalEntries, currentStreak, longestStreak));
        }

        private int CalculateCurrentStreak()
        {
            if (!_entries.Any()) return 0;

            var sortedEntries = _entries.OrderByDescending(e => e.Date).ToList();
            int streak = 0;
            var today = DateOnly.FromDateTime(DateTime.Today);

            for (int i = 0; i < sortedEntries.Count; i++)
            {
                var expectedDate = today.AddDays(-i);
                if (sortedEntries[i].Date == expectedDate)
                    streak++;
                else
                    break;
            }

            return streak;
        }

        private int CalculateLongestStreak()
        {
            if (!_entries.Any()) return 0;

            var sortedEntries = _entries.OrderByDescending(e => e.Date).ToList();
            int maxStreak = 1;
            int currentStreak = 1;

            for (int i = 1; i < sortedEntries.Count; i++)
            {
                if (sortedEntries[i - 1].Date.AddDays(-1) == sortedEntries[i].Date)
                {
                    currentStreak++;
                    maxStreak = Math.Max(maxStreak, currentStreak);
                }
                else
                {
                    currentStreak = 1;
                }
            }

            return maxStreak;
        }

        private async Task SaveEntriesAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_entriesFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving entries to file: {ex}");
            }
        }

        private async Task SaveTagsAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_tags, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_tagsFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving tags to file: {ex}");
            }
        }
    }
}
