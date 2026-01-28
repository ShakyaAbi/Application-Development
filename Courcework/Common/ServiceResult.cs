namespace Courcework.Common
{
    
    /// Generic result wrapper for all service operations
    /// Provides success/failure status, data, and error messages
    
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }

        public static ServiceResult<T> Ok(T data) => new()
        {
            Success = true,
            Data = data
        };

        public static ServiceResult<T> Fail(string error) => new()
        {
            Success = false,
            ErrorMessage = error
        };
    }
}
