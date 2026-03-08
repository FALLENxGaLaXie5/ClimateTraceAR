using System.Collections.Generic;
using Data_Management;
using Data_Management.Runtime;
using Data_Management.Runtime.Scriptable_Objects;
using NUnit.Framework;
using UnityEngine;

namespace Data_Management.Tests
{
    public class EmissionsDataTests
    {
        private EmissionsData emissionsData;

        [SetUp]
        public void SetUp()
        {
            emissionsData = ScriptableObject.CreateInstance<EmissionsData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(emissionsData);
        }

        #region TotalSources Tests

        [Test]
        public void TotalSources_EmptyList_ReturnsZero()
        {
            Assert.AreEqual(0, emissionsData.TotalSources);
        }

        [Test]
        public void TotalSources_WithSources_ReturnsCorrectCount()
        {
            emissionsData.sources.Add(CreateEmissionsSource());
            emissionsData.sources.Add(CreateEmissionsSource());
            emissionsData.sources.Add(CreateEmissionsSource());

            Assert.AreEqual(3, emissionsData.TotalSources);
        }

        #endregion

        #region TotalEmissions Tests

        [Test]
        public void TotalEmissions_EmptyList_ReturnsZero()
        {
            Assert.AreEqual(0f, emissionsData.TotalEmissions);
        }

        [Test]
        public void TotalEmissions_SingleSource_ReturnsCorrectValue()
        {
            emissionsData.sources.Add(CreateEmissionsSource(emissionsQuantity: 1000f));

            Assert.AreEqual(1000f, emissionsData.TotalEmissions);
        }

        [Test]
        public void TotalEmissions_MultipleSources_SumsCorrectly()
        {
            emissionsData.sources.Add(CreateEmissionsSource(emissionsQuantity: 1000f));
            emissionsData.sources.Add(CreateEmissionsSource(emissionsQuantity: 2500f));
            emissionsData.sources.Add(CreateEmissionsSource(emissionsQuantity: 500f));

            Assert.AreEqual(4000f, emissionsData.TotalEmissions, 0.01f);
        }

        [Test]
        public void TotalEmissions_IncludesSourcesWithoutSector()
        {
            emissionsData.sources.Add(CreateEmissionsSource(sector: "energy", emissionsQuantity: 1000f));
            emissionsData.sources.Add(CreateEmissionsSource(sector: null, emissionsQuantity: 500f));
            emissionsData.sources.Add(CreateEmissionsSource(sector: "", emissionsQuantity: 250f));

            Assert.AreEqual(1750f, emissionsData.TotalEmissions, 0.01f);
        }

        [Test]
        public void TotalEmissions_WithZeroValues_HandlesCorrectly()
        {
            emissionsData.sources.Add(CreateEmissionsSource(emissionsQuantity: 0f));
            emissionsData.sources.Add(CreateEmissionsSource(emissionsQuantity: 1000f));
            emissionsData.sources.Add(CreateEmissionsSource(emissionsQuantity: 0f));

            Assert.AreEqual(1000f, emissionsData.TotalEmissions);
        }

        [Test]
        public void TotalEmissions_WithNegativeValues_SumsCorrectly()
        {
            // Edge case - shouldn't happen in real data but tests robustness
            emissionsData.sources.Add(CreateEmissionsSource(emissionsQuantity: 1000f));
            emissionsData.sources.Add(CreateEmissionsSource(emissionsQuantity: -100f));

            Assert.AreEqual(900f, emissionsData.TotalEmissions, 0.01f);
        }

        [Test]
        public void TotalEmissions_WithLargeValues_HandlesCorrectly()
        {
            emissionsData.sources.Add(CreateEmissionsSource(emissionsQuantity: 500000000f));
            emissionsData.sources.Add(CreateEmissionsSource(emissionsQuantity: 300000000f));

            Assert.AreEqual(800000000f, emissionsData.TotalEmissions, 0.01f);
        }

        #endregion

        #region SourcesBySector Tests

        [Test]
        public void SourcesBySector_EmptyList_ReturnsEmptyDictionary()
        {
            var result = emissionsData.SourcesBySector;

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void SourcesBySector_SingleSector_ReturnsCorrectCount()
        {
            emissionsData.sources.Add(CreateEmissionsSource(sector: "energy"));
            emissionsData.sources.Add(CreateEmissionsSource(sector: "energy"));
            emissionsData.sources.Add(CreateEmissionsSource(sector: "energy"));

            var result = emissionsData.SourcesBySector;

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(3, result["energy"]);
        }

        [Test]
        public void SourcesBySector_MultipleSectors_CountsCorrectly()
        {
            emissionsData.sources.Add(CreateEmissionsSource(sector: "energy"));
            emissionsData.sources.Add(CreateEmissionsSource(sector: "energy"));
            emissionsData.sources.Add(CreateEmissionsSource(sector: "transport"));
            emissionsData.sources.Add(CreateEmissionsSource(sector: "agriculture"));
            emissionsData.sources.Add(CreateEmissionsSource(sector: "transport"));

            var result = emissionsData.SourcesBySector;

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(2, result["energy"]);
            Assert.AreEqual(2, result["transport"]);
            Assert.AreEqual(1, result["agriculture"]);
        }

        [Test]
        public void SourcesBySector_ExcludesNullSectors()
        {
            emissionsData.sources.Add(CreateEmissionsSource(sector: "energy"));
            emissionsData.sources.Add(CreateEmissionsSource(sector: null));
            emissionsData.sources.Add(CreateEmissionsSource(sector: "transport"));

            var result = emissionsData.SourcesBySector;

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.ContainsKey("energy"));
            Assert.IsTrue(result.ContainsKey("transport"));
        }

        [Test]
        public void SourcesBySector_ExcludesEmptySectors()
        {
            emissionsData.sources.Add(CreateEmissionsSource(sector: "energy"));
            emissionsData.sources.Add(CreateEmissionsSource(sector: ""));
            emissionsData.sources.Add(CreateEmissionsSource(sector: "transport"));

            Dictionary<string, int> result = emissionsData.SourcesBySector;

            Assert.AreEqual(2, result.Count);
            Assert.IsFalse(result.ContainsKey(""));
        }

        [Test]
        public void SourcesBySector_CaseSensitive_TreatsDifferentCasesAsSeparate()
        {
            emissionsData.sources.Add(CreateEmissionsSource(sector: "Energy"));
            emissionsData.sources.Add(CreateEmissionsSource(sector: "energy"));
            emissionsData.sources.Add(CreateEmissionsSource(sector: "ENERGY"));

            var result = emissionsData.SourcesBySector;

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(1, result["Energy"]);
            Assert.AreEqual(1, result["energy"]);
            Assert.AreEqual(1, result["ENERGY"]);
        }

        [Test]
        public void SourcesBySector_AllSourcesWithoutSector_ReturnsEmpty()
        {
            emissionsData.sources.Add(CreateEmissionsSource(sector: null));
            emissionsData.sources.Add(CreateEmissionsSource(sector: ""));
            emissionsData.sources.Add(CreateEmissionsSource(sector: null));

            var result = emissionsData.SourcesBySector;

            Assert.AreEqual(0, result.Count);
        }

        #endregion

        #region EmissionsSource Tests

        [Test]
        public void EmissionsSource_Latitude_NullCentroid_ReturnsZero()
        {
            var source = new EmissionsSource { centroid = null };

            Assert.AreEqual(0f, source.Latitude);
        }

        [Test]
        public void EmissionsSource_Longitude_NullCentroid_ReturnsZero()
        {
            var source = new EmissionsSource { centroid = null };

            Assert.AreEqual(0f, source.Longitude);
        }

        [Test]
        public void EmissionsSource_Latitude_ValidCentroid_ReturnsCorrectValue()
        {
            var source = new EmissionsSource
            {
                centroid = new Centroid { latitude = 40.7128f, longitude = -74.0060f }
            };

            Assert.AreEqual(40.7128f, source.Latitude, 0.0001f);
        }

        [Test]
        public void EmissionsSource_Longitude_ValidCentroid_ReturnsCorrectValue()
        {
            var source = new EmissionsSource
            {
                centroid = new Centroid { latitude = 40.7128f, longitude = -74.0060f }
            };

            Assert.AreEqual(-74.0060f, source.Longitude, 0.0001f);
        }

        #endregion

        #region Helper Methods

        private EmissionsSource CreateEmissionsSource(
            int id = 1,
            string name = "Test Source",
            string sector = "energy",
            string subsector = "coal",
            string country = "US",
            float emissionsQuantity = 1000f,
            int year = 2024)
        {
            return new EmissionsSource
            {
                id = id,
                name = name,
                sector = sector,
                subsector = subsector,
                country = country,
                emissionsQuantity = emissionsQuantity,
                year = year,
                centroid = new Centroid { latitude = 0f, longitude = 0f, srid = 4326 }
            };
        }

        #endregion
    }
}