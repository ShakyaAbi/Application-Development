namespace Courcework.Entities
{
    using Courcework.Enums;

    public class JournalEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }  // Link to User
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public Mood? Mood { get; set; }  // Changed to enum
        public string SecondaryMood { get; set; } = string.Empty;  // Keep as string for now
        public string Category { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public List<string> Tags { get; set; } = new();
        public bool IsRichText { get; set; } = false;
    }
}
