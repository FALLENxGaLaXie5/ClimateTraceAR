using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Data_Management.Runtime.Scriptable_Objects
{
    [CreateAssetMenu(fileName = "EmissionsData", menuName = "Climate Trace/Emissions Data", order = 0)]
    public class EmissionsData : ScriptableObject
    {
        [Title("Query Info")]
        [ReadOnly]
        public float queryLatitude;

        [ReadOnly]
        public float queryLongitude;

        [ReadOnly]
        public float queryRadiusKm;

        [ReadOnly]
        public string lastUpdated;

        [Title("Emissions Sources")]
        [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        public List<EmissionsSource> sources = new();

        [Title("Statistics")]
        [ReadOnly, ShowInInspector]
        public int TotalSources => sources.Count;

        [ReadOnly, ShowInInspector]
        public float TotalEmissions => CalculateTotalEmissions();

        [ReadOnly, ShowInInspector]
        public Dictionary<string, int> SourcesBySector => GetSourcesBySector();

        private float CalculateTotalEmissions()
        {
            return sources.Sum(source => source.emissionsQuantity);
        }

        private Dictionary<string, int> GetSourcesBySector()
        {
            return sources
                .Where(s => !string.IsNullOrEmpty(s.sector))
                .GroupBy(s => s.sector)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}