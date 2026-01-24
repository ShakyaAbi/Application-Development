using Microsoft.EntityFrameworkCore;
using Courcework.Common;
using Courcework.Entities;
using Courcework.Data;

namespace Courcework.Services
{
    /// <summary>
    /// Database access service using Entity Framework Core with SQLite
    /// Implements IStorageService with ServiceResult<T> for consistent error handling
    /// Directly accesses data through DbContext (simplified architecture)
    /// </summary>
    public class DatabaseStorageService : IStorageService
    {
        private readonly JournalDbContext _context;
        private readonly DatabaseInitializer _initializer;

        public DatabaseStorageService(JournalDbContext context)
        {
            _context = context;
            _initializer = new DatabaseInitializer(context);
        }

        /// <summary>
        /// Initialize the database on app startup
        /// </summary>
        public async Task InitializeAsync()
        {
            await _initializer.InitializeAsync();
        }

        /// <summary>
        /// Get entry by specific date
        /// </summary>
        public async Task<ServiceResult<JournalEntry?>> GetEntryByDateAsync(DateOnly date)
        {
            try
            {
                var entry = await _context.JournalEntries
                    .FirstOrDefaultAsync(e => e.Date == date);
                return ServiceResult<JournalEntry?>.Ok(entry);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting entry by date: {ex.Message}");
                return ServiceResult<JournalEntry?>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Get all entries ordered by date (newest first)
        /// </summary>
        public async Task<ServiceResult<List<JournalEntry>>> GetAllEntriesAsync()
        {
            try
            {
                var entries = await _context.JournalEntries
                    .OrderByDescending(e => e.Date)
                    .ToListAsync();
                return ServiceResult<List<JournalEntry>>.Ok(entries);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting all entries: {ex.Message}");
                return ServiceResult<List<JournalEntry>>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Get entries for a specific month
        /// </summary>
        public async Task<ServiceResult<List<JournalEntry>>> GetEntriesByMonthAsync(int year, int month)
        {
            try
            {
                var entries = await _context.JournalEntries
                    .Where(e => e.Date.Year == year && e.Date.Month == month)
                    .OrderByDescending(e => e.Date)
                    .ToListAsync();
                return ServiceResult<List<JournalEntry>>.Ok(entries);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting entries by month: {ex.Message}");
                return ServiceResult<List<JournalEntry>>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Search entries by content (case-insensitive)
        /// </summary>
        public async Task<ServiceResult<List<JournalEntry>>> SearchEntriesAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await GetAllEntriesAsync();
                }

                var lowerSearch = searchTerm.ToLower();
                var entries = await _context.JournalEntries
                    .Where(e => e.Title.ToLower().Contains(lowerSearch) || 
                                e.Content.ToLower().Contains(lowerSearch))
                    .OrderByDescending(e => e.Date)
                    .ToListAsync();
                return ServiceResult<List<JournalEntry>>.Ok(entries);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching entries: {ex.Message}");
                return ServiceResult<List<JournalEntry>>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Get entries by mood
        /// </summary>
        public async Task<ServiceResult<List<JournalEntry>>> GetEntriesByMoodAsync(string mood)
        {
            try
            {
                var entries = await _context.JournalEntries
                    .Where(e => e.Mood == mood)
                    .OrderByDescending(e => e.Date)
                    .ToListAsync();
                return ServiceResult<List<JournalEntry>>.Ok(entries);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting entries by mood: {ex.Message}");
                return ServiceResult<List<JournalEntry>>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Save or update an entry
        /// </summary>
        public async Task<ServiceResult<bool>> SaveEntryAsync(JournalEntry entry)
        {
            try
            {
                var existing = await _context.JournalEntries.FindAsync(entry.Id);

                if (existing == null)
                {
                    entry.CreatedAt = DateTime.Now;
                    entry.UpdatedAt = DateTime.Now;
                    _context.JournalEntries.Add(entry);
                    System.Diagnostics.Debug.WriteLine($"Creating new entry: {entry.Id}");
                }
                else
                {
                    existing.Title = entry.Title;
                    existing.Content = entry.Content;
                    existing.Mood = entry.Mood;
                    existing.SecondaryMood = entry.SecondaryMood;
                    existing.Category = entry.Category;
                    existing.Tags = entry.Tags;
                    existing.IsRichText = entry.IsRichText;
                    existing.UpdatedAt = DateTime.Now;
                    System.Diagnostics.Debug.WriteLine($"Updating entry: {entry.Id}");
                }

                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving entry: {ex.Message}");
                return ServiceResult<bool>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Delete an entry by ID
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteEntryAsync(Guid entryId)
        {
            try
            {
                var entry = await _context.JournalEntries.FindAsync(entryId);
                if (entry == null)
                {
                    return ServiceResult<bool>.Fail("Entry not found");
                }

                _context.JournalEntries.Remove(entry);
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"Deleted entry: {entryId}");
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting entry: {ex.Message}");
                return ServiceResult<bool>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Get all available tags
        /// </summary>
        public async Task<ServiceResult<List<Tag>>> GetAllTagsAsync()
        {
            try
            {
                var tags = await _context.Tags
                    .OrderBy(t => t.Name)
                    .ToListAsync();
                return ServiceResult<List<Tag>>.Ok(tags);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting tags: {ex.Message}");
                return ServiceResult<List<Tag>>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Add a new tag
        /// </summary>
        public async Task<ServiceResult<Tag>> AddTagAsync(string name, string color = "#1976d2")
        {
            try
            {
                // Check if tag already exists
                var exists = await _context.Tags
                    .AnyAsync(t => t.Name.ToLower() == name.ToLower());
                
                if (exists)
                {
                    var existingTag = await _context.Tags
                        .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
                    return ServiceResult<Tag>.Ok(existingTag);
                }

                var tag = new Tag { Id = Guid.NewGuid(), Name = name, Color = color };
                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();
                return ServiceResult<Tag>.Ok(tag);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding tag: {ex.Message}");
                return ServiceResult<Tag>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Get statistics about entries (totals, streaks)
        /// </summary>
        public async Task<ServiceResult<(int TotalEntries, int CurrentStreak, int LongestStreak)>> GetStatisticsAsync()
        {
            try
            {
                var entries = await _context.JournalEntries
                    .OrderByDescending(e => e.Date)
                    .ToListAsync();

                int totalEntries = entries.Count;
                int currentStreak = CalculateCurrentStreak(entries);
                int longestStreak = CalculateLongestStreak(entries);

                return ServiceResult<(int, int, int)>.Ok((totalEntries, currentStreak, longestStreak));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating statistics: {ex.Message}");
                return ServiceResult<(int, int, int)>.Fail(ex.Message);
            }
        }

        private int CalculateCurrentStreak(List<JournalEntry> entries)
        {
            if (!entries.Any()) return 0;

            int streak = 0;
            var today = DateOnly.FromDateTime(DateTime.Today);

            for (int i = 0; i < entries.Count; i++)
            {
                var expectedDate = today.AddDays(-i);
                if (entries[i].Date == expectedDate)
                    streak++;
                else
                    break;
            }

            return streak;
        }

        private int CalculateLongestStreak(List<JournalEntry> entries)
        {
            if (!entries.Any()) return 0;

            int maxStreak = 1;
            int currentStreak = 1;

            for (int i = 1; i < entries.Count; i++)
            {
                if (entries[i - 1].Date.AddDays(-1) == entries[i].Date)
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

        /// <summary>
        /// Get count of entries for a specific date range
        /// </summary>
        public async Task<ServiceResult<int>> GetEntryCountAsync(DateOnly startDate, DateOnly endDate)
        {
            try
            {
                var count = await _context.JournalEntries
                    .CountAsync(e => e.Date >= startDate && e.Date <= endDate);
                return ServiceResult<int>.Ok(count);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error counting entries: {ex.Message}");
                return ServiceResult<int>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Get total word count across all entries
        /// </summary>
        public async Task<ServiceResult<int>> GetTotalWordCountAsync()
        {
            try
            {
                var entries = await _context.JournalEntries.ToListAsync();
                var wordCount = entries.Sum(e => e.Content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length);
                return ServiceResult<int>.Ok(wordCount);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating word count: {ex.Message}");
                return ServiceResult<int>.Fail(ex.Message);
            }
        }
    }
}
