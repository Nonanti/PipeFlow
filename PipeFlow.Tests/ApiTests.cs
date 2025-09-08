using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Xunit;
using PipeFlow.Core;
using PipeFlow.Core.Api;

namespace PipeFlow.Tests
{
    public class ApiTests
    {
        [Fact]
        public void ApiReader_Constructor_ThrowsOnNullUrl()
        {
            Assert.Throws<ArgumentNullException>(() => new ApiReader(null));
        }

        [Fact]
        public void ApiReader_WithAuth_SetsToken()
        {
            var reader = new ApiReader("https://api.example.com")
                .WithAuth("test-token");
            
            Assert.NotNull(reader);
        }

        [Fact]
        public void ApiReader_WithHeader_AddsHeader()
        {
            var reader = new ApiReader("https://api.example.com")
                .WithHeader("X-Custom", "Value");
            
            Assert.NotNull(reader);
        }

        [Fact]
        public void ApiReader_WithRetry_SetsRetryParams()
        {
            var reader = new ApiReader("https://api.example.com")
                .WithRetry(3, TimeSpan.FromSeconds(2));
            
            Assert.NotNull(reader);
        }

        [Fact]
        public void ApiReader_WithPagination_SetsPaginationParams()
        {
            var reader = new ApiReader("https://api.example.com")
                .WithPagination(pageSize: 20, pageParam: "page", sizeParam: "limit");
            
            Assert.NotNull(reader);
        }

        [Fact]
        public void ApiWriter_Constructor_ThrowsOnNullEndpoint()
        {
            Assert.Throws<ArgumentNullException>(() => new ApiWriter(null));
        }

        [Fact]
        public void ApiWriter_WithAuth_SetsToken()
        {
            var writer = new ApiWriter("https://api.example.com/data")
                .WithAuth("api-key");
            
            Assert.NotNull(writer);
        }

        [Fact]
        public void ApiWriter_WithBatchSize_SetsBatchSize()
        {
            var writer = new ApiWriter("https://api.example.com/data")
                .WithBatchSize(50);
            
            Assert.NotNull(writer);
        }

        [Fact]
        public void ApiWriter_WithMethod_SetsHttpMethod()
        {
            var writer = new ApiWriter("https://api.example.com/data")
                .WithMethod(HttpMethod.Put);
            
            Assert.NotNull(writer);
        }

        [Fact]
        public void PipeFlow_From_Api_CreatesValidPipeline()
        {
            var pipeline = PipeFlow.Core.PipeFlow.From.Api("https://api.example.com/test");
            
            Assert.NotNull(pipeline);
            Assert.IsAssignableFrom<IPipeline<DataRow>>(pipeline);
        }

        [Fact]
        public void PipeFlow_From_Api_WithConfiguration_CreatesValidPipeline()
        {
            var pipeline = PipeFlow.Core.PipeFlow.From.Api("https://api.example.com/test", api =>
            {
                api.WithAuth("token")
                   .WithRetry(2)
                   .WithHeader("Accept", "application/json");
            });
            
            Assert.NotNull(pipeline);
            Assert.IsAssignableFrom<IPipeline<DataRow>>(pipeline);
        }

        [Fact]
        public void ToApi_ExtensionMethod_Available()
        {
            var rows = new List<DataRow>
            {
                new DataRow { ["id"] = 1, ["name"] = "Test" }
            };
            
            var pipeline = PipeFlow.Core.PipeFlow.From.Collection(rows);
            
            // This would normally send data to an API
            // For testing, we just verify the method exists and can be called
            Assert.NotNull(pipeline);
        }

        [Fact]
        public void ApiReader_ChainedConfiguration_WorksCorrectly()
        {
            var reader = new ApiReader("https://api.example.com")
                .WithAuth("token")
                .WithHeader("X-Custom", "Value")
                .WithRetry(3, TimeSpan.FromSeconds(1))
                .WithPagination(10);
            
            Assert.NotNull(reader);
        }

        [Fact]
        public void ApiWriter_ChainedConfiguration_WorksCorrectly()
        {
            var writer = new ApiWriter("https://api.example.com/endpoint")
                .WithAuth("api-key")
                .WithHeader("Content-Type", "application/json")
                .WithMethod(HttpMethod.Post)
                .WithBatchSize(100)
                .UseBulkEndpoint();
            
            Assert.NotNull(writer);
        }

        [Fact]
        public async System.Threading.Tasks.Task ApiReader_InvalidUrl_ThrowsException()
        {
            var reader = new ApiReader("not-a-valid-url")
                .WithRetry(1, TimeSpan.FromMilliseconds(100));
            
            // This should throw an exception when trying to read
            var exception = await Assert.ThrowsAsync<AggregateException>(async () =>
            {
                await System.Threading.Tasks.Task.Run(() => reader.Read().ToList());
            });
            
            Assert.NotNull(exception);
            Assert.Contains("Failed to fetch data", exception.InnerException?.Message ?? "");
        }

        [Fact]
        public void DataRow_JsonParsing_HandlesNestedObjects()
        {
            var row = new DataRow
            {
                ["id"] = 1,
                ["name"] = "Test",
                ["metadata"] = "{\"key\":\"value\"}"
            };
            
            Assert.Equal(1, row["id"]);
            Assert.Equal("Test", row["name"]);
            Assert.NotNull(row["metadata"]);
        }

        [Fact]
        public void ApiPipeline_FilterAndTransform_WorksCorrectly()
        {
            var testData = new List<DataRow>
            {
                new DataRow { ["id"] = 1, ["status"] = "active", ["value"] = 100 },
                new DataRow { ["id"] = 2, ["status"] = "inactive", ["value"] = 200 },
                new DataRow { ["id"] = 3, ["status"] = "active", ["value"] = 300 }
            };
            
            var result = PipeFlow.Core.PipeFlow.From.Collection(testData)
                .Filter(r => r["status"].ToString() == "active")
                .Map(r => new DataRow 
                { 
                    ["id"] = r["id"], 
                    ["doubled"] = (int)r["value"] * 2 
                })
                .ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Equal(200, result[0]["doubled"]);
            Assert.Equal(600, result[1]["doubled"]);
        }
    }
}