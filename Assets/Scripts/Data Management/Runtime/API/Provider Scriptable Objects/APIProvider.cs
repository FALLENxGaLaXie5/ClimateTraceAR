using Data_Management.Runtime.API.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Data_Management.Runtime.API.Provider_Scriptable_Objects
{
    /// <summary>
    /// Abstract base class for API provider configurations.
    /// Subclass this to create specific API providers (e.g., ClimateTraceAPI, WeatherAPI).
    /// Implements IAPIProvider with sensible defaults that can be overridden.
    /// </summary>
    public abstract class APIProvider : ScriptableObject, IAPIProvider
    {
        [Header("Base Configuration")]
        [Tooltip("Base URL for the API (e.g., https://api.example.com/v1)")]
        [SerializeField] private string baseUrl;

        [Tooltip("Request timeout in seconds")]
        [SerializeField] private int timeout = 30;

        [Tooltip("Maximum number of retry attempts for failed requests")]
        [SerializeField] private int maxRetries = 3;

        [Header("Rate Limiting")]
        [Tooltip("Minimum time between consecutive requests (seconds)")]
        [SerializeField] private float minTimeBetweenRequests = 0.1f;

        // Expose fields through interface properties
        public string BaseUrl => baseUrl;
        public int Timeout => timeout;
        public int MaxRetries => maxRetries;
        public float MinTimeBetweenRequests => minTimeBetweenRequests;

        /// <summary>
        /// Apply API-specific authentication.
        /// Default implementation does nothing (no auth).
        /// Override in subclasses to add API keys, OAuth tokens, etc.
        /// </summary>
        public abstract void ApplyAuthentication(UnityWebRequest request);

        /// <summary>
        /// Apply default headers required by this API.
        /// Default implementation adds standard JSON headers.
        /// Override to add custom headers or modify behavior.
        /// </summary>
        public virtual void ApplyDefaultHeaders(UnityWebRequest request)
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
        }

        /// <summary>
        /// Construct the full endpoint URL.
        /// Default implementation simply appends endpoint to base URL.
        /// Override if your API requires custom URL construction.
        /// </summary>
        public virtual string BuildEndpointUrl(string endpoint)
        {
            return $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        }

        /// <summary>
        /// Parse error messages from failed requests.
        /// Default implementation returns the Unity error string.
        /// Override to parse JSON error responses or custom error formats.
        /// </summary>
        public virtual string ParseErrorMessage(UnityWebRequest request)
        {
            return request.error;
        }

        /// <summary>
        /// Determine if a request should be retried based on status code.
        /// Default: retry on server errors (5xx) and rate limiting (429).
        /// Override to customize retry logic for specific APIs.
        /// </summary>
        public virtual bool ShouldRetry(long statusCode)
        {
            // Retry on server errors and rate limiting
            return statusCode >= 500 || statusCode == 429;
        }

        /// <summary>
        /// Calculate retry delay with exponential backoff.
        /// Default: 2^attemptNumber seconds (1s, 2s, 4s, 8s, etc.)
        /// Override to customize backoff strategy.
        /// </summary>
        public virtual float GetRetryDelay(int attemptNumber)
        {
            return Mathf.Pow(2, attemptNumber);
        }
    }
}
