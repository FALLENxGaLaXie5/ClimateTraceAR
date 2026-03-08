using System;
using System.Collections;
using Data_Management.Runtime.API.Core;

namespace Data_Management.Tests.Runtime.API
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
