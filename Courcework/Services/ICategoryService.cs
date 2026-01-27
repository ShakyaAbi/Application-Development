using Courcework.Common;

namespace Courcework.Services
{
    /// <summary>
    /// Service for managing predefined journal categories
    /// </summary>
    public interface ICategoryService
    {
        Task<ServiceResult<List<string>>> GetAllCategoriesAsync();
        Task<ServiceResult<List<string>>> GetCategoriesByTypeAsync(string type);
    }

    public class CategoryService : ICategoryService
    {
        /// <summary>
        /// Predefined journal categories organized by type (simplified list)
        /// </summary>
        private static readonly Dictionary<string, List<string>> Categories = new()
        {
            { "Work", new List<string> { "Work", "Studies", "Projects", "Career" } },
            { "Health", new List<string> { "Health", "Fitness", "Mental Health", "Self-care" } },
            { "Relationships", new List<string> { "Family", "Friends", "Social", "Personal" } },
            { "Interests", new List<string> { "Hobbies", "Travel", "Reading", "Creativity" } },
            { "Events", new List<string> { "Birthday", "Holiday", "Celebration", "Special" } }
        };

        /// <summary>
        /// Get all available categories
        /// </summary>
        public async Task<ServiceResult<List<string>>> GetAllCategoriesAsync()
        {
            try
            {
                var allCategories = Categories.Values
                    .SelectMany(x => x)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"? Loaded {allCategories.Count} categories");
                return ServiceResult<List<string>>.Ok(allCategories);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error loading categories: {ex.Message}");
                return ServiceResult<List<string>>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Get categories by type (Positive, Neutral, Negative, etc.)
        /// </summary>
        public async Task<ServiceResult<List<string>>> GetCategoriesByTypeAsync(string type)
        {
            try
            {
                if (!Categories.ContainsKey(type))
                    return ServiceResult<List<string>>.Fail($"Category type '{type}' not found");

                var categoryList = Categories[type].OrderBy(x => x).ToList();
                return ServiceResult<List<string>>.Ok(categoryList);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error loading {type} categories: {ex.Message}");
                return ServiceResult<List<string>>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Get all category types/groups
        /// </summary>
        public List<string> GetCategoryTypes()
        {
            return Categories.Keys.OrderBy(x => x).ToList();
        }
    }
}
