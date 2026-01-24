namespace Courcework.Services
{
    /// <summary>
    /// Interface for secure storage operations
    /// </summary>
    public interface ISecureStorageService
    {
        /// <summary>
        /// Store a value securely
        /// </summary>
        Task SetAsync(string key, string value);

        /// <summary>
        /// Retrieve a securely stored value
        /// </summary>
        Task<string?> GetAsync(string key);

        /// <summary>
        /// Remove a stored value
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// Check if a key exists
        /// </summary>
        Task<bool> ExistsAsync(string key);
    }
}
