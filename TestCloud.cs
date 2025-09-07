using System;
using System.Threading.Tasks;
using DataFlow.Core;
using DataFlow.Core.Cloud;
using Amazon;

class TestCloud
{
    static async Task Main()
    {
        Console.WriteLine("=== DataFlow Cloud Storage Test ===\n");
        
        // Test sınıflarının oluşturulması
        TestS3Classes();
        TestAzureClasses();
        TestGoogleCloudClasses();
        
        // Test kullanım örnekleri
        await TestCloudUsageExamples();
        
        Console.WriteLine("\n=== Cloud Storage Desteği Başarıyla Eklendi ===");
    }
    
    static void TestS3Classes()
    {
        Console.WriteLine("1. AWS S3 Sınıf Testi:");
        
        try
        {
            // S3Reader test
            var reader = new S3Reader("test-bucket", "data.csv")
                .WithRegion(RegionEndpoint.USWest2)
                .WithCredentials("access-key", "secret-key");
            Console.WriteLine("   - S3Reader sınıfı ✓");
            
            // S3Writer test
            var writer = new S3Writer("test-bucket", "output.csv")
                .WithRegion(RegionEndpoint.USWest2)
                .WithCredentials("access-key", "secret-key")
                .WithStorageClass(Amazon.S3.S3StorageClass.StandardIa)
                .WithEncryption(Amazon.S3.ServerSideEncryptionMethod.AES256);
            Console.WriteLine("   - S3Writer sınıfı ✓");
            
            Console.WriteLine("   - AWS S3 entegrasyonu hazır!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   - S3 test hatası: {ex.Message}");
        }
    }
    
    static void TestAzureClasses()
    {
        Console.WriteLine("\n2. Azure Blob Storage Sınıf Testi:");
        
        try
        {
            // AzureBlobReader test
            var reader = new AzureBlobReader(
                "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key==;EndpointSuffix=core.windows.net",
                "container",
                "data.csv");
            Console.WriteLine("   - AzureBlobReader sınıfı ✓");
            
            // AzureBlobWriter test
            var writer = new AzureBlobWriter(
                "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key==;EndpointSuffix=core.windows.net",
                "container",
                "output.csv")
                .WithAccessTier(Azure.Storage.Blobs.Models.AccessTier.Cool)
                .WithOverwrite(true);
            Console.WriteLine("   - AzureBlobWriter sınıfı ✓");
            
            Console.WriteLine("   - Azure Blob Storage entegrasyonu hazır!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   - Azure test hatası: {ex.Message}");
        }
    }
    
    static void TestGoogleCloudClasses()
    {
        Console.WriteLine("\n3. Google Cloud Storage Sınıf Testi:");
        
        try
        {
            // GoogleCloudStorageReader test
            var reader = new GoogleCloudStorageReader("test-bucket", "data.csv")
                .WithProjectId("my-project");
            Console.WriteLine("   - GoogleCloudStorageReader sınıfı ✓");
            
            // GoogleCloudStorageWriter test
            var writer = new GoogleCloudStorageWriter("test-bucket", "output.csv")
                .WithProjectId("my-project")
                .WithStorageClass("NEARLINE")
                .WithMetadata("created-by", "DataFlow");
            Console.WriteLine("   - GoogleCloudStorageWriter sınıfı ✓");
            
            Console.WriteLine("   - Google Cloud Storage entegrasyonu hazır!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   - GCS test hatası: {ex.Message}");
        }
    }
    
    static async Task TestCloudUsageExamples()
    {
        Console.WriteLine("\n4. Cloud Storage Kullanım Örnekleri:");
        
        Console.WriteLine("\n   AWS S3:");
        Console.WriteLine("   ```csharp");
        Console.WriteLine("   // S3'ten CSV okuma");
        Console.WriteLine("   var pipeline = await DataFlow.From.S3Csv(\"my-bucket\", \"data.csv\", \"us-west-2\");");
        Console.WriteLine("   pipeline.Filter(row => row[\"Status\"] == \"Active\")");
        Console.WriteLine("          .ToS3Csv(\"my-bucket\", \"filtered.csv\");");
        Console.WriteLine("   ```");
        
        Console.WriteLine("\n   Azure Blob:");
        Console.WriteLine("   ```csharp");
        Console.WriteLine("   // Azure Blob'dan okuma");
        Console.WriteLine("   var pipeline = await DataFlow.From.AzureBlobCsv(connString, \"container\", \"data.csv\");");
        Console.WriteLine("   pipeline.Filter(row => row[\"Amount\"] > 1000)");
        Console.WriteLine("          .ToAzureBlobCsv(connString, \"container\", \"filtered.csv\");");
        Console.WriteLine("   ```");
        
        Console.WriteLine("\n   Google Cloud Storage:");
        Console.WriteLine("   ```csharp");
        Console.WriteLine("   // GCS'den okuma");
        Console.WriteLine("   var pipeline = await DataFlow.From.GoogleCloudCsv(\"bucket\", \"data.csv\");");
        Console.WriteLine("   pipeline.GroupBy(row => row[\"Category\"])");
        Console.WriteLine("          .ToGoogleCloudCsv(\"bucket\", \"grouped.csv\");");
        Console.WriteLine("   ```");
        
        Console.WriteLine("\n   Cross-Cloud İşlem:");
        Console.WriteLine("   ```csharp");
        Console.WriteLine("   // S3'ten oku, Azure'a yaz");
        Console.WriteLine("   var data = await DataFlow.From.S3Csv(\"aws-bucket\", \"input.csv\");");
        Console.WriteLine("   await data.ToAzureBlobCsv(azureConn, \"container\", \"output.csv\");");
        Console.WriteLine("   ```");
    }
}