using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

namespace Data_Management.Runtime.API.Core
{
    /// <summary>
    /// Fluent builder for constructing API requests.
    /// Allows chaining method calls to build complex requests cleanly.
    /// </summary>
    public class APIRequest
    {
        private readonly string endpoint;
        private readonly Dictionary<string, string> queryParams = new();
        private readonly Dictionary<string, string> headers = new();

        /// <summary>
        /// Create a new API request for the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The API endpoint (e.g., "sources", "users/123")</param>
        public APIRequest(string endpoint)
        {
            this.endpoint = endpoint;
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
                queryParams[key] = value;
            }
            return this;
        }

        /// <summary>
        /// Add a query parameter from a list of strings.
        /// Concatenates values with commas. Empty or null lists are ignored.
        /// </summary>
        /// <param name="key">Parameter name</param>
        /// <param name="values">List of values to concatenate</param>
        /// <returns>This request instance for method chaining</returns>
        public APIRequest AddQueryParam(string key, List<string> values)
        {
            if (values is not { Count: > 0 }) return this;
            string concatenated = string.Join(",", values.Where(v => !string.IsNullOrEmpty(v)));
            if (!string.IsNullOrEmpty(concatenated))
            {
                queryParams[key] = concatenated;
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
            queryParams[key] = value.ToString();
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
            queryParams[key] = value.ToString();
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
            headers[key] = value;
            return this;
        }

        /// <summary>
        /// Build the complete URL with query parameters.
        /// </summary>
        /// <param name="baseUrl">The base URL from the API provider</param>
        /// <returns>The complete URL with encoded query parameters</returns>
        public string BuildUrl(string baseUrl)
        {
            var url = $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

            if (queryParams.Count > 0)
            {
                // URL-encode each query parameter value
                var queryString = string.Join("&",
                    queryParams.Select(kvp => $"{kvp.Key}={UnityWebRequest.EscapeURL(kvp.Value)}"));
                url += $"?{queryString}";
            }

            return url;
        }

        /// <summary>
        /// Get a copy of the custom headers dictionary.
        /// </summary>
        /// <returns>Dictionary of header key-value pairs</returns>
        public Dictionary<string, string> GetHeaders() => new(headers);
    }
}
