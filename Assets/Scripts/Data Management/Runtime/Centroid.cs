using System;
using Newtonsoft.Json;

namespace Data_Management.Runtime
{
    [Serializable]
    public class Centroid
    {
        [JsonProperty("longitude")]
        public float longitude;

        [JsonProperty("latitude")]
        public float latitude;

        [JsonProperty("srid")]
        public int srid;
    }
}