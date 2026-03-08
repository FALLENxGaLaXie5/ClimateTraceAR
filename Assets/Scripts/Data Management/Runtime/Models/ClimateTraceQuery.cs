using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Data_Management.Runtime.Models
{
    /// <summary>
    /// Query parameters for Climate Trace emissions sources API.
    /// Encapsulates all the parameters that can be sent to the /v6/assets endpoint.
    /// </summary>
    [Serializable]
    public class ClimateTraceQuery
    {
        public string countryCode;
        [LabelText("Fetch All Sectors")]
        [Tooltip("If checked, ignores the list below and fetches industrial point sources (all_no_forest).")]
        public bool allSectors = true;

        [DisableIf("allSectors")]
        [ListDrawerSettings(DefaultExpandedState = true)]
        public List<string> sectors = new();
        public int limit = 100;

        /// <summary>
        /// Offset for pagination (number of results to skip).
        /// Use in combination with limit for paginated queries.
        /// </summary>
        public int offset;

        public ClimateTraceQuery()
        {
        }

        public ClimateTraceQuery(string countryCode = "", List<string> sectors = null, int limit = 100, int offset = 0)
        {
            this.countryCode = countryCode;
            this.sectors = sectors ?? new List<string>();
            this.limit = limit;
            this.offset = offset;
        }
    }
}
