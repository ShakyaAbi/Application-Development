namespace Courcework.Entities
{
    public class Tag
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }  
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#1976d2";
    }
}

