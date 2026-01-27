namespace Courcework.Entities
{
    /// <summary>
    /// Represents a user account in the system
    /// </summary>
    public class User
    {
        /// <summary>
        /// Unique identifier for the user
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Username for login
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Full name of the user
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Hashed password (using bcrypt or similar)
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Account creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Last login timestamp
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Whether the account is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// User's preferred theme (light/dark)
        /// </summary>
        public string PreferredTheme { get; set; } = "light";
    }
}
