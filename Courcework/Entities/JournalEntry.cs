namespace Courcework.Entities
{
    public class JournalEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public string Mood { get; set; } = string.Empty;
        public string SecondaryMood { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public List<string> Tags { get; set; } = new();
        public bool IsRichText { get; set; } = false;
    }
}
