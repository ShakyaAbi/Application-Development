using Courcework.Common;
using Courcework.Data;
using Courcework.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Courcework.Services
{
    /// <summary>
    /// Authentication service implementation using SQLite
    /// Handles user registration, login, and password management
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly JournalDbContext _context;
        private readonly ISecureStorageService _secureStorage;
        private User? _currentUser;
        private string? _currentToken;

        // Password hashing configuration
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 10000;
        private const string TokenKey = "auth_token";

        public AuthenticationService(JournalDbContext context, ISecureStorageService secureStorage)
        {
            _context = context;
            _secureStorage = secureStorage;
        }

        /// <summary>
        /// Register a new user account
        /// </summary>
        public async Task<ServiceResult<string>> RegisterAsync(string username, string email, string fullName, string password)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(username))
                    return ServiceResult<string>.Fail("Username cannot be empty");

                if (string.IsNullOrWhiteSpace(email))
                    return ServiceResult<string>.Fail("Email cannot be empty");

                if (string.IsNullOrWhiteSpace(password))
                    return ServiceResult<string>.Fail("Password cannot be empty");

                // Check if username already exists
                var existingUsername = await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());
                if (existingUsername)
                    return ServiceResult<string>.Fail("Username already exists");

                // Check if email already exists
                var existingEmail = await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
                if (existingEmail)
                    return ServiceResult<string>.Fail("Email already exists");

                // Validate password strength
                var strengthResult = await ValidatePasswordStrengthAsync(password);
                if (!strengthResult.Success)
                    return ServiceResult<string>.Fail(strengthResult.ErrorMessage);

                // Hash password
                var passwordHash = HashPassword(password);

                // Create new user
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    Email = email,
                    FullName = fullName,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Generate token
                var token = GenerateToken(user);

                // Store token securely
                await _secureStorage.SetAsync(TokenKey, token);
                _currentUser = user;
                _currentToken = token;

                System.Diagnostics.Debug.WriteLine($"User registered: {username}");
                return ServiceResult<string>.Ok(token);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Registration error: {ex.Message}");
                return ServiceResult<string>.Fail($"Registration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Authenticate user with credentials
        /// </summary>
        public async Task<ServiceResult<string>> LoginAsync(string usernameOrEmail, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(usernameOrEmail))
                    return ServiceResult<string>.Fail("Username or email is required");

                if (string.IsNullOrWhiteSpace(password))
                    return ServiceResult<string>.Fail("Password is required");

                // Find user by username or email
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Username.ToLower() == usernameOrEmail.ToLower() ||
                    u.Email.ToLower() == usernameOrEmail.ToLower());

                if (user == null)
                    return ServiceResult<string>.Fail("Invalid username or password");

                if (!user.IsActive)
                    return ServiceResult<string>.Fail("Account is inactive");

                // Verify password
                if (!VerifyPassword(password, user.PasswordHash))
                    return ServiceResult<string>.Fail("Invalid username or password");

                // Update last login
                user.LastLoginAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // Generate token
                var token = GenerateToken(user);

                // Store token securely
                await _secureStorage.SetAsync(TokenKey, token);
                _currentUser = user;
                _currentToken = token;

                System.Diagnostics.Debug.WriteLine($"User logged in: {user.Username}");
                return ServiceResult<string>.Ok(token);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                return ServiceResult<string>.Fail($"Login failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Logout current user
        /// </summary>
        public async Task LogoutAsync()
        {
            try
            {
                if (_currentUser != null)
                {
                    System.Diagnostics.Debug.WriteLine($"User logged out: {_currentUser.Username}");
                }

                _currentUser = null;
                _currentToken = null;
                await _secureStorage.RemoveAsync(TokenKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current authenticated user
        /// </summary>
        public async Task<ServiceResult<User?>> GetCurrentUserAsync()
        {
            try
            {
                // Try to load from memory first
                if (_currentUser != null)
                {
                    // Refresh from database
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.Id);
                    if (user != null)
                    {
                        _currentUser = user;
                        return ServiceResult<User?>.Ok(_currentUser);
                    }
                }

                // Try to restore from secure storage
                var token = await _secureStorage.GetAsync(TokenKey);
                if (!string.IsNullOrEmpty(token))
                {
                    var userId = ExtractUserIdFromToken(token);
                    if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var id))
                    {
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
                        if (user != null)
                        {
                            _currentUser = user;
                            _currentToken = token;
                            return ServiceResult<User?>.Ok(_currentUser);
                        }
                    }
                }

                return ServiceResult<User?>.Ok(null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get current user error: {ex.Message}");
                return ServiceResult<User?>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Validate password strength
        /// </summary>
        public async Task<ServiceResult<bool>> ValidatePasswordStrengthAsync(string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(password))
                    return ServiceResult<bool>.Fail("Password cannot be empty");

                if (password.Length < 8)
                    return ServiceResult<bool>.Fail("Password must be at least 8 characters");

                if (!password.Any(char.IsUpper))
                    return ServiceResult<bool>.Fail("Password must contain at least one uppercase letter");

                if (!password.Any(char.IsLower))
                    return ServiceResult<bool>.Fail("Password must contain at least one lowercase letter");

                if (!password.Any(char.IsDigit))
                    return ServiceResult<bool>.Fail("Password must contain at least one number");

                if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
                    return ServiceResult<bool>.Fail("Password must contain at least one special character");

                return await Task.FromResult(ServiceResult<bool>.Ok(true));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Password validation error: {ex.Message}");
                return ServiceResult<bool>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        public async Task<ServiceResult<bool>> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            try
            {
                if (_currentUser == null)
                    return ServiceResult<bool>.Fail("No user is currently logged in");

                // Verify current password
                if (!VerifyPassword(currentPassword, _currentUser.PasswordHash))
                    return ServiceResult<bool>.Fail("Current password is incorrect");

                // Validate new password strength
                var strengthResult = await ValidatePasswordStrengthAsync(newPassword);
                if (!strengthResult.Success)
                    return ServiceResult<bool>.Fail(strengthResult.ErrorMessage);

                // Hash new password
                var newPasswordHash = HashPassword(newPassword);

                // Update password
                _currentUser.PasswordHash = newPasswordHash;
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"Password changed for user: {_currentUser.Username}");
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Change password error: {ex.Message}");
                return ServiceResult<bool>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Reset password with token
        /// </summary>
        public async Task<ServiceResult<bool>> ResetPasswordAsync(string token, string newPassword)
        {
            try
            {
                // TODO: Implement token validation
                // This would require token generation and validation logic

                return await Task.FromResult(ServiceResult<bool>.Ok(true));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Reset password error: {ex.Message}");
                return ServiceResult<bool>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Check if user is authenticated
        /// </summary>
        public async Task<bool> IsAuthenticatedAsync()
        {
            if (_currentUser != null && !string.IsNullOrEmpty(_currentToken))
                return true;

            // Try to restore from secure storage
            var token = await _secureStorage.GetAsync(TokenKey);
            return !string.IsNullOrEmpty(token);
        }

        /// <summary>
        /// Get authentication token
        /// </summary>
        public async Task<ServiceResult<string>> GetTokenAsync()
        {
            if (!string.IsNullOrEmpty(_currentToken))
                return ServiceResult<string>.Ok(_currentToken);

            var token = await _secureStorage.GetAsync(TokenKey);
            if (string.IsNullOrEmpty(token))
                return ServiceResult<string>.Fail("No token available");

            _currentToken = token;
            return ServiceResult<string>.Ok(token);
        }

        // ===== Helper Methods =====

        /// <summary>
        /// Hash password using PBKDF2
        /// </summary>
        private string HashPassword(string password)
        {
            using (var algorithm = new Rfc2898DeriveBytes(password, SaltSize, Iterations, HashAlgorithmName.SHA256))
            {
                var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
                var salt = Convert.ToBase64String(algorithm.Salt);

                return $"{Iterations}.{salt}.{key}";
            }
        }

        /// <summary>
        /// Verify password against hash
        /// </summary>
        private bool VerifyPassword(string password, string hash)
        {
            try
            {
                var parts = hash.Split('.', 3);

                if (parts.Length != 3)
                    return false;

                var iterations = int.Parse(parts[0]);
                var salt = Convert.FromBase64String(parts[1]);
                var key = Convert.FromBase64String(parts[2]);

                using (var algorithm = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                {
                    var keyToCheck = algorithm.GetBytes(KeySize);

                    return keyToCheck.SequenceEqual(key);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generate token with user claims
        /// </summary>
        private string GenerateToken(User user)
        {
            var claims = new Dictionary<string, object>
            {
                { "sub", user.Id.ToString() },
                { "username", user.Username },
                { "email", user.Email },
                { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                { "exp", DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds() }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(claims);
            var bytes = Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Extract user ID from token
        /// </summary>
        private string? ExtractUserIdFromToken(string token)
        {
            try
            {
                var bytes = Convert.FromBase64String(token);
                var json = Encoding.UTF8.GetString(bytes);
                var doc = System.Text.Json.JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("sub", out var subElement))
                {
                    return subElement.GetString();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
