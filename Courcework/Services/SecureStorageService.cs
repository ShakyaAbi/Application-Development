namespace Courcework.Services
{
    /// <summary>
    /// Implementation of secure storage using MAUI SecureStorage
    /// </summary>
    public class SecureStorageService : ISecureStorageService
    {
        public async Task SetAsync(string key, string value)
        {
            await SecureStorage.SetAsync(key, value);
        }

        public async Task<string?> GetAsync(string key)
        {
            try
            {
                return await SecureStorage.GetAsync(key);
            }
            catch
            {
                return null;
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                SecureStorage.Remove(key);
                await Task.CompletedTask;
            }
            catch
            {
                await Task.CompletedTask;
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            var value = await GetAsync(key);
            return !string.IsNullOrEmpty(value);
        }
    }
}
