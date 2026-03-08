using Data_Management.Runtime.API.Core;
using UnityEngine.Networking;

namespace Data_Management.Tests.Runtime.API
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
