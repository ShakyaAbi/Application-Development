namespace Courcework.Constants
{
    
    /// ? Mood categories and classifications
    /// Organizes moods by emotional sentiment
    
    public static class MoodConstants
    {
        // ? POSITIVE MOODS
        public static readonly string[] PositiveMoods = new[]
        {
            "Happy",
            "Excited",
            "Relaxed",
            "Grateful",
            "Confident"
        };

        // ? NEUTRAL MOODS
        public static readonly string[] NeutralMoods = new[]
        {
            "Calm",
            "Thoughtful",
            "Curious",
            "Nostalgic",
            "Bored"
        };

        // ? NEGATIVE MOODS
        public static readonly string[] NegativeMoods = new[]
        {
            "Sad",
            "Angry",
            "Stressed",
            "Lonely",
            "Anxious"
        };

        
        /// Get all moods organized by category
        
        public static Dictionary<string, string[]> GetMoodsByCategory()
        {
            return new Dictionary<string, string[]>
            {
                { "Positive", PositiveMoods },
                { "Neutral", NeutralMoods },
                { "Negative", NegativeMoods }
            };
        }

        
        /// Get all moods as a flat list
        
        public static string[] GetAllMoods()
        {
            return PositiveMoods
                .Concat(NeutralMoods)
                .Concat(NegativeMoods)
                .ToArray();
        }

        
        /// Get color for mood category
        
        public static string GetCategoryColor(string category)
        {
            return category switch
            {
                "Positive" => "#4caf50",  // Green
                "Neutral" => "#2196f3",   // Blue
                "Negative" => "#f44336",  // Red
                _ => "#9e9e9e"            // Gray
            };
        }

        
        /// Get mood category for a mood
        
        public static string GetMoodCategory(string mood)
        {
            if (PositiveMoods.Contains(mood)) return "Positive";
            if (NeutralMoods.Contains(mood)) return "Neutral";
            if (NegativeMoods.Contains(mood)) return "Negative";
            return "Unknown";
        }
    }
}
