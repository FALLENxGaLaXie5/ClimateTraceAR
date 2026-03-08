using System;
using System.Collections;
using System.Collections.Generic;
using Data_Management.Runtime.API.Core;
using Data_Management.Runtime.Models;

namespace Data_Management.Runtime.API.Clients
{
    public class ClimateTraceClient
    {
        private readonly IAPIClient apiClient;

        public ClimateTraceClient(IAPIClient apiClient)
        {
            this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public IEnumerator GetEmissionsSources(
            ClimateTraceQuery query,
            Action<APIResponse<List<EmissionsSource>>> onComplete) // Change back to List
        {
            // Ensure parameters are correct for v7
            APIRequest request = new APIRequest("sources")
                .AddQueryParam("limit", query.limit.ToString())
                .AddQueryParam("offset", query.offset.ToString())
                .AddQueryParam("gadmId", query.countryCode);

            _ = query.allSectors ? request.AddQueryParam("sectors", "all_no_forest") : request.AddQueryParam("sectors", query.sectors);

            yield return apiClient.GetWithRetry(request, onComplete);
        }
    }
}