Climate Trace API System Refactor - Implementation Guide
Overview
This guide details the complete refactoring of the Climate Trace API querying system from a monolithic MonoBehaviour to a modular, testable, and reusable architecture using dependency injection and the strategy pattern.

Architecture Overview
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  ClimateTraceController (MonoBehaviour - thin UI layer)     │
└──────────────────────┬──────────────────────────────────────┘
│
▼
┌─────────────────────────────────────────────────────────────┐
│                   Domain/Business Layer                      │
│        ClimateTraceClient (optional wrapper)                 │
│        GeographicFilter (utility class)                      │
└──────────────────────┬──────────────────────────────────────┘
│
▼
┌─────────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                        │
│  APIClient (generic HTTP client)                            │
│  APIRequest (fluent request builder)                        │
│  APIResponse (generic response container)                   │
└──────────────────────┬──────────────────────────────────────┘
│
▼
┌─────────────────────────────────────────────────────────────┐
│                  Configuration Layer                         │
│  APIProvider (abstract ScriptableObject)                    │
│  ClimateTraceAPI (concrete implementation)                  │
└─────────────────────────────────────────────────────────────┘

File Structure
Create the following folder structure:
Assets/Scripts/Data Management/
├── Runtime/
│   ├── API/
│   │   ├── Core/
│   │   │   ├── IAPIProvider.cs
│   │   │   ├── IAPIClient.cs
│   │   │   ├── APIProvider.cs
│   │   │   ├── APIClient.cs
│   │   │   ├── APIRequest.cs
│   │   │   └── APIResponse.cs
│   │   ├── Providers/
│   │   │   └── ClimateTraceAPI.cs
│   │   └── Clients/
│   │       └── ClimateTraceClient.cs (optional)
│   ├── Utilities/
│   │   └── GeographicFilter.cs
│   ├── Models/
│   │   ├── EmissionsSource.cs (existing)
│   │   ├── Centroid.cs (existing)
│   │   └── ClimateTraceQuery.cs (new)
│   ├── ScriptableObjects/
│   │   └── EmissionsData.cs (existing)
│   └── Controllers/
│       └── ClimateTraceController.cs
└── Tests/
└── Runtime/
├── API/
│   ├── MockAPIProvider.cs
│   ├── MockAPIClient.cs
│   └── ClimateTraceClientTests.cs
└── EmissionsDataTests.cs (existing)

Implementation Steps
Step 1: Create Interfaces
File: IAPIProvider.cs
Location: Assets/Scripts/Data Management/Runtime/API/Core/IAPIProvider.cs
Purpose: Define the contract for all API provider implementations. This allows for dependency injection and testing.
Implementation:
csharpusing UnityEngine.Networking;

namespace Data_Management.Runtime.API
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

File: IAPIClient.cs
Location: Assets/Scripts/Data Management/Runtime/API/Core/IAPIClient.cs
Purpose: Define the contract for HTTP client implementations. Enables mocking for tests.
Implementation:
csharpusing System;
using System.Collections;

namespace Data_Management.Runtime.API
{
/// <summary>
/// Interface for HTTP client implementations that handle network requests.
/// </summary>
public interface IAPIClient
{
/// <summary>
/// Execute a GET request and deserialize the response to type T.
/// </summary>
/// <typeparam name="T">The type to deserialize the response to</typeparam>
/// <param name="request">The API request configuration</param>
/// <param name="onComplete">Callback invoked when request completes (success or failure)</param>
/// <returns>Coroutine enumerator</returns>
IEnumerator Get<T>(APIRequest request, Action<APIResponse<T>> onComplete);

        /// <summary>
        /// Execute a GET request with automatic retry logic for transient failures.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response to</typeparam>
        /// <param name="request">The API request configuration</param>
        /// <param name="onComplete">Callback invoked when request completes or all retries exhausted</param>
        /// <returns>Coroutine enumerator</returns>
        IEnumerator GetWithRetry<T>(APIRequest request, Action<APIResponse<T>> onComplete);
    }
}

Step 2: Create Core API Infrastructure
File: APIResponse.cs
Location: Assets/Scripts/Data Management/Runtime/API/Core/APIResponse.cs
Purpose: Generic container for API responses that includes success state, data, errors, and metadata.
Implementation:
csharpnamespace Data_Management.Runtime.API
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

File: APIRequest.cs
Location: Assets/Scripts/Data Management/Runtime/API/Core/APIRequest.cs
Purpose: Fluent builder for constructing API requests with query parameters and headers.
Implementation:
csharpusing System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

namespace Data_Management.Runtime.API
{
/// <summary>
/// Fluent builder for constructing API requests.
/// Allows chaining method calls to build complex requests cleanly.
/// </summary>
public class APIRequest
{
private readonly string _endpoint;
private readonly Dictionary<string, string> _queryParams = new();
private readonly Dictionary<string, string> _headers = new();

        /// <summary>
        /// Create a new API request for the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint (e.g., "sources", "users/123")</param>
        public APIRequest(string endpoint)
        {
            _endpoint = endpoint;
        }

        /// <summary>
        /// Add a query parameter to the request.
        /// Empty or null values are ignored.
        /// </summary>
        /// <param name="key">Parameter name</param>
        /// <param name="value">Parameter value</param>
        /// <returns>This request instance for method chaining</returns>
        public APIRequest AddQueryParam(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _queryParams[key] = value;
            }
            return this;
        }

        /// <summary>
        /// Add an integer query parameter to the request.
        /// </summary>
        /// <param name="key">Parameter name</param>
        /// <param name="value">Parameter value</param>
        /// <returns>This request instance for method chaining</returns>
        public APIRequest AddQueryParam(string key, int value)
        {
            _queryParams[key] = value.ToString();
            return this;
        }

