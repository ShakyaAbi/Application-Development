namespace Courcework.Entities
{
    
    /// Represents a user account in the system
    
    public class User
    {
        
        /// Unique identifier for the user
        
        public Guid Id { get; set; } = Guid.NewGuid();

        
        /// Username for login
        
        public string Username { get; set; } = string.Empty;

        
        /// Email address
        
        public string Email { get; set; } = string.Empty;

        
        /// Full name of the user
        
        public string FullName { get; set; } = string.Empty;

        
        /// Hashed password (using bcrypt or similar)
        
        public string PasswordHash { get; set; } = string.Empty;

        
        /// Account creation timestamp
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        
        /// Last login timestamp
        
        public DateTime? LastLoginAt { get; set; }

        
        /// Whether the account is active
        
        public bool IsActive { get; set; } = true;

        
        /// User's preferred theme (light/dark)
        
        public string PreferredTheme { get; set; } = "light";
    }
}
