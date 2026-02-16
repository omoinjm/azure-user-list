using System;
using System.Threading.Tasks;

namespace Infrastructure.Utilities
{
    /// <summary>
    /// Utility class for implementing retry policies with exponential backoff.
    /// </summary>
    public static class RetryPolicy
    {
        /// <summary>
        /// Executes an asynchronous operation with retry logic and exponential backoff.
        /// </summary>
        /// <typeparam name="T">Return type of the operation</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <param name="delay">Initial delay between retries</param>
        /// <returns>Result of the operation</returns>
        public static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, int maxRetries = 3, TimeSpan? delay = null)
        {
            var actualDelay = delay ?? TimeSpan.FromSeconds(1);
            var currentDelay = actualDelay;

            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception) when (i < maxRetries)
                {
                    await Task.Delay(currentDelay);
                    currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 2); // Exponential backoff
                }
            }

            // This should not be reached, but added for completeness
            throw new InvalidOperationException($"Operation failed after {maxRetries + 1} attempts.");
        }
    }
}