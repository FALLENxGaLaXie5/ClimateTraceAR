using System;
using System.Collections.Generic;

namespace Data_Management.Runtime.Models
{
    [Serializable]
    public class ClimateTraceRootResponse
    {
        public List<EmissionsSource> assets;

        public List<float> bbox;
        public int total;
    }
}