        /// <summary>
        /// Add a float query parameter to the request.
        /// </summary>
        /// <param name="key">Parameter name</param>
        /// <param name="value">Parameter value</param>
        /// <returns>This request instance for method chaining</returns>
        public APIRequest AddQueryParam(string key, float value)
        {
            _queryParams[key] = value.ToString();
            return this;
        }

        /// <summary>
        /// Add a custom header to the request.
        /// </summary>
        /// <param name="key">Header name</param>
        /// <param name="value">Header value</param>
        /// <returns>This request instance for method chaining</returns>
        public APIRequest AddHeader(string key, string value)
        {
            _headers[key] = value;
            return this;
        }

        /// <summary>
        /// Build the complete URL with query parameters.
        /// </summary>
        /// <param name="baseUrl">The base URL from the API provider</param>
        /// <returns>The complete URL with encoded query parameters</returns>
        public string BuildUrl(string baseUrl)
        {
            var url = $"{baseUrl.TrimEnd('/')}/{_endpoint.TrimStart('/')}";
            
            if (_queryParams.Count > 0)
            {
                // URL-encode each query parameter value
                var queryString = string.Join("&", 
                    _queryParams.Select(kvp => $"{kvp.Key}={UnityWebRequest.EscapeURL(kvp.Value)}"));
                url += $"?{queryString}";
            }
            
            return url;
        }

        /// <summary>
        /// Get a copy of the custom headers dictionary.
        /// </summary>
        /// <returns>Dictionary of header key-value pairs</returns>
        public Dictionary<string, string> GetHeaders() => new(_headers);
    }
}

File: APIProvider.cs
Location: Assets/Scripts/Data Management/Runtime/API/Core/APIProvider.cs
Purpose: Abstract base class for API provider ScriptableObjects. Implements IAPIProvider with sensible defaults.
Implementation:
csharpusing UnityEngine;
using UnityEngine.Networking;

namespace Data_Management.Runtime.API
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

File: APIClient.cs
Location: Assets/Scripts/Data Management/Runtime/API/Core/APIClient.cs
Purpose: Generic HTTP client that handles GET requests, retry logic, rate limiting, and response deserialization.
Implementation:
csharpusing System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace Data_Management.Runtime.API
{
/// <summary>
/// Generic HTTP client for making API requests.
/// Handles authentication, headers, retries, rate limiting, and deserialization.
/// </summary>
public class APIClient : IAPIClient
{
private readonly IAPIProvider _provider;
private float _lastRequestTime;

        /// <summary>
        /// Create a new API client with the specified provider configuration.
        /// </summary>
        /// <param name="provider">The API provider that defines authentication, headers, etc.</param>
        public APIClient(IAPIProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Execute a GET request and deserialize the response.
        /// </summary>
        public IEnumerator Get<T>(APIRequest request, Action<APIResponse<T>> onComplete)
        {
            // Enforce rate limiting
            yield return EnforceRateLimit();
            
            // Build the complete URL
            var url = request.BuildUrl(_provider.BaseUrl);
            
            using var webRequest = UnityWebRequest.Get(url);
            webRequest.timeout = _provider.Timeout;
            
            // Apply provider-specific configuration
            _provider.ApplyDefaultHeaders(webRequest);
            _provider.ApplyAuthentication(webRequest);
            
            // Add any custom headers from the request
            foreach (var header in request.GetHeaders())
            {
                webRequest.SetRequestHeader(header.Key, header.Value);
            }
            
            // Log the request
            Debug.Log($"[APIClient] GET {url}");
            
            // Send the request
            yield return webRequest.SendWebRequest();
            
            // Create response object
            var response = new APIResponse<T>
            {
                success = webRequest.result == UnityWebRequest.Result.Success,
                statusCode = webRequest.responseCode,
                error = _provider.ParseErrorMessage(webRequest)
            };

            if (response.success)
            {
                // Try to deserialize the response
                try
                {
                    response.rawJson = webRequest.downloadHandler.text;
                    response.data = JsonConvert.DeserializeObject<T>(response.rawJson);
                    Debug.Log($"[APIClient] Success: {response.statusCode}");
                }
                catch (Exception e)
                {
                    response.success = false;
                    response.error = $"JSON Parse Error: {e.Message}";
                    Debug.LogError($"[APIClient] {response.error}");
                }
            }
            else
            {
                Debug.LogError($"[APIClient] Error: {response.statusCode} - {response.error}");
            }
            
            // Invoke the callback
            onComplete?.Invoke(response);
        }

        /// <summary>
        /// Execute a GET request with automatic retry logic.
        /// Retries are attempted based on the provider's ShouldRetry and GetRetryDelay logic.
        /// </summary>
        public IEnumerator GetWithRetry<T>(APIRequest request, Action<APIResponse<T>> onComplete)
        {
            int attempts = 0;
            APIResponse<T> response = null;

            while (attempts < _provider.MaxRetries)
            {
                // Make the request
                yield return Get(request, r => response = r);

                // Check if successful or shouldn't retry
                if (response.success || !_provider.ShouldRetry(response.statusCode))
                {
                    break;
                }

                // Increment attempt counter
                attempts++;
                
                // If we have more retries available, wait before retrying
                if (attempts < _provider.MaxRetries)
                {
                    float delay = _provider.GetRetryDelay(attempts);
                    Debug.LogWarning($"[APIClient] Retry {attempts}/{_provider.MaxRetries} after {delay}s (status: {response.statusCode})");
                    yield return new WaitForSeconds(delay);
                }
            }

            // Invoke final callback with last response
            onComplete?.Invoke(response);
        }

        /// <summary>
        /// Enforce rate limiting by waiting if requests are too frequent.
        /// </summary>
        private IEnumerator EnforceRateLimit()
        {
            float timeSinceLastRequest = Time.time - _lastRequestTime;
            if (timeSinceLastRequest < _provider.MinTimeBetweenRequests)
            {
                float waitTime = _provider.MinTimeBetweenRequests - timeSinceLastRequest;
                Debug.Log($"[APIClient] Rate limiting: waiting {waitTime:F2}s");
                yield return new WaitForSeconds(waitTime);
            }
            _lastRequestTime = Time.time;
        }
    }
}

Step 3: Create Climate Trace Specific Implementation
File: ClimateTraceAPI.cs
Location: Assets/Scripts/Data Management/Runtime/API/Providers/ClimateTraceAPI.cs
Purpose: Concrete implementation of APIProvider for Climate Trace API with their specific configuration.
Implementation:
csharpusing UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace Data_Management.Runtime.API.Providers
{
/// <summary>
/// API provider configuration for Climate Trace API.
/// Handles Climate Trace-specific authentication, headers, and error parsing.
/// </summary>
[CreateAssetMenu(fileName = "ClimateTraceAPI", menuName = "API/Providers/Climate Trace")]
public class ClimateTraceAPI : APIProvider
{
[Header("Climate Trace Specific")]
[Tooltip("API key if Climate Trace adds authentication in the future")]
[SerializeField] private string apiKey = "";

        /// <summary>
        /// Apply authentication headers.
        /// Currently Climate Trace doesn't require auth, but this is future-proofed.
        /// </summary>
        public override void ApplyAuthentication(UnityWebRequest request)
        {
            // Climate Trace API currently doesn't require authentication
            // If they add it in the future, implement it here
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.SetRequestHeader("X-API-Key", apiKey);
            }
        }

        /// <summary>
        /// Apply Climate Trace-specific default headers.
        /// </summary>
        public override void ApplyDefaultHeaders(UnityWebRequest request)
        {
            base.ApplyDefaultHeaders(request);
            // Add any Climate Trace-specific headers here if needed
        }

        /// <summary>
        /// Parse Climate Trace error responses.
        /// Climate Trace may return JSON error messages - parse them if available.
        /// </summary>
        public override string ParseErrorMessage(UnityWebRequest request)
        {
            // Try to parse JSON error response
            if (!string.IsNullOrEmpty(request.downloadHandler?.text))
            {
                try
                {
                    var errorResponse = JsonConvert.DeserializeObject<ClimateTraceError>(
                        request.downloadHandler.text);
                    
                    if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.message))
                    {
                        return errorResponse.message;
                    }
                }
                catch
                {
                    // If JSON parsing fails, fall back to default error
                }
            }
            
            // Fall back to default Unity error message
            return base.ParseErrorMessage(request);
        }

        /// <summary>
        /// Climate Trace error response structure.
        /// Adjust this based on actual Climate Trace API error format.
        /// </summary>
        [System.Serializable]
        private class ClimateTraceError
        {
            public string message;
            public string code;
            public string details;
        }
    }
}

