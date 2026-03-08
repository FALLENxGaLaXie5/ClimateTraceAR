using UnityEngine.Networking;

namespace Data_Management.Runtime.API.Core
{
    /// <summary>
    /// Interface for API provider implementations that handle authentication,
    /// headers, URL construction, and retry logic for different APIs.
    /// </summary>
    public interface IAPIProvider
    {
        /// <summary>
        /// The base URL for the API (e.g., "https://api.climatetrace.org/v7")
        /// </summary>
        string BaseUrl { get; }

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        int Timeout { get; }

        /// <summary>
        /// Maximum number of retry attempts for failed requests
        /// </summary>
        int MaxRetries { get; }

        /// <summary>
        /// Minimum time between consecutive requests (rate limiting)
        /// </summary>
        float MinTimeBetweenRequests { get; }

        /// <summary>
        /// Apply API-specific authentication to the request.
        /// This could add headers, query parameters, or modify the request in other ways.
        /// </summary>
        /// <param name="request">The UnityWebRequest to modify</param>
        void ApplyAuthentication(UnityWebRequest request);

        /// <summary>
        /// Apply default headers required by this API (e.g., Content-Type, Accept)
        /// </summary>
        /// <param name="request">The UnityWebRequest to modify</param>
        void ApplyDefaultHeaders(UnityWebRequest request);

        /// <summary>
        /// Construct the full URL for a given endpoint.
        /// Combines base URL with endpoint and handles any API-specific URL formatting.
        /// </summary>
        /// <param name="endpoint">The endpoint path (e.g., "sources")</param>
        /// <returns>The complete URL</returns>
        string BuildEndpointUrl(string endpoint);

        /// <summary>
        /// Parse and extract error messages from failed requests.
        /// Different APIs may return errors in different formats (JSON, XML, plain text).
        /// </summary>
        /// <param name="request">The failed UnityWebRequest</param>
        /// <returns>A human-readable error message</returns>
        string ParseErrorMessage(UnityWebRequest request);

        /// <summary>
        /// Determine if a request should be retried based on the HTTP status code.
        /// </summary>
        /// <param name="statusCode">The HTTP status code</param>
        /// <returns>True if the request should be retried</returns>
        bool ShouldRetry(long statusCode);

        /// <summary>
        /// Calculate the delay before retrying a failed request.
        /// Typically implements exponential backoff.
        /// </summary>
        /// <param name="attemptNumber">The current attempt number (0-indexed)</param>
        /// <returns>Delay in seconds</returns>
        float GetRetryDelay(int attemptNumber);
    }
}
