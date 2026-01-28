namespace Courcework.Services
{
    
    /// Interface for secure storage operations
    
    public interface ISecureStorageService
    {
        
        /// Store a value securely
        
        Task SetAsync(string key, string value);

        
        /// Retrieve a securely stored value
        
        Task<string?> GetAsync(string key);

        
        /// Remove a stored value
        
        Task RemoveAsync(string key);

        
        /// Check if a key exists
        
        Task<bool> ExistsAsync(string key);
    }
}
