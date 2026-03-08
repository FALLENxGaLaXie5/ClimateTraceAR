using System.Collections.Generic;
using System.Collections;
using Data_Management.Runtime.API.Clients;
using Data_Management.Runtime.API.Core;
using Data_Management.Runtime.API.Provider_Scriptable_Objects;
using Data_Management.Runtime.Models;
using Data_Management.Runtime.Scriptable_Objects;
using Data_Management.Runtime.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

namespace Data_Management.Runtime.Controllers
{
    [CreateAssetMenu(fileName = "ClimateTraceController", menuName = "Data Management/Climate Trace Controller")]
    public class ClimateTraceController : ScriptableObject
    {
        [Title("API Configuration")]
        [Tooltip("The API provider configuration (create via Assets/Create/API/Providers/Climate Trace)")]
        [Required, AssetsOnly, InlineEditor]
        [SerializeField] private APIProvider apiProvider;

        [Tooltip("Where to store the fetched emissions data")]
        [Required, AssetsOnly, InlineEditor]
        [SerializeField] private EmissionsData emissionsData;

        [Title("Query Parameters")]
        [InlineProperty, HideLabel]
        [SerializeField] private ClimateTraceQuery query = new();

        [Title("Location Filtering")]
        [Tooltip("Filter results by distance from a center point")]
        [SerializeField] private bool filterByLocation = true;

        [ShowIf("filterByLocation")]
        [SerializeField] private float centerLatitude = 37.7749f;

        [ShowIf("filterByLocation")]
        [SerializeField] private float centerLongitude = -122.4194f;

        [ShowIf("filterByLocation"), Range(1f, 500f)]
        [SerializeField] private float radiusKm = 50f;

        [Title("Status")]
        [ReadOnly, ShowInInspector]
        private bool isLoading;

        [ReadOnly, ShowInInspector]
        private string lastError;

        private ClimateTraceClient climateTraceClient;

        [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
        [DisableIf("isLoading")]
        public void FetchEmissionsData()
        {
#if UNITY_EDITOR
            climateTraceClient ??= new ClimateTraceClient(new APIClient(apiProvider));
            EditorCoroutineUtility.StartCoroutineOwnerless(FetchDataCoroutine());
#else
            Debug.LogWarning("[ClimateTraceController] Fetch only works in Editor mode");
#endif
        }

        [Button(ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.5f)]
        public void ClearData()
        {
            emissionsData.sources.Clear();
            emissionsData.lastUpdated = "";
            lastError = "";
            Debug.Log("[ClimateTraceController] Cleared emissions data");
        }

        private IEnumerator FetchDataCoroutine()
        {
            isLoading = true;
            lastError = "";

            yield return climateTraceClient.GetEmissionsSources(query, OnDataReceived);

            isLoading = false;
        }

        private void OnDataReceived(APIResponse<List<EmissionsSource>> response)
        {
            if (!response.success)
            {
                lastError = $"API Error [{response.statusCode}]: {response.error}";
                Debug.LogError($"[ClimateTraceController] ❌ {lastError}");
                return;
            }

            if (response.data.Count <= 0)
            {
                lastError = "No sources returned from API (Verify USA vs US and sector names)";
                Debug.LogWarning($"[ClimateTraceController] ⚠️ {lastError}");
                return;
            }

            Debug.Log($"[ClimateTraceController] 📊 Received {response.data.Count} sources from API");

            List<EmissionsSource> finalSources = filterByLocation
                ? FilterByLocation(response.data)
                : response.data;

            SaveToScriptableObject(finalSources);
            LogSummary(finalSources);
        }

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

        private void SaveToScriptableObject(List<EmissionsSource> sources)
        {
            emissionsData.sources = sources;
            emissionsData.queryLatitude = centerLatitude;
            emissionsData.queryLongitude = centerLongitude;
            emissionsData.queryRadiusKm = radiusKm;
            emissionsData.lastUpdated = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            Debug.Log($"[ClimateTraceController] ✅ Saved {sources.Count} sources to {emissionsData.name}");

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(emissionsData);
            UnityEditor.EditorUtility.SetDirty(this); // Also mark this controller as dirty
#endif
        }

        private void LogSummary(List<EmissionsSource> sources)
        {
            int sampleCount = Mathf.Min(3, sources.Count);
            if (sampleCount > 0)
            {
                Debug.Log("[ClimateTraceController] 📋 Sample sources:");
                for (var i = 0; i < sampleCount; i++)
                {
                    EmissionsSource s = sources[i];
                    float lat = s.centroid?.latitude ?? 0;
                    float lon = s.centroid?.longitude ?? 0;
                    Debug.Log($"  • {s.name}");
                    Debug.Log($"    └─ Sector: {s.sector} / {s.subsector}");
                    Debug.Log($"    └─ Emissions: {s.emissionsQuantity:N0} t {s.gas} ({s.year})");
                    Debug.Log($"    └─ Location: ({lat:F4}, {lon:F4})");
                }
            }

            Debug.Log($"[ClimateTraceController] 📈 Total emissions: {emissionsData.TotalEmissions:N0} t CO2e");

            Dictionary<string, int> sectorCounts = emissionsData.SourcesBySector;
            if (sectorCounts.Count <= 0) return;

            Debug.Log("[ClimateTraceController] 📊 Sources by sector:");
            foreach (KeyValuePair<string, int> kvp in sectorCounts)
            {
                Debug.Log($"  • {kvp.Key}: {kvp.Value} sources");
            }
        }
    }
}