File: ClimateTraceQuery.cs
Location: Assets/Scripts/Data Management/Runtime/Models/ClimateTraceQuery.cs
Purpose: Data model for Climate Trace query parameters. Makes it easy to pass query configuration around.
Implementation:
csharpusing System;

namespace Data_Management.Runtime.Models
{
/// <summary>
/// Query parameters for Climate Trace emissions sources API.
/// Encapsulates all the parameters that can be sent to the /sources endpoint.
/// </summary>
[Serializable]
public class ClimateTraceQuery
{
/// <summary>
/// 3-letter country code (e.g., "USA", "CHN", "IND")
/// Leave empty to query all countries.
/// </summary>
public string countryCode;

        /// <summary>
        /// Sector filter (e.g., "power", "transportation", "fossil-fuel-operations")
        /// Leave empty to query all sectors.
        /// </summary>
        public string sector;
        
        /// <summary>
        /// Maximum number of results to return.
        /// Climate Trace API may have its own limits.
        /// </summary>
        public int limit = 100;
        
        /// <summary>
        /// Offset for pagination (number of results to skip).
        /// Use in combination with limit for paginated queries.
        /// </summary>
        public int offset = 0;

        /// <summary>
        /// Create a default query with standard parameters.
        /// </summary>
        public ClimateTraceQuery()
        {
        }

        /// <summary>
        /// Create a query with specific parameters.
        /// </summary>
        public ClimateTraceQuery(string countryCode = "", string sector = "", int limit = 100, int offset = 0)
        {
            this.countryCode = countryCode;
            this.sector = sector;
            this.limit = limit;
            this.offset = offset;
        }
    }
}

File: ClimateTraceClient.cs (Optional)
Location: Assets/Scripts/Data Management/Runtime/API/Clients/ClimateTraceClient.cs
Purpose: Optional wrapper that provides domain-specific methods for Climate Trace API. Only create this if you need to reuse Climate Trace queries across multiple controllers or add Climate Trace-specific logic.
Implementation:
csharpusing System;
using System.Collections;
using System.Collections.Generic;
using Data_Management.Runtime.Models;

namespace Data_Management.Runtime.API.Clients
{
/// <summary>
/// Client wrapper for Climate Trace API.
/// Provides domain-specific methods for querying emissions data.
/// This is optional - you can use APIClient directly if you prefer.
/// </summary>
public class ClimateTraceClient
{
private readonly IAPIClient _apiClient;

