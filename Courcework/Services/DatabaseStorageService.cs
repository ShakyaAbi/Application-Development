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
        private readonly IAuthenticationService _authService;

        public DatabaseStorageService(JournalDbContext context, IAuthenticationService authService = null)
        {
            _context = context;
            _initializer = new DatabaseInitializer(context);
            _authService = authService;
        }

        /// <summary>
        /// Initialize the database on app startup
        /// </summary>
        public async Task InitializeAsync()
        {
            await _initializer.InitializeAsync();
        }

        /// <summary>
        /// Get entry by specific date (filtered by current user)
        /// </summary>
        public async Task<ServiceResult<JournalEntry?>> GetEntryByDateAsync(DateOnly date)
        {
            try
            {
                // ? Get current user safely
                var currentUser = await GetCurrentUserSafeAsync();
                if (currentUser == null)
                {
                    // If no auth service, return all entries (fallback for testing)
                    var entryNoAuth = await _context.JournalEntries
                        .FirstOrDefaultAsync(e => e.Date == date);
                    return ServiceResult<JournalEntry?>.Ok(entryNoAuth);
                }

                var entry = await _context.JournalEntries
                    .FirstOrDefaultAsync(e => e.Date == date && e.UserId == currentUser.Id);
                return ServiceResult<JournalEntry?>.Ok(entry);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting entry by date: {ex.Message}");
                return ServiceResult<JournalEntry?>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Get all entries ordered by date (newest first, filtered by current user)
        /// </summary>
        public async Task<ServiceResult<List<JournalEntry>>> GetAllEntriesAsync()
        {
            try
            {
                // ? Get current user safely
                var currentUser = await GetCurrentUserSafeAsync();
                if (currentUser == null)
                {
                    // If no auth service, return all entries (fallback for testing)
                    var allEntries = await _context.JournalEntries
                        .OrderByDescending(e => e.Date)
                        .ToListAsync();
                    return ServiceResult<List<JournalEntry>>.Ok(allEntries);
                }

                var entries = await _context.JournalEntries
                    .Where(e => e.UserId == currentUser.Id)
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
        /// Get entries for a specific month (filtered by current user)
        /// </summary>
        public async Task<ServiceResult<List<JournalEntry>>> GetEntriesByMonthAsync(int year, int month)
        {
            try
            {
                // ? Get current user safely
                var currentUser = await GetCurrentUserSafeAsync();
                if (currentUser == null)
                {
                    // If no auth service, return all entries for month (fallback)
                    var allEntries = await _context.JournalEntries
                        .Where(e => e.Date.Year == year && e.Date.Month == month)
                        .OrderByDescending(e => e.Date)
                        .ToListAsync();
                    return ServiceResult<List<JournalEntry>>.Ok(allEntries);
                }

                var entries = await _context.JournalEntries
                    .Where(e => e.Date.Year == year && e.Date.Month == month && e.UserId == currentUser.Id)
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
                // ? Get current user safely
                var currentUser = await GetCurrentUserSafeAsync();
                if (currentUser == null)
                {
                    return ServiceResult<bool>.Fail("User not authenticated");
                }

                // ? Ensure entry has required fields
                if (string.IsNullOrWhiteSpace(entry.Content))
                {
                    return ServiceResult<bool>.Fail("Content is required");
                }

                // ? Ensure entry has valid date
                if (entry.Date == default(DateOnly))
                {
                    entry.Date = DateOnly.FromDateTime(DateTime.Today);
                }

                var existing = await _context.JournalEntries.FindAsync(entry.Id);

                if (existing == null)
                {
                    entry.UserId = currentUser.Id;  // ? SET USER ID FOR NEW ENTRY
                    entry.CreatedAt = DateTime.Now;
                    entry.UpdatedAt = DateTime.Now;
                    
                    // ? Ensure Tags is initialized
                    if (entry.Tags == null)
                    {
                        entry.Tags = new();
                    }
                    
                    // ? Ensure empty strings instead of null
                    entry.Title = entry.Title ?? string.Empty;
                    entry.Mood = entry.Mood ?? string.Empty;
                    entry.SecondaryMood = entry.SecondaryMood ?? string.Empty;
                    entry.Category = entry.Category ?? string.Empty;
                    
                    _context.JournalEntries.Add(entry);
                    System.Diagnostics.Debug.WriteLine($"Creating new entry: {entry.Id}, UserId: {entry.UserId}, Date: {entry.Date}");
                }
                else
                {
                    existing.Title = entry.Title ?? string.Empty;
                    existing.Content = entry.Content;
                    existing.Mood = entry.Mood ?? string.Empty;
                    existing.SecondaryMood = entry.SecondaryMood ?? string.Empty;
                    existing.Category = entry.Category ?? string.Empty;
                    existing.Tags = entry.Tags ?? new();
                    existing.IsRichText = entry.IsRichText;
                    existing.Date = entry.Date;
                    existing.UpdatedAt = DateTime.Now;
                    System.Diagnostics.Debug.WriteLine($"Updating entry: {entry.Id}");
                }

                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"? Entry saved successfully!");
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error saving entry: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"? Inner Exception: {ex.InnerException?.Message}");
                System.Diagnostics.Debug.WriteLine($"? Stack Trace: {ex.StackTrace}");
                return ServiceResult<bool>.Fail($"Error saving entry: {ex.Message}");
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
        /// Get all available tags (filtered by current user)
        /// </summary>
        public async Task<ServiceResult<List<Tag>>> GetAllTagsAsync()
        {
            try
            {
                // ? GET CURRENT USER safely
                var currentUser = await GetCurrentUserSafeAsync();
                
                if (currentUser == null)
                {
                    // If no auth, return all tags (fallback)
                    var allTags = await _context.Tags
                        .OrderBy(t => t.Name)
                        .ToListAsync();
                    return ServiceResult<List<Tag>>.Ok(allTags);
                }

                var tags = await _context.Tags
                    .Where(t => t.UserId == currentUser.Id)  // ? FILTER BY USER
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
                // ? GET CURRENT USER safely
                var currentUser = await GetCurrentUserSafeAsync();
                
                if (currentUser == null)
                {
                    return ServiceResult<Tag>.Fail("User not authenticated");
                }

                // Check if tag already exists for this user
                var exists = await _context.Tags
                    .AnyAsync(t => t.Name.ToLower() == name.ToLower() && t.UserId == currentUser.Id);
                
                if (exists)
                {
                    var existingTag = await _context.Tags
                        .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower() && t.UserId == currentUser.Id);
                    return ServiceResult<Tag>.Ok(existingTag);
                }

                var tag = new Tag 
                { 
                    Id = Guid.NewGuid(), 
                    UserId = currentUser.Id,  // ? SET USERID
                    Name = name, 
                    Color = color 
                };
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
        /// Get statistics about entries (totals, streaks) - filtered by current user
        /// </summary>
        public async Task<ServiceResult<(int TotalEntries, int CurrentStreak, int LongestStreak)>> GetStatisticsAsync()
        {
            try
            {
                // ? GET CURRENT USER safely
                var currentUser = await GetCurrentUserSafeAsync();
                if (currentUser == null)
                {
                    return ServiceResult<(int, int, int)>.Fail("User not authenticated");
                }

                // ? FILTER BY CURRENT USER
                var entries = await _context.JournalEntries
                    .Where(e => e.UserId == currentUser.Id)
                    .OrderByDescending(e => e.Date)
                    .ToListAsync();

                int totalEntries = entries.Count;
                int currentStreak = CalculateCurrentStreak(entries);
                int longestStreak = CalculateLongestStreak(entries);

                System.Diagnostics.Debug.WriteLine($"Statistics - Total: {totalEntries}, Current Streak: {currentStreak}, Longest: {longestStreak}");
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

            // ? Create a set of dates for quick lookup
            var entryDates = new HashSet<DateOnly>(entries.Select(e => e.Date));
            
            int streak = 0;
            var today = DateOnly.FromDateTime(DateTime.Today);
            
            // ? Start from today and go backwards
            for (int i = 0; i < 365; i++)  // Check up to a year back
            {
                var checkDate = today.AddDays(-i);
                
                if (entryDates.Contains(checkDate))
                {
                    streak++;
                }
                else
                {
                    // ? Break if we find a day without an entry
                    break;
                }
            }

            System.Diagnostics.Debug.WriteLine($"Current Streak Calculation - Today: {today}, Streak: {streak}, Dates: {string.Join(",", entryDates.OrderByDescending(d => d).Take(10))}");
            return streak;
        }

        private int CalculateLongestStreak(List<JournalEntry> entries)
        {
            if (!entries.Any()) return 0;

            // ? Sort entries by date in ascending order
            var sortedEntries = entries.OrderBy(e => e.Date).ToList();
            
            int maxStreak = 1;
            int currentStreak = 1;

            for (int i = 1; i < sortedEntries.Count; i++)
            {
                // ? Check if consecutive days
                if (sortedEntries[i - 1].Date.AddDays(1) == sortedEntries[i].Date)
                {
                    currentStreak++;
                    maxStreak = Math.Max(maxStreak, currentStreak);
                }
                else
                {
                    currentStreak = 1;
                }
            }

            System.Diagnostics.Debug.WriteLine($"Longest Streak Calculation - Max: {maxStreak}");
            return maxStreak;
        }

        /// <summary>
        /// Get count of entries for a specific date range - filtered by current user
        /// </summary>
        public async Task<ServiceResult<int>> GetEntryCountAsync(DateOnly startDate, DateOnly endDate)
        {
            try
            {
                // ? GET CURRENT USER safely
                var currentUser = await GetCurrentUserSafeAsync();
                if (currentUser == null)
                {
                    return ServiceResult<int>.Fail("User not authenticated");
                }

                // ? FILTER BY CURRENT USER
                var count = await _context.JournalEntries
                    .CountAsync(e => e.Date >= startDate && e.Date <= endDate && e.UserId == currentUser.Id);
                return ServiceResult<int>.Ok(count);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error counting entries: {ex.Message}");
                return ServiceResult<int>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Get total word count across all entries - filtered by current user
        /// </summary>
        public async Task<ServiceResult<int>> GetTotalWordCountAsync()
        {
            try
            {
                // ? GET CURRENT USER safely
                var currentUser = await GetCurrentUserSafeAsync();
                if (currentUser == null)
                {
                    return ServiceResult<int>.Fail("User not authenticated");
                }

                // ? FILTER BY CURRENT USER
                var entries = await _context.JournalEntries
                    .Where(e => e.UserId == currentUser.Id)
                    .ToListAsync();
                    
                var wordCount = entries.Sum(e => e.Content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length);
                
                System.Diagnostics.Debug.WriteLine($"Total word count: {wordCount}");
                return ServiceResult<int>.Ok(wordCount);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating word count: {ex.Message}");
                return ServiceResult<int>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Helper method to safely get current user
        /// </summary>
        private async Task<User> GetCurrentUserSafeAsync()
        {
            try
            {
                if (_authService == null)
                {
                    System.Diagnostics.Debug.WriteLine("Auth service is null, returning null user");
                    return null;
                }

                var userResult = await _authService.GetCurrentUserAsync();
                return userResult?.Data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting current user: {ex.Message}");
                return null;
            }
        }
    }
}
