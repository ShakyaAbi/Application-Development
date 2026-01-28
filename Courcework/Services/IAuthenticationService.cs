using Courcework.Common;
using Courcework.Entities;

namespace Courcework.Services
{
    
    /// Interface for user authentication operations
    
    public interface IAuthenticationService
    {
        
        /// Register a new user account
        
        Task<ServiceResult<string>> RegisterAsync(string username, string email, string fullName, string password);

        
        /// Authenticate user with credentials
        
        Task<ServiceResult<string>> LoginAsync(string usernameOrEmail, string password);

        
        /// Logout current user
        
        Task LogoutAsync();

        
        /// Get current authenticated user
        
        Task<ServiceResult<User?>> GetCurrentUserAsync();

        
        /// Validate if password is strong enough
        
        Task<ServiceResult<bool>> ValidatePasswordStrengthAsync(string password);

        
        /// Change user password
        
        Task<ServiceResult<bool>> ChangePasswordAsync(string currentPassword, string newPassword);

        
        /// Reset password with token
        
        Task<ServiceResult<bool>> ResetPasswordAsync(string token, string newPassword);

        
        /// Check if user is authenticated
        
        Task<bool> IsAuthenticatedAsync();

        
        /// Get authentication token
        
        Task<ServiceResult<string>> GetTokenAsync();
    }

    // Models for authentication requests
    public class LoginRequest
    {
        public string UsernameOrEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public User User { get; set; } = new();
        public DateTime ExpiresAt { get; set; }
    }
}
