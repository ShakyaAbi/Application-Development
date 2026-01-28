// Theme Service for Light/Dark mode management
using Courcework.Common;

namespace Courcework.Services
{
    public interface IThemeService
    {
        Task<string> GetThemeAsync();
        Task SetThemeAsync(string theme);
        event Action<string>? ThemeChanged;
    }

    public class ThemeService : IThemeService
    {
        private readonly ISecureStorageService _secureStorage;
        private string _currentTheme = "light";
        
        public event Action<string>? ThemeChanged;

        public ThemeService(ISecureStorageService secureStorage)
        {
            _secureStorage = secureStorage;
        }

        public async Task<string> GetThemeAsync()
        {
            try
            {
                _currentTheme = await _secureStorage.GetAsync("app_theme") ?? "light";
                return _currentTheme;
            }
            catch
            {
                return "light";
            }
        }

        public async Task SetThemeAsync(string theme)
        {
            if (theme != "light" && theme != "dark")
                throw new ArgumentException("Theme must be 'light' or 'dark'");

            try
            {
                await _secureStorage.SetAsync("app_theme", theme);
                _currentTheme = theme;
                ThemeChanged?.Invoke(theme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting theme: {ex.Message}");
                throw;
            }
        }
    }
}
