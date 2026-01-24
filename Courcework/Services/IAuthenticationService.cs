using Courcework.Common;
using Courcework.Entities;

namespace Courcework.Services
{
    /// <summary>
    /// Interface for user authentication operations
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Register a new user account
        /// </summary>
        Task<ServiceResult<string>> RegisterAsync(string username, string email, string fullName, string password);

        /// <summary>
        /// Authenticate user with credentials
        /// </summary>
        Task<ServiceResult<string>> LoginAsync(string usernameOrEmail, string password);

        /// <summary>
        /// Logout current user
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Get current authenticated user
        /// </summary>
        Task<ServiceResult<User?>> GetCurrentUserAsync();

        /// <summary>
        /// Validate if password is strong enough
        /// </summary>
        Task<ServiceResult<bool>> ValidatePasswordStrengthAsync(string password);

        /// <summary>
        /// Change user password
        /// </summary>
        Task<ServiceResult<bool>> ChangePasswordAsync(string currentPassword, string newPassword);

        /// <summary>
        /// Reset password with token
        /// </summary>
        Task<ServiceResult<bool>> ResetPasswordAsync(string token, string newPassword);

        /// <summary>
        /// Check if user is authenticated
        /// </summary>
        Task<bool> IsAuthenticatedAsync();

        /// <summary>
        /// Get authentication token
        /// </summary>
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
