using System;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Data_Management.Runtime
{
    [Serializable]
    public class EmissionsSource
    {
        [JsonProperty("id")]
        [ReadOnly, TableColumnWidth(70)]
        public int id;

        [JsonProperty("name")]
        [TableColumnWidth(250)]
        public string name;

        [JsonProperty("sector")]
        [TableColumnWidth(120)]
        public string sector;

        [JsonProperty("subsector")]
        [TableColumnWidth(150)]
        public string subsector;

        [JsonProperty("country")]
        [TableColumnWidth(60)]
        public string country;

        [JsonProperty("assetType")]
        [TableColumnWidth(100)]
        public string assetType;

        [JsonProperty("sourceType")]
        [TableColumnWidth(100)]
        public string sourceType;

        [JsonProperty("centroid")]
        [TableColumnWidth(200)]
        public Centroid centroid;

        [JsonProperty("gas")]
        [TableColumnWidth(80)]
        public string gas;

        [JsonProperty("emissionsQuantity")]
        [TableColumnWidth(120), SuffixLabel("t CO2e", true)]
        [ProgressBar(0, 300000000, ColorGetter = "GetEmissionsColor")]
        public float emissionsQuantity;

        [JsonProperty("emissionsFactor")]
        [TableColumnWidth(100)]
        public float emissionsFactor;

        [JsonProperty("emissionsFactorUnits")]
        [TableColumnWidth(150)]
        public string emissionsFactorUnits;

        [JsonProperty("activity")]
        [TableColumnWidth(100)]
        public float activity;

        [JsonProperty("activityUnits")]
        [TableColumnWidth(150)]
        public string activityUnits;

        [JsonProperty("capacity")]
        [TableColumnWidth(100)]
        public float capacity;

        [JsonProperty("capacityUnits")]
        [TableColumnWidth(150)]
        public string capacityUnits;

        [JsonProperty("capacityFactor")]
        [TableColumnWidth(100)]
        public float capacityFactor;

        [JsonProperty("year")]
        [TableColumnWidth(60)]
        public int year;

        // Computed properties for easier display in Odin
        [ShowInInspector, ReadOnly, TableColumnWidth(80)]
        public float Latitude => centroid?.latitude ?? 0;

        [ShowInInspector, ReadOnly, TableColumnWidth(80)]
        public float Longitude => centroid?.longitude ?? 0;

        private Color GetEmissionsColor()
        {
            if (emissionsQuantity < 10000000) return Color.green;      // < 10M
            if (emissionsQuantity < 100000000) return Color.yellow;    // < 100M
            return Color.red;                                           // > 100M
        }
    }
}