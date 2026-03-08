using System;
using System.Collections;

namespace Data_Management.Runtime.API.Core
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
