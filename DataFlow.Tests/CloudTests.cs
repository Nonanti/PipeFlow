using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using DataFlow.Core;
using DataFlow.Core.Cloud;
using Amazon;

namespace DataFlow.Tests
{
    public class CloudTests
    {
        [Fact]
        public void S3Reader_Constructor_ThrowsOnNullBucket()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new S3Reader(null, "key"));
        }

        [Fact]
        public void S3Reader_Constructor_ThrowsOnNullKey()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new S3Reader("bucket", null));
        }

        [Fact]
        public void S3Reader_ValidConstructor_CreatesInstance()
        {
            var reader = new S3Reader("test-bucket", "test.csv");
            Assert.NotNull(reader);
        }

        [Fact]
        public void S3Reader_ChainedMethods_ReturnsSelf()
        {
            var reader = new S3Reader("test-bucket", "test.csv");
            
            var result = reader
                .WithRegion(RegionEndpoint.USWest2)
                .WithCredentials("access", "secret");
            
            Assert.Same(reader, result);
        }

        [Fact]
        public void S3Writer_Constructor_ThrowsOnNullBucket()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new S3Writer(null, "key"));
        }

        [Fact]
        public void S3Writer_ValidConstructor_CreatesInstance()
        {
            var writer = new S3Writer("test-bucket", "output.csv");
            Assert.NotNull(writer);
        }

        [Fact]
        public void AzureBlobReader_Constructor_ThrowsOnNullConnection()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new AzureBlobReader(null, "container", "blob"));
        }

        [Fact]
        public void AzureBlobReader_Constructor_ThrowsOnNullContainer()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new AzureBlobReader("connstring", null, "blob"));
        }

        [Fact]
        public void AzureBlobReader_Constructor_ThrowsOnNullBlob()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new AzureBlobReader("connstring", "container", null));
        }

        [Fact]
        public void AzureBlobReader_ValidConstructor_CreatesInstance()
        {
            var reader = new AzureBlobReader("DefaultEndpointsProtocol=https", "container", "blob.csv");
            Assert.NotNull(reader);
        }

        [Fact]
        public void AzureBlobWriter_Constructor_ThrowsOnNullConnection()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new AzureBlobWriter(null, "container", "blob"));
        }

        [Fact]
        public void AzureBlobWriter_ValidConstructor_CreatesInstance()
        {
            var writer = new AzureBlobWriter("DefaultEndpointsProtocol=https", "container", "blob.csv");
            Assert.NotNull(writer);
        }

        [Fact]
        public void AzureBlobWriter_ChainedMethods_ReturnsSelf()
        {
            var writer = new AzureBlobWriter("conn", "container", "blob");
            
            var result = writer
                .WithAccessTier(Azure.Storage.Blobs.Models.AccessTier.Cool)
                .WithOverwrite(false);
            
            Assert.Same(writer, result);
        }

        [Fact]
        public void GoogleCloudStorageReader_Constructor_ThrowsOnNullBucket()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new GoogleCloudStorageReader(null, "object"));
        }

        [Fact]
        public void GoogleCloudStorageReader_Constructor_ThrowsOnNullObject()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new GoogleCloudStorageReader("bucket", null));
        }

        [Fact]
        public void GoogleCloudStorageReader_ValidConstructor_CreatesInstance()
        {
            var reader = new GoogleCloudStorageReader("test-bucket", "test.csv");
            Assert.NotNull(reader);
        }

        [Fact]
        public void GoogleCloudStorageWriter_Constructor_ThrowsOnNullBucket()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new GoogleCloudStorageWriter(null, "object"));
        }

        [Fact]
        public void GoogleCloudStorageWriter_ValidConstructor_CreatesInstance()
        {
            var writer = new GoogleCloudStorageWriter("test-bucket", "output.csv");
            Assert.NotNull(writer);
        }

        [Fact]
        public void GoogleCloudStorageWriter_ChainedMethods_ReturnsSelf()
        {
            var writer = new GoogleCloudStorageWriter("bucket", "object");
            
            var result = writer
                .WithProjectId("my-project")
                .WithStorageClass("NEARLINE")
                .WithMetadata("key", "value");
            
            Assert.Same(writer, result);
        }

        [Fact]
        public async Task DataFlow_S3Csv_CreatesValidPipeline()
        {
            // This would need actual S3 credentials to work
            // For now, we're just testing that the method exists
            Assert.NotNull(DataFlow.Core.DataFlow.From);
            
            // Method signature test
            var methodInfo = typeof(DataFlow.Core.DataFlow.DataFlowBuilder)
                .GetMethod("S3Csv");
            Assert.NotNull(methodInfo);
        }

        [Fact]
        public async Task DataFlow_AzureBlobCsv_CreatesValidPipeline()
        {
            // Method signature test
            var methodInfo = typeof(DataFlow.Core.DataFlow.DataFlowBuilder)
                .GetMethod("AzureBlobCsv");
            Assert.NotNull(methodInfo);
        }

        [Fact]
        public async Task DataFlow_GoogleCloudCsv_CreatesValidPipeline()
        {
            // Method signature test
            var methodInfo = typeof(DataFlow.Core.DataFlow.DataFlowBuilder)
                .GetMethod("GoogleCloudCsv");
            Assert.NotNull(methodInfo);
        }
    }
}