        /// <summary>
        /// Create a new Climate Trace client.
        /// </summary>
        /// <param name="apiClient">The underlying HTTP client</param>
        public ClimateTraceClient(IAPIClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        /// <summary>
        /// Get emissions sources based on query parameters.
        /// </summary>
        /// <param name="query">Query parameters (country, sector, limit, offset)</param>
        /// <param name="onComplete">Callback invoked when request completes</param>
        /// <returns>Coroutine enumerator</returns>
        public IEnumerator GetEmissionsSources(
            ClimateTraceQuery query,
            Action<APIResponse<List<EmissionsSource>>> onComplete)
        {
            // Build the request using fluent API
            var request = new APIRequest("sources")
                .AddQueryParam("limit", query.limit)
                .AddQueryParam("offset", query.offset)
                .AddQueryParam("country", query.countryCode)
                .AddQueryParam("sector", query.sector);

            // Execute with retry logic
            yield return _apiClient.GetWithRetry(request, onComplete);
        }

        /// <summary>
        /// Future method: Get a specific emissions source by ID.
        /// Uncomment and implement when needed.
        /// </summary>
        /*
        public IEnumerator GetEmissionsSourceById(
            int id,
            Action<APIResponse<EmissionsSource>> onComplete)
        {
            var request = new APIRequest($"sources/{id}");
            yield return _apiClient.GetWithRetry(request, onComplete);
        }
        */

        /// <summary>
        /// Future method: Get all sectors.
        /// Uncomment and implement when needed.
        /// </summary>
        /*
        public IEnumerator GetSectors(
            Action<APIResponse<List<string>>> onComplete)
        {
            var request = new APIRequest("sectors");
            yield return _apiClient.GetWithRetry(request, onComplete);
        }
        */
    }
}

Step 4: Create Geographic Utility
File: GeographicFilter.cs
Location: Assets/Scripts/Data Management/Runtime/Utilities/GeographicFilter.cs
Purpose: Reusable utility for filtering any collection by geographic distance. Uses Haversine formula.
Implementation:
csharpusing System;
using System.Collections.Generic;
using UnityEngine;

namespace Data_Management.Runtime.Utilities
{
/// <summary>
/// Utility class for geographic filtering and distance calculations.
/// Provides reusable methods for filtering collections by location.
/// </summary>
public static class GeographicFilter
{
/// <summary>
/// Filter a collection of items by distance from a center point.
/// Generic method that works with any type - you provide a function to extract coordinates.
/// </summary>
/// <typeparam name="T">The type of items to filter</typeparam>
/// <param name="items">The collection to filter</param>
/// <param name="centerLat">Center latitude</param>
/// <param name="centerLon">Center longitude</param>
/// <param name="radiusKm">Radius in kilometers</param>
/// <param name="getCoordinates">Function to extract coordinates from each item. Return null to exclude item.</param>
/// <returns>Filtered list containing only items within the radius</returns>
public static List<T> FilterByDistance<T>(
List<T> items,
float centerLat,
float centerLon,
float radiusKm,
Func<T, (float lat, float lon)?> getCoordinates)
{
var filtered = new List<T>();

            foreach (var item in items)
            {
                var coords = getCoordinates(item);
                
                // Skip items without valid coordinates
                if (!coords.HasValue)
                {
                    continue;
                }

                float distance = CalculateDistance(
                    centerLat, centerLon,
                    coords.Value.lat, coords.Value.lon);

                if (distance <= radiusKm)
                {
                    filtered.Add(item);
                }
            }

            return filtered;
        }

        /// <summary>
        /// Calculate the great-circle distance between two points on Earth using Haversine formula.
        /// </summary>
        /// <param name="lat1">Latitude of first point (degrees)</param>
        /// <param name="lon1">Longitude of first point (degrees)</param>
        /// <param name="lat2">Latitude of second point (degrees)</param>
        /// <param name="lon2">Longitude of second point (degrees)</param>
        /// <returns>Distance in kilometers</returns>
        public static float CalculateDistance(float lat1, float lon1, float lat2, float lon2)
        {
            const float EARTH_RADIUS_KM = 6371f;

            // Convert degrees to radians
            float dLat = Mathf.Deg2Rad * (lat2 - lat1);
            float dLon = Mathf.Deg2Rad * (lon2 - lon1);
            float lat1Rad = Mathf.Deg2Rad * lat1;
            float lat2Rad = Mathf.Deg2Rad * lat2;

            // Haversine formula
            float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                      Mathf.Cos(lat1Rad) * Mathf.Cos(lat2Rad) *
                      Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);

            float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));

            return EARTH_RADIUS_KM * c;
        }

        /// <summary>
        /// Check if a point is within a radius of a center point.
        /// Convenience method for simple distance checks.
        /// </summary>
        /// <returns>True if point is within radius</returns>
        public static bool IsWithinRadius(
            float centerLat, float centerLon,
            float pointLat, float pointLon,
            float radiusKm)
        {
            float distance = CalculateDistance(centerLat, centerLon, pointLat, pointLon);
            return distance <= radiusKm;
        }
    }
}

