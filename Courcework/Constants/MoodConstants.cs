namespace Courcework.Constants
{
    /// <summary>
    /// ? Mood categories and classifications
    /// Organizes moods by emotional sentiment
    /// </summary>
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

        /// <summary>
        /// Get all moods organized by category
        /// </summary>
        public static Dictionary<string, string[]> GetMoodsByCategory()
        {
            return new Dictionary<string, string[]>
            {
                { "Positive", PositiveMoods },
                { "Neutral", NeutralMoods },
                { "Negative", NegativeMoods }
            };
        }

        /// <summary>
        /// Get all moods as a flat list
        /// </summary>
        public static string[] GetAllMoods()
        {
            return PositiveMoods
                .Concat(NeutralMoods)
                .Concat(NegativeMoods)
                .ToArray();
        }

        /// <summary>
        /// Get color for mood category
        /// </summary>
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

        /// <summary>
        /// Get mood category for a mood
        /// </summary>
        public static string GetMoodCategory(string mood)
        {
            if (PositiveMoods.Contains(mood)) return "Positive";
            if (NeutralMoods.Contains(mood)) return "Neutral";
            if (NegativeMoods.Contains(mood)) return "Negative";
            return "Unknown";
        }
    }
}
