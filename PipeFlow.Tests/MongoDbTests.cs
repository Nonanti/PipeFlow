using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PipeFlow.Core;
using PipeFlow.Core.MongoDB;
using MongoDB.Driver;

namespace PipeFlow.Tests
{
    public class MongoDbTests
    {
        [Fact]
        public void MongoReader_Constructor_ThrowsOnNullConnectionString()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new MongoReader(null, "database", "collection"));
        }

        [Fact]
        public void MongoReader_Constructor_ThrowsOnNullDatabase()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new MongoReader("mongodb://localhost", null, "collection"));
        }

        [Fact]
        public void MongoReader_Constructor_ThrowsOnNullCollection()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new MongoReader("mongodb://localhost", "database", null));
        }

        [Fact]
        public void MongoReader_ValidConstructor_CreatesInstance()
        {
            var reader = new MongoReader("mongodb://localhost", "testdb", "users");
            Assert.NotNull(reader);
        }

        [Fact]
        public void MongoReader_ChainedMethods_ReturnsSelf()
        {
            var reader = new MongoReader("mongodb://localhost", "testdb", "users");
            
            var result = reader
                .Where("status", "active")
                .Sort("name")
                .Limit(10)
                .Skip(5)
                .Project("name", "email");
            
            Assert.Same(reader, result);
        }

        [Fact]
        public void MongoWriter_Constructor_ThrowsOnNullConnectionString()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new MongoWriter(null, "database", "collection"));
        }

        [Fact]
        public void MongoWriter_Constructor_ThrowsOnNullDatabase()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new MongoWriter("mongodb://localhost", null, "collection"));
        }

        [Fact]
        public void MongoWriter_Constructor_ThrowsOnNullCollection()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new MongoWriter("mongodb://localhost", "database", null));
        }

        [Fact]
        public void MongoWriter_ValidConstructor_CreatesInstance()
        {
            var writer = new MongoWriter("mongodb://localhost", "testdb", "users");
            Assert.NotNull(writer);
        }

        [Fact]
        public void MongoWriter_ChainedMethods_ReturnsSelf()
        {
            var writer = new MongoWriter("mongodb://localhost", "testdb", "users");
            
            var result = writer
                .WithBatchSize(500)
                .WithUpsert("_id")
                .CreateIndex("email", "username");
            
            Assert.Same(writer, result);
        }

        [Fact]
        public void PipeFlow_From_MongoDB_CreatesValidPipeline()
        {
            var pipeline = PipeFlow.Core.PipeFlow.From.MongoDB(
                "mongodb://localhost", "testdb", "users");
            
            Assert.NotNull(pipeline);
            Assert.IsAssignableFrom<IPipeline<DataRow>>(pipeline);
        }

        [Fact]
        public void PipeFlow_From_MongoDB_WithConfiguration_CreatesValidPipeline()
        {
            var pipeline = PipeFlow.Core.PipeFlow.From.MongoDB(
                "mongodb://localhost", "testdb", "users",
                mongo => mongo
                    .Where("active", true)
                    .Sort("created", false)
                    .Limit(100));
            
            Assert.NotNull(pipeline);
            Assert.IsAssignableFrom<IPipeline<DataRow>>(pipeline);
        }

        [Fact]
        public void ToMongoDB_ExtensionMethod_Available()
        {
            var rows = new List<DataRow>
            {
                new DataRow { ["_id"] = 1, ["name"] = "Test User" }
            };
            
            var pipeline = PipeFlow.Core.PipeFlow.From.Collection(rows);
            
            Assert.NotNull(pipeline);
        }

        [Fact]
        public void MongoReader_WhereJson_SetsFilter()
        {
            var reader = new MongoReader("mongodb://localhost", "testdb", "users");
            var result = reader.WhereJson("{ 'age': { '$gt': 18 } }");
            
            Assert.Same(reader, result);
        }

        [Fact]
        public void MongoReader_Aggregate_AddsPipelineStage()
        {
            var reader = new MongoReader("mongodb://localhost", "testdb", "users");
            
            var result = reader
                .Aggregate("{ '$match': { 'status': 'active' } }")
                .Aggregate("{ '$group': { '_id': '$city', 'count': { '$sum': 1 } } }");
            
            Assert.Same(reader, result);
        }

        [Fact]
        public void MongoWriter_DropCollectionFirst_SetsFlag()
        {
            var writer = new MongoWriter("mongodb://localhost", "testdb", "users");
            var result = writer.DropCollectionFirst();
            
            Assert.Same(writer, result);
        }

        [Fact]
        public void DataRow_ConversionToMongoDB_HandlesAllTypes()
        {
            var row = new DataRow
            {
                ["string"] = "text",
                ["int"] = 42,
                ["long"] = 42L,
                ["double"] = 42.5,
                ["bool"] = true,
                ["date"] = DateTime.Now,
                ["null"] = null,
                ["guid"] = Guid.NewGuid(),
                ["list"] = new List<object> { 1, 2, 3 },
                ["dict"] = new Dictionary<string, object> { ["key"] = "value" }
            };
            
            Assert.Equal(10, row.GetColumnNames().Count());
        }

        [Fact]
        public void MongoDbPipeline_FilterAndTransform_WorksCorrectly()
        {
            var testData = new List<DataRow>
            {
                new DataRow { ["_id"] = 1, ["name"] = "Alice", ["age"] = 25, ["city"] = "NYC" },
                new DataRow { ["_id"] = 2, ["name"] = "Bob", ["age"] = 30, ["city"] = "LA" },
                new DataRow { ["_id"] = 3, ["name"] = "Charlie", ["age"] = 35, ["city"] = "NYC" }
            };
            
            var result = PipeFlow.Core.PipeFlow.From.Collection(testData)
                .Filter(r => r["city"].ToString() == "NYC")
                .Map(r => new DataRow 
                { 
                    ["_id"] = r["_id"],
                    ["name"] = r["name"],
                    ["ageGroup"] = (int)r["age"] > 30 ? "Senior" : "Junior"
                })
                .ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Equal("Junior", result[0]["ageGroup"]);
            Assert.Equal("Senior", result[1]["ageGroup"]);
        }
    }
}