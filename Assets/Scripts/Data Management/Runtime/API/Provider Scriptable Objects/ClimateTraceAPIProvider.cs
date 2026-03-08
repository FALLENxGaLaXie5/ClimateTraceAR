using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using System;

namespace Data_Management.Runtime.API.Provider_Scriptable_Objects
{
    /// <summary>
    /// API provider configuration for Climate Trace API.
    /// Handles Climate Trace-specific authentication, headers, and error parsing.
    /// </summary>
    [CreateAssetMenu(fileName = "ClimateTraceAPI", menuName = "API/Providers/Climate Trace")]
    public class ClimateTraceAPIProvider : APIProvider
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
        [Serializable]
        private class ClimateTraceError
        {
            public string message;
            public string code;
            public string details;
        }
    }
}
