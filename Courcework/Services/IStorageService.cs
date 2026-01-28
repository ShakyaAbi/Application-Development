using Courcework.Common;
using Courcework.Entities;

namespace Courcework.Services
{
    
    /// Interface for data storage operations
    /// Allows flexibility to switch between implementations (SQLite, JSON, etc.)
    
    public interface IStorageService
    {
        Task InitializeAsync();
        Task<ServiceResult<JournalEntry?>> GetEntryByDateAsync(DateOnly date);
        Task<ServiceResult<List<JournalEntry>>> GetAllEntriesAsync();
        Task<ServiceResult<List<JournalEntry>>> GetEntriesByMonthAsync(int year, int month);
        Task<ServiceResult<List<JournalEntry>>> SearchEntriesAsync(string searchTerm);
        Task<ServiceResult<List<JournalEntry>>> GetEntriesByMoodAsync(string mood);
        Task<ServiceResult<bool>> SaveEntryAsync(JournalEntry entry);
        Task<ServiceResult<bool>> DeleteEntryAsync(Guid entryId);
        Task<ServiceResult<List<Tag>>> GetAllTagsAsync();
        Task<ServiceResult<Tag>> AddTagAsync(string name, string color = "#1976d2");
        Task<ServiceResult<(int TotalEntries, int CurrentStreak, int LongestStreak)>> GetStatisticsAsync();
        Task<ServiceResult<int>> GetEntryCountAsync(DateOnly startDate, DateOnly endDate);
        Task<ServiceResult<int>> GetTotalWordCountAsync();
    }
}
