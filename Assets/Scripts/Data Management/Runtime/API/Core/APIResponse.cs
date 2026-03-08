namespace Data_Management.Runtime.API.Core
{
    /// <summary>
    /// Generic container for API responses.
    /// Wraps the deserialized data along with success/error information.
    /// </summary>
    /// <typeparam name="T">The type of data expected in the response</typeparam>
    public class APIResponse<T>
    {
        /// <summary>
        /// Whether the request completed successfully
        /// </summary>
        public bool success;

        /// <summary>
        /// The deserialized response data (null if request failed or parsing failed)
        /// </summary>
        public T data;

        /// <summary>
        /// The raw JSON response string (useful for debugging)
        /// </summary>
        public string rawJson;

        /// <summary>
        /// Error message if request failed (null if successful)
        /// </summary>
        public string error;

        /// <summary>
        /// HTTP status code (e.g., 200, 404, 500)
        /// </summary>
        public long statusCode;
    }
}
