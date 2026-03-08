using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Data_Management.Runtime.API.Core
{
    /// <summary>
    /// Generic HTTP client for making API requests.
    /// Handles authentication, headers, retries, rate limiting, and deserialization.
    /// </summary>
    public class APIClient : IAPIClient
    {
        private readonly IAPIProvider provider;
        private float lastRequestTime;

        /// <summary>
        /// Create a new API client with the specified provider configuration.
        /// </summary>
        /// <param name="provider">The API provider that defines authentication, headers, etc.</param>
        public APIClient(IAPIProvider provider)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Execute a GET request and deserialize the response.
        /// </summary>
        public IEnumerator Get<T>(APIRequest request, Action<APIResponse<T>> onComplete)
        {
            // Enforce rate limiting
            yield return EnforceRateLimit();

            // Build the complete URL
            string url = request.BuildUrl(provider.BaseUrl);

            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            webRequest.timeout = provider.Timeout;

            // Apply provider-specific configuration
            provider.ApplyDefaultHeaders(webRequest);
            provider.ApplyAuthentication(webRequest);

            // Add any custom headers from the request
            foreach (KeyValuePair<string, string> header in request.GetHeaders())
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
                error = provider.ParseErrorMessage(webRequest)
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

            onComplete?.Invoke(response);
        }

        /// <summary>
        /// Execute a GET request with automatic retry logic.
        /// Retries are attempted based on the provider's ShouldRetry and GetRetryDelay logic.
        /// </summary>
        public IEnumerator GetWithRetry<T>(APIRequest request, Action<APIResponse<T>> onComplete)
        {
            var attempts = 0;
            APIResponse<T> response = null;

            while (attempts < provider.MaxRetries)
            {
                // Make the request - EXPLICITLY specify type T
                yield return Get<T>(request, r => response = r);

                // Check if successful or shouldn't retry
                if (response.success || !provider.ShouldRetry(response.statusCode))
                {
                    break;
                }

                // Increment attempt counter
                attempts++;

                // If we have more retries available, wait before retrying
                if (attempts < provider.MaxRetries)
                {
                    float delay = provider.GetRetryDelay(attempts);
                    Debug.LogWarning($"[APIClient] Retry {attempts}/{provider.MaxRetries} after {delay}s (status: {response.statusCode})");
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
            float timeSinceLastRequest = Time.time - lastRequestTime;
            if (timeSinceLastRequest < provider.MinTimeBetweenRequests)
            {
                float waitTime = provider.MinTimeBetweenRequests - timeSinceLastRequest;
                Debug.Log($"[APIClient] Rate limiting: waiting {waitTime:F2}s");
                yield return new WaitForSeconds(waitTime);
            }
            lastRequestTime = Time.time;
        }
    }
}