Step 5: Create the New Controller
File: ClimateTraceController.cs
Location: Assets/Scripts/Data Management/Runtime/Controllers/ClimateTraceController.cs
Purpose: Thin MonoBehaviour controller that wires up dependencies and handles UI interactions. This replaces the old QueryClimateTrace.cs.
Implementation:
csharpusing System.Collections.Generic;
using Data_Management.Runtime.API;
using Data_Management.Runtime.API.Clients;
using Data_Management.Runtime.Models;
using Data_Management.Runtime.Scriptable_Objects;
using Data_Management.Runtime.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Data_Management.Runtime.Controllers
{
/// <summary>
/// Controller for querying Climate Trace emissions data.
/// This is the thin presentation layer that wires up the API infrastructure
/// and provides UI controls via Odin Inspector.
/// </summary>
public class ClimateTraceController : MonoBehaviour
{
[Title("API Configuration")]
[Tooltip("The API provider configuration (create via Assets/Create/API/Providers/Climate Trace)")]
[Required, AssetsList]
[SerializeField] private APIProvider apiProvider;

        [Tooltip("Where to store the fetched emissions data")]
        [Required, AssetsOnly]
        [SerializeField] private EmissionsData emissionsData;

        [Title("Query Parameters")]
        [InlineProperty, HideLabel]
        [SerializeField] private ClimateTraceQuery query = new();

        [Title("Location Filtering")]
        [Tooltip("Filter results by distance from a center point")]
        [SerializeField] private bool filterByLocation = true;

        [ShowIf("filterByLocation")]
        [SerializeField] private float centerLatitude = 37.7749f; // San Francisco

        [ShowIf("filterByLocation")]
        [SerializeField] private float centerLongitude = -122.4194f;

        [ShowIf("filterByLocation"), Range(1f, 500f)]
        [SerializeField] private float radiusKm = 50f;

        [Title("Status")]
        [ReadOnly, ShowInInspector]
        private bool _isLoading;

        [ReadOnly, ShowInInspector]
        private string _lastError;

        // Dependencies (injected in Awake)
        private IAPIClient _apiClient;
        private ClimateTraceClient _climateTraceClient; // Optional wrapper

        /// <summary>
        /// Initialize dependencies.
        /// This is where dependency injection happens manually.
        /// </summary>
        private void Awake()
        {
            if (apiProvider != null)
            {
                // Create the generic API client with the provider
                _apiClient = new APIClient(apiProvider);
                
                // Optionally create the Climate Trace wrapper client
                // Comment this out if you prefer to use _apiClient directly
                _climateTraceClient = new ClimateTraceClient(_apiClient);
            }
            else
            {
                Debug.LogError("[ClimateTraceController] API Provider is not assigned!");
            }
        }

        [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
        [DisableIf("_isLoading")]
        public void FetchEmissionsData()
        {
            if (!ValidateConfiguration())
            {
                return;
            }

            StartCoroutine(FetchDataCoroutine());
        }

        [Button(ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.5f)]
        public void ClearData()
        {
            if (emissionsData != null)
            {
                emissionsData.sources.Clear();
                emissionsData.lastUpdated = "";
                _lastError = "";
                Debug.Log("[ClimateTraceController] Cleared emissions data");
            }
        }

        /// <summary>
        /// Validate that all required configuration is set.
        /// </summary>
        private bool ValidateConfiguration()
        {
            if (apiProvider == null)
            {
                _lastError = "API Provider is not assigned!";
                Debug.LogError($"[ClimateTraceController] {_lastError}");
                return false;
            }

            if (emissionsData == null)
            {
                _lastError = "Emissions Data is not assigned!";
                Debug.LogError($"[ClimateTraceController] {_lastError}");
                return false;
            }

            if (_apiClient == null)
            {
                _lastError = "API Client failed to initialize!";
                Debug.LogError($"[ClimateTraceController] {_lastError}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Main coroutine for fetching data.
        /// </summary>
        private System.Collections.IEnumerator FetchDataCoroutine()
        {
            _isLoading = true;
            _lastError = "";

            string queryDescription = string.IsNullOrEmpty(query.countryCode) 
                ? "all countries" 
                : query.countryCode;
            
            if (!string.IsNullOrEmpty(query.sector))
            {
                queryDescription += $" / {query.sector}";
            }

            Debug.Log($"[ClimateTraceController] 🔍 Fetching emissions data for {queryDescription}...");

            // Option 1: Use the ClimateTraceClient wrapper (recommended if you have it)
            if (_climateTraceClient != null)
            {
                yield return _climateTraceClient.GetEmissionsSources(query, OnDataReceived);
            }
            // Option 2: Use APIClient directly (simpler, fewer layers)
            else
            {
                var request = new APIRequest("sources")
                    .AddQueryParam("limit", query.limit)
                    .AddQueryParam("offset", query.offset)
                    .AddQueryParam("country", query.countryCode)
                    .AddQueryParam("sector", query.sector);

                yield return _apiClient.GetWithRetry<List<EmissionsSource>>(request, OnDataReceived);
            }

            _isLoading = false;
        }

        /// <summary>
        /// Handle the API response.
        /// </summary>
        private void OnDataReceived(APIResponse<List<EmissionsSource>> response)
        {
            // Handle errors
            if (!response.success)
            {
                _lastError = $"API Error [{response.statusCode}]: {response.error}";
                Debug.LogError($"[ClimateTraceController] ❌ {_lastError}");
                return;
            }

            // Handle empty response
            if (response.data == null || response.data.Count == 0)
            {
                _lastError = "No sources returned from API";
                Debug.LogWarning($"[ClimateTraceController] ⚠️ {_lastError}");
                return;
            }

            Debug.Log($"[ClimateTraceController] 📊 Received {response.data.Count} sources from API");

            // Apply location filtering if enabled
            var finalSources = filterByLocation
                ? FilterByLocation(response.data)
                : response.data;

            if (filterByLocation)
            {
                Debug.Log($"[ClimateTraceController] 📍 Filtered to {finalSources.Count} sources within {radiusKm}km of ({centerLatitude}, {centerLongitude})");
            }

            // Store results in ScriptableObject
            SaveToScriptableObject(finalSources);

            // Log summary
            LogSummary(finalSources);
        }

        /// <summary>
        /// Filter sources by geographic location using the GeographicFilter utility.
        /// </summary>
        private List<EmissionsSource> FilterByLocation(List<EmissionsSource> sources)
        {
            return GeographicFilter.FilterByDistance(
                sources,
                centerLatitude,
                centerLongitude,
                radiusKm,
                source => source.centroid != null
                    ? (source.centroid.latitude, source.centroid.longitude)
                    : ((float, float)?)null);
        }

        /// <summary>
        /// Save the filtered sources to the EmissionsData ScriptableObject.
        /// </summary>
        private void SaveToScriptableObject(List<EmissionsSource> sources)
        {
            emissionsData.sources = sources;
            emissionsData.queryLatitude = centerLatitude;
            emissionsData.queryLongitude = centerLongitude;
            emissionsData.queryRadiusKm = radiusKm;
            emissionsData.lastUpdated = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            Debug.Log($"[ClimateTraceController] ✅ Saved {sources.Count} sources to {emissionsData.name}");

            // Mark the ScriptableObject as dirty so Unity saves the changes
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(emissionsData);
            #endif
        }

        /// <summary>
        /// Log a summary of the fetched data.
        /// </summary>
        private void LogSummary(List<EmissionsSource> sources)
        {
            // Log sample sources
            int sampleCount = Mathf.Min(3, sources.Count);
            if (sampleCount > 0)
            {
                Debug.Log("[ClimateTraceController] 📋 Sample sources:");
                for (int i = 0; i < sampleCount; i++)
                {
                    var s = sources[i];
                    float lat = s.centroid?.latitude ?? 0;
                    float lon = s.centroid?.longitude ?? 0;
                    Debug.Log($"  • {s.name}");
                    Debug.Log($"    └─ Sector: {s.sector} / {s.subsector}");
                    Debug.Log($"    └─ Emissions: {s.emissionsQuantity:N0} t {s.gas} ({s.year})");
                    Debug.Log($"    └─ Location: ({lat:F4}, {lon:F4})");
                }
            }

            // Log total emissions
            Debug.Log($"[ClimateTraceController] 📈 Total emissions: {emissionsData.TotalEmissions:N0} t CO2e");

            // Log sector breakdown
            var sectorCounts = emissionsData.SourcesBySector;
            if (sectorCounts.Count > 0)
            {
                Debug.Log("[ClimateTraceController] 📊 Sources by sector:");
                foreach (var kvp in sectorCounts)
                {
                    Debug.Log($"  • {kvp.Key}: {kvp.Value} sources");
                }
            }
        }
    }
}

Step 6: Create Test Mocks
File: MockAPIProvider.cs
Location: Assets/Scripts/Data Management/Tests/Runtime/API/MockAPIProvider.cs
Purpose: Mock implementation of IAPIProvider for testing.
Implementation:
csharpusing Data_Management.Runtime.API;
using UnityEngine.Networking;

namespace Data_Management.Tests.API
{
/// <summary>
/// Mock API provider for testing.
/// Allows tests to control all provider behavior without creating ScriptableObjects.
/// </summary>
public class MockAPIProvider : IAPIProvider
{
public string BaseUrl { get; set; } = "https://test.api.com";
public int Timeout { get; set; } = 10;
public int MaxRetries { get; set; } = 2;
public float MinTimeBetweenRequests { get; set; } = 0f; // No rate limiting in tests

        public int AuthenticationCallCount { get; private set; }
        public int DefaultHeadersCallCount { get; private set; }

        public void ApplyAuthentication(UnityWebRequest request)
        {
            AuthenticationCallCount++;
            // Mock: don't actually modify the request in tests
        }

        public void ApplyDefaultHeaders(UnityWebRequest request)
        {
            DefaultHeadersCallCount++;
            // Mock: don't actually modify the request in tests
        }

        public string BuildEndpointUrl(string endpoint)
        {
            return $"{BaseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        }

        public string ParseErrorMessage(UnityWebRequest request)
        {
            return request?.error ?? "Mock error";
        }

        public bool ShouldRetry(long statusCode)
        {
            return statusCode >= 500 || statusCode == 429;
        }

        public float GetRetryDelay(int attemptNumber)
        {
            return 0.1f; // Fast retries for testing
        }
    }
}

File: MockAPIClient.cs
Location: Assets/Scripts/Data Management/Tests/Runtime/API/MockAPIClient.cs
Purpose: Mock implementation of IAPIClient for testing without network calls.
Implementation:
csharpusing System;
using System.Collections;
using Data_Management.Runtime.API;

namespace Data_Management.Tests.API
{
/// <summary>
/// Mock API client for testing.
/// Returns pre-configured responses without making actual network calls.
/// </summary>
public class MockAPIClient : IAPIClient
{
/// <summary>
/// The response that will be returned by Get/GetWithRetry.
/// Set this in your tests to control what the client returns.
/// </summary>
public object MockResponse { get; set; }

        /// <summary>
        /// Number of times Get was called.
        /// </summary>
        public int CallCount { get; private set; }

        /// <summary>
        /// The last request that was made.
        /// Useful for verifying the request was built correctly.
        /// </summary>
        public APIRequest LastRequest { get; private set; }

        /// <summary>
        /// Reset all tracking counters.
        /// Call this in test SetUp if needed.
        /// </summary>
        public void Reset()
        {
            CallCount = 0;
            LastRequest = null;
            MockResponse = null;
        }

        public IEnumerator Get<T>(APIRequest request, Action<APIResponse<T>> onComplete)
        {
            CallCount++;
            LastRequest = request;

            // Return the pre-configured mock response
            if (MockResponse is APIResponse<T> typedResponse)
            {
                onComplete?.Invoke(typedResponse);
            }
            else
            {
                // If MockResponse is wrong type, return a default error
                onComplete?.Invoke(new APIResponse<T>
                {
                    success = false,
                    error = "MockResponse type mismatch"
                });
            }

            yield break;
        }

        public IEnumerator GetWithRetry<T>(APIRequest request, Action<APIResponse<T>> onComplete)
        {
            // For mock, retry logic is the same as regular Get
            return Get(request, onComplete);
        }
    }
}

File: ClimateTraceClientTests.cs
Location: Assets/Scripts/Data Management/Tests/Runtime/API/ClimateTraceClientTests.cs
Purpose: Example tests for the ClimateTraceClient showing how to use the mocks.
Implementation:
csharpusing System.Collections.Generic;
using Data_Management.Runtime;
using Data_Management.Runtime.API;
using Data_Management.Runtime.API.Clients;
using Data_Management.Runtime.Models;
using NUnit.Framework;

namespace Data_Management.Tests.API
{
/// <summary>
/// Tests for ClimateTraceClient.
/// Demonstrates how to test the client layer using mocks.
/// </summary>
public class ClimateTraceClientTests
{
private MockAPIClient _mockClient;
private ClimateTraceClient _client;

        [SetUp]
        public void SetUp()
        {
            _mockClient = new MockAPIClient();
            _client = new ClimateTraceClient(_mockClient);
        }

        [TearDown]
        public void TearDown()
        {
            _mockClient.Reset();
        }

        [Test]
        public void GetEmissionsSources_CallsAPIClient()
        {
            // Arrange
            var query = new ClimateTraceQuery("USA", "energy", 50, 0);
            
            _mockClient.MockResponse = new APIResponse<List<EmissionsSource>>
            {
                success = true,
                data = new List<EmissionsSource>()
            };

            APIResponse<List<EmissionsSource>> result = null;

            // Act
            var coroutine = _client.GetEmissionsSources(query, r => result = r);
            while (coroutine.MoveNext()) { } // Run coroutine synchronously

            // Assert
            Assert.AreEqual(1, _mockClient.CallCount, "Should call API client once");
            Assert.IsNotNull(result, "Should receive a response");
        }

        [Test]
        public void GetEmissionsSources_SuccessResponse_ReturnsData()
        {
            // Arrange
            var expectedSources = new List<EmissionsSource>
            {
                new EmissionsSource { id = 1, name = "Test Source", sector = "energy" },
                new EmissionsSource { id = 2, name = "Another Source", sector = "transport" }
            };

            _mockClient.MockResponse = new APIResponse<List<EmissionsSource>>
            {
                success = true,
                data = expectedSources,
                statusCode = 200
            };

            APIResponse<List<EmissionsSource>> result = null;

            // Act
            var coroutine = _client.GetEmissionsSources(new ClimateTraceQuery(), r => result = r);
            while (coroutine.MoveNext()) { }

            // Assert
            Assert.IsTrue(result.success, "Response should be successful");
            Assert.AreEqual(2, result.data.Count, "Should return 2 sources");
            Assert.AreEqual("Test Source", result.data[0].name);
            Assert.AreEqual("energy", result.data[0].sector);
        }

        [Test]
        public void GetEmissionsSources_ErrorResponse_ReturnsError()
        {
            // Arrange
            _mockClient.MockResponse = new APIResponse<List<EmissionsSource>>
            {
                success = false,
                error = "Network timeout",
                statusCode = 408
            };

            APIResponse<List<EmissionsSource>> result = null;

            // Act
            var coroutine = _client.GetEmissionsSources(new ClimateTraceQuery(), r => result = r);
            while (coroutine.MoveNext()) { }

            // Assert
            Assert.IsFalse(result.success, "Response should indicate failure");
            Assert.AreEqual("Network timeout", result.error);
            Assert.AreEqual(408, result.statusCode);
        }

        [Test]
        public void GetEmissionsSources_EmptyResponse_ReturnsEmptyList()
        {
            // Arrange
            _mockClient.MockResponse = new APIResponse<List<EmissionsSource>>
            {
                success = true,
                data = new List<EmissionsSource>(),
                statusCode = 200
            };

            APIResponse<List<EmissionsSource>> result = null;

            // Act
            var coroutine = _client.GetEmissionsSources(new ClimateTraceQuery(), r => result = r);
            while (coroutine.MoveNext()) { }

            // Assert
            Assert.IsTrue(result.success);
            Assert.IsNotNull(result.data);
            Assert.AreEqual(0, result.data.Count, "Should return empty list");
        }
    }
}

Step 7: Update Existing Models (if needed)
Ensure your existing EmissionsSource.cs and Centroid.cs are in the correct namespace:
Update EmissionsSource.cs
Change the namespace from:
csharpnamespace Data_Management.Runtime
To:
csharpnamespace Data_Management.Runtime
(Keep it the same - this is fine)
Update Centroid.cs
Add the [Serializable] attribute if it's missing:
csharpusing System;

namespace Data_Management.Runtime
{
[Serializable]
public class Centroid
{
public float longitude;
public float latitude;
public int srid;
}
}

Unity Setup Instructions
1. Create the API Provider Asset

In Unity, right-click in Project window
Navigate to: Create > API > Providers > Climate Trace
Name it: ClimateTraceAPI
Set the Base URL to: https://api.climatetrace.org/v7
Set Timeout to: 30
Set Max Retries to: 3
Set Min Time Between Requests to: 0.1

2. Update the Scene

Find the GameObject with QueryClimateTrace component
Remove the old QueryClimateTrace component
Add the new ClimateTraceController component
Assign the ClimateTraceAPI asset to the Api Provider field
Assign your existing EmissionsData ScriptableObject to the Emissions Data field

3. Test the Implementation

Enter Play Mode
In the Inspector, configure query parameters (country code, sector, etc.)
Enable/disable location filtering as needed
Click the "Fetch Emissions Data" button
Check the Console for detailed logs
Verify data appears in the EmissionsData ScriptableObject


Testing Instructions
Running Unit Tests

Open Test Runner: Window > General > Test Runner
Switch to EditMode tab
Click "Run All"
Verify all tests pass

Writing New Tests
Follow this pattern:
csharp[Test]
public void YourTest_Scenario_ExpectedBehavior()
{
// Arrange - Set up test data and mocks
var mockClient = new MockAPIClient();
mockClient.MockResponse = new APIResponse<YourType>
{
success = true,
data = yourTestData
};

    // Act - Execute the code being tested
    var result = YourMethodUnderTest();

    // Assert - Verify the behavior
    Assert.IsTrue(result.success);
    Assert.AreEqual(expectedValue, result.data);
}

Migration Checklist
Before Starting

Backup your project
Commit current changes to version control
Note any custom modifications to QueryClimateTrace.cs

Implementation Phase

Create all interface files (IAPIProvider, IAPIClient)
Create all core infrastructure files (APIProvider, APIClient, APIRequest, APIResponse)
Create ClimateTraceAPI provider
Create ClimateTraceQuery model
Create ClimateTraceClient (optional)
Create GeographicFilter utility
Create ClimateTraceController
Create test mocks (MockAPIProvider, MockAPIClient)
Create example tests

Unity Setup Phase

Create ClimateTraceAPI ScriptableObject asset
Configure API provider settings
Replace old component with new controller
Assign references in Inspector
Test in Play Mode

Validation Phase

Verify data fetching works
Verify location filtering works
Check Console for errors
Run unit tests
Verify EmissionsData ScriptableObject updates correctly

Cleanup Phase

Delete old QueryClimateTrace.cs
Remove unused using statements
Update documentation
Commit to version control


Troubleshooting
"Type or namespace not found" errors
Solution: Make sure all files are in the correct folders and namespaces match the folder structure.
API requests fail with 0 status code
Solution: Check that the Base URL in your ClimateTraceAPI asset is correct and doesn't have a trailing slash.
Tests can't find types
Solution: Make sure your test assembly references the runtime assembly in the .asmdef file.
Network requests timeout
Solution: Increase the Timeout value in your ClimateTraceAPI asset (try 60 seconds).
Location filtering returns 0 results
Solution: Verify the center coordinates are correct and the radius is large enough. Check the Console for distance calculations.

Extension Points
Adding a New API

Create a new provider class extending APIProvider
Override ApplyAuthentication if needed
Override ParseErrorMessage for custom error handling
Create a ScriptableObject asset with the new provider
Use with APIClient or create a specific client wrapper

Example:
csharp[CreateAssetMenu(fileName = "WeatherAPI", menuName = "API/Providers/Weather")]
public class WeatherAPI : APIProvider
{
[SerializeField] private string apiKey;

    public override void ApplyAuthentication(UnityWebRequest request)
    {
        request.SetRequestHeader("X-API-Key", apiKey);
    }
}
Adding Caching
Extend APIClient to cache responses:
csharppublic class CachedAPIClient : APIClient
{
private Dictionary<string, object> _cache = new();

    public override IEnumerator Get<T>(APIRequest request, Action<APIResponse<T>> onComplete)
    {
        string cacheKey = request.BuildUrl(_provider.BaseUrl);
        
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            onComplete?.Invoke((APIResponse<T>)cached);
            yield break;
        }
        
        yield return base.Get<T>(request, response =>
        {
            if (response.success)
            {
                _cache[cacheKey] = response;
            }
            onComplete?.Invoke(response);
        });
    }
}
Adding POST Requests
Extend APIClient and APIRequest:
csharppublic IEnumerator Post<TRequest, TResponse>(
APIRequest request,
TRequest body,
Action<APIResponse<TResponse>> onComplete)
{
var jsonBody = JsonConvert.SerializeObject(body);
var url = request.BuildUrl(_provider.BaseUrl);

    using var webRequest = UnityWebRequest.Post(url, jsonBody);
    // ... rest of implementation
}

Performance Considerations
Rate Limiting
The MinTimeBetweenRequests setting in APIProvider automatically rate-limits requests. Adjust based on API quotas.
Memory Management

Large responses are deserialized once and stored in EmissionsData
Old data is cleared when new data is fetched
Coroutines are properly disposed with using statements

Network Optimization

Use GetWithRetry for transient failures
Implement caching for frequently accessed data
Consider pagination for large datasets


Best Practices Summary

Always use interfaces for dependencies - Enables testing
Keep MonoBehaviours thin - Business logic in plain C# classes
Use ScriptableObjects for configuration - Not runtime logic
Write tests first when possible - TDD catches bugs early
Log appropriately - Use Debug.Log for info, LogError for errors
Handle all error cases - Network requests can fail in many ways
Document public APIs - XML comments for all public methods
Follow SOLID principles - Single responsibility, dependency injection
Use fluent APIs - APIRequest builder pattern is clean and readable
Avoid premature optimization - Start simple, optimize when needed


Additional Resources

Unity Web Requests Documentation
Newtonsoft JSON Documentation
NUnit Testing Framework
SOLID Principles
Climate Trace API Documentation


Conclusion
This refactoring transforms a monolithic MonoBehaviour into a modular, testable system following Unity and C# best practices. The new architecture:

✅ Separates concerns (UI, business logic, infrastructure)
✅ Enables comprehensive unit testing
✅ Supports multiple APIs with minimal code changes
✅ Provides clear error handling and logging
✅ Uses dependency injection for flexibility
✅ Follows SOLID principles
✅ Is production-ready and maintainable