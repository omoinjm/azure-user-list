namespace Core.Models
{
    /// <summary>
    /// Result of a user synchronization operation.
    /// 
    /// WHY: Provides structured result instead of returning raw data.
    /// Includes metadata (success, counts, execution time) for monitoring.
    /// </summary>
    public class SyncResult
    {
        /// <summary>
        /// Whether the synchronization succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Number of users fetched from Azure AD.
        /// </summary>
        public int UserCount { get; set; }

        /// <summary>
        /// Number of database rows affected.
        /// </summary>
        public int RowsAffected { get; set; }

        /// <summary>
        /// Human-readable status message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Total execution time of the synchronization.
        /// Useful for monitoring performance.
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }
    }
}