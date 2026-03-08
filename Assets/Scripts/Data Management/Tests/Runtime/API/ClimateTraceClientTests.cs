using System.Collections;
using System.Collections.Generic;
using Data_Management.Runtime;
using Data_Management.Runtime.API.Clients;
using Data_Management.Runtime.API.Core;
using Data_Management.Runtime.Models;
using NUnit.Framework;

namespace Data_Management.Tests.Runtime.API
{
    public class ClimateTraceClientTests
    {
        private MockAPIClient mockClient;
        private ClimateTraceClient client;

        [SetUp]
        public void SetUp()
        {
            mockClient = new MockAPIClient();
            client = new ClimateTraceClient(mockClient);
        }

        [TearDown]
        public void TearDown()
        {
            mockClient.Reset();
        }

        private void RunCoroutine(IEnumerator coroutine)
        {
            while (coroutine.MoveNext())
            {
                if (coroutine.Current is IEnumerator nestedCoroutine)
                {
                    RunCoroutine(nestedCoroutine);
                }
            }
        }

        [Test]
        public void GetEmissionsSources_CallsAPIClient_WithListResponse()
        {
            // Arrange
            var query = new ClimateTraceQuery("USA", new List<string> { "power", "transportation" }, 50, 0);

            // Updated to match the flat array return type
            mockClient.MockResponse = new APIResponse<List<EmissionsSource>>
            {
                success = true,
                data = new List<EmissionsSource>()
            };

            APIResponse<List<EmissionsSource>> result = null;

            // Act
            RunCoroutine(client.GetEmissionsSources(query, r => result = r));

            // Assert
            Assert.AreEqual(1, mockClient.CallCount, "Should call API client once");
            Assert.IsNotNull(result, "Should receive a response");
            Assert.IsTrue(result.success, "API call should be successful");
        }

        [Test]
        public void GetEmissionsSources_SuccessResponse_ReturnsData()
        {
            // Arrange
            var expectedSources = new List<EmissionsSource>
            {
                new EmissionsSource { id = 1, name = "Test Source", sector = "power" },
                new EmissionsSource { id = 2, name = "Another Source", sector = "transportation" }
            };

            mockClient.MockResponse = new APIResponse<List<EmissionsSource>>
            {
                success = true,
                data = expectedSources,
                statusCode = 200
            };

            APIResponse<List<EmissionsSource>> result = null;

            // Act
            RunCoroutine(client.GetEmissionsSources(new ClimateTraceQuery(), r => result = r));

            // Assert
            Assert.IsTrue(result.success, "Response should be successful");
            Assert.AreEqual(2, result.data.Count, "Should return 2 sources");
            Assert.AreEqual("Test Source", result.data[0].name);
            Assert.AreEqual("power", result.data[0].sector);
        }

        [Test]
        public void GetEmissionsSources_ErrorResponse_ReturnsError()
        {
            // Arrange
            mockClient.MockResponse = new APIResponse<List<EmissionsSource>>
            {
                success = false,
                error = "Network timeout",
                statusCode = 408
            };

            APIResponse<List<EmissionsSource>> result = null;

            // Act
            RunCoroutine(client.GetEmissionsSources(new ClimateTraceQuery(), r => result = r));

            // Assert
            Assert.IsFalse(result.success, "Response should indicate failure");
            Assert.AreEqual("Network timeout", result.error);
            Assert.AreEqual(408, result.statusCode);
        }

        [Test]
        public void GetEmissionsSources_EmptyResponse_ReturnsEmptyList()
        {
            // Arrange
            mockClient.MockResponse = new APIResponse<List<EmissionsSource>>
            {
                success = true,
                data = new List<EmissionsSource>(),
                statusCode = 200
            };

            APIResponse<List<EmissionsSource>> result = null;

            // Act
            RunCoroutine(client.GetEmissionsSources(new ClimateTraceQuery(), r => result = r));

            // Assert
            Assert.IsTrue(result.success);
            Assert.IsNotNull(result.data);
            Assert.AreEqual(0, result.data.Count, "Should return empty list");
        }
    }
}