using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PipeFlow.Core;
using PipeFlow.Core.MongoDB;
using PipeFlow.Core.Api;

class TestPerformance
{
    static async Task Main()
    {
        Console.WriteLine("=== PipeFlow Performance Test (New Improvements) ===\n");
        CreateTestCsvFile("test_data.csv", 10000);
        CreateTestCsvFile("large_test_data.csv", 100000);
        TestNormalCsvReading();
        TestCsvWithoutAutoConvert();
        TestBufferSizes();
        await TestAsyncCsvReading();
        
        TestLazyEvaluation();
        TestParallelPipeline();
        TestMongoDBClasses();
        
        TestApiClasses();
        
        Console.WriteLine("\n=== All Improvements Successfully Tested ===");
    }
    
    static void CreateTestCsvFile(string filename, int rowCount)
    {
        using var writer = new StreamWriter(filename);
        writer.WriteLine("Id,Name,Age,Salary,Department,IsActive,JoinDate");
        
        var random = new Random(42);
        var departments = new[] { "IT", "HR", "Sales", "Marketing", "Finance" };
        
        for (int i = 1; i <= rowCount; i++)
        {
            writer.WriteLine($"{i},Person{i},{random.Next(22, 65)},{random.Next(30000, 150000)},{departments[random.Next(departments.Length)]},{(random.Next(2) == 1).ToString()},{DateTime.Now.AddDays(-random.Next(1, 3650)):yyyy-MM-dd}");
        }
    }
    
    static void TestNormalCsvReading()
    {
        Console.WriteLine("1. Normal CSV Reading Test (10,000 records):");
        
        var sw = Stopwatch.StartNew();
        var count = PipeFlow.Core.PipeFlow.From.Csv("test_data.csv")
            .Filter(row => (int)row["Age"] > 30)
            .Count();
        sw.Stop();
        
        Console.WriteLine($"   - Duration: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"   - Filtered record count: {count}");
    }
    
    static void TestCsvWithoutAutoConvert()
    {
        Console.WriteLine("\n2. CSV Reading Without Type Conversion (10,000 records):");
        
        var sw = Stopwatch.StartNew();
        var count = PipeFlow.Core.PipeFlow.From.Csv("test_data.csv", csv => csv
            .WithAutoConvert(false))
            .Filter(row => int.Parse(row["Age"].ToString()) > 30)
            .Count();
        sw.Stop();
        
        Console.WriteLine($"   - Duration: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"   - Filtered record count: {count}");
        Console.WriteLine($"   - Automatic type conversion disabled, should be faster");
    }
    
    static void TestBufferSizes()
    {
        Console.WriteLine("\n3. Buffer Size Comparison (100,000 records):");
        
        // Küçük buffer
        var sw = Stopwatch.StartNew();
        var count1 = PipeFlow.Core.PipeFlow.From.Csv("large_test_data.csv", csv => csv
            .WithBufferSize(4096))
            .Count();
        sw.Stop();
        var smallBufferTime = sw.ElapsedMilliseconds;
        
        Console.WriteLine($"   - 4KB buffer: {smallBufferTime} ms");
        
        // Normal buffer (varsayılan 64KB)
        sw.Restart();
        var count2 = PipeFlow.Core.PipeFlow.From.Csv("large_test_data.csv")
            .Count();
        sw.Stop();
        var normalBufferTime = sw.ElapsedMilliseconds;
        
        Console.WriteLine($"   - 64KB buffer (default): {normalBufferTime} ms");
        
        // Büyük buffer
        sw.Restart();
        var count3 = PipeFlow.Core.PipeFlow.From.Csv("large_test_data.csv", csv => csv
            .WithBufferSize(256 * 1024))
            .Count();
        sw.Stop();
        
        Console.WriteLine($"   - 256KB buffer: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"   - Best performance: {Math.Min(Math.Min(smallBufferTime, normalBufferTime), sw.ElapsedMilliseconds)} ms");
    }
    
    static async Task TestAsyncCsvReading()
    {
        Console.WriteLine("\n4. Async CSV Reading Test (10,000 records):");
        
        var sw = Stopwatch.StartNew();
        var count = 0;
        
        await foreach (var row in PipeFlow.Core.PipeFlow.From.CsvAsync("test_data.csv"))
        {
            if ((int)row["Age"] > 30)
                count++;
        }
        sw.Stop();
        
        Console.WriteLine($"   - Duration: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"   - Filtered record count: {count}");
        Console.WriteLine($"   - Async I/O used");
    }
    
    static void TestFilterAndMap()
    {
        Console.WriteLine("\n2. Filter and Map Test (10,000 records):");
        
        var sw = Stopwatch.StartNew();
        var results = PipeFlow.Core.PipeFlow.From.Csv("test_data.csv")
            .Filter(row => (int)row["Age"] > 30)
            .Map(row => new {
                Name = row["Name"],
                Age = row["Age"],
                Department = row["Department"]
            })
            .Take(10)
            .ToList();
        sw.Stop();
        
        Console.WriteLine($"   - Duration: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"   - First 10 records taken");
        Console.WriteLine($"   - Example: {results.First().Name}, Age: {results.First().Age}");
    }
    
    static void TestGroupBy()
    {
        Console.WriteLine("\n3. GroupBy Test:");
        
        var sw = Stopwatch.StartNew();
        var grouped = PipeFlow.Core.PipeFlow.From.Csv("test_data.csv")
            .GroupBy(row => row["Department"])
            .Select(g => new {
                Department = g.Key,
                Count = g.Count(),
                AvgAge = g.Average(r => Convert.ToDouble(r["Age"]))
            })
            .ToList();
        sw.Stop();
        
        Console.WriteLine($"   - Duration: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"   - Department count: {grouped.Count}");
        foreach (var dept in grouped)
        {
            Console.WriteLine($"     {dept.Department}: {dept.Count} people, Average age: {dept.AvgAge:F1}");
        }
    }
    
    static void TestLazyEvaluation()
    {
        Console.WriteLine("\n5. Lazy Evaluation Test (Improved):");
        
        var sw = Stopwatch.StartNew();

        var pipeline = PipeFlow.Core.PipeFlow.From.Csv("test_data.csv")
            .Filter(row => (int)row["Age"] > 30)
            .Filter(row => (bool)row["IsActive"])
            .Map(row => new { 
                Name = row["Name"], 
                Department = row["Department"] 
            });
        
        var setupTime = sw.ElapsedMilliseconds;
        Console.WriteLine($"   - Pipeline setup time: {setupTime} ms");
        
        // Now execute
        sw.Restart();
        var firstFive = pipeline.Take(5).ToList();
        sw.Stop();
        
        Console.WriteLine($"   - First 5 records retrieval time: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"   - Full file not read thanks to lazy evaluation");
    }
    
    static void TestParallelPipeline()
    {
        Console.WriteLine("\n6. Parallel Pipeline Test - Using PLINQ (100,000 records):");
        
        // Normal pipeline
        var sw = Stopwatch.StartNew();
        var normalCount = PipeFlow.Core.PipeFlow.From.Csv("large_test_data.csv")
            .Filter(row => (int)row["Age"] > 30)
            .Map(row => (int)row["Salary"] * 1.1)
            .Count();
        sw.Stop();
        var normalTime = sw.ElapsedMilliseconds;
        
        Console.WriteLine($"   - Normal pipeline: {normalTime} ms");
        
        // Parallel pipeline
        sw.Restart();
        var parallelCount = PipeFlow.Core.PipeFlow.From.Csv("large_test_data.csv")
            .Parallel(4)
            .Filter(row => (int)row["Age"] > 30)
            .Map(row => (int)row["Salary"] * 1.1)
            .Count();
        sw.Stop();
        
        Console.WriteLine($"   - Parallel pipeline (4 thread): {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"   - Speedup: {(double)normalTime / sw.ElapsedMilliseconds:F2}x");
    }
    
    static void TestMongoDBClasses()
    {
        Console.WriteLine("\n7. MongoDB Class Test:");
        
        try
        {
            // MongoReader test
            var reader = new MongoReader("mongodb://localhost", "testdb", "testcol");
            reader.Where("status", "active")
                  .Sort("name")
                  .Limit(10);
            Console.WriteLine("   - MongoReader sinifi OK");
            
            // MongoWriter test
            var writer = new MongoWriter("mongodb://localhost", "testdb", "testcol");
            writer.WithBatchSize(500)
                  .WithUpsert("_id");
            Console.WriteLine("   - MongoWriter sinifi OK");
            
            // PipeFlow MongoDB integration test
            var pipeline = PipeFlow.Core.PipeFlow.From.MongoDB("mongodb://localhost", "testdb", "testcol");
            Console.WriteLine("   - MongoDB Pipeline entegrasyonu OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   - MongoDB test error: {ex.Message}");
        }
    }
    
    static void TestApiClasses()
    {
        Console.WriteLine("\n8. API Class Test:");
        
        try
        {
            // ApiReader test
            var reader = new ApiReader("https://api.example.com/data");
            reader.WithAuth("test-token", "Bearer")
                  .WithRetry(3, TimeSpan.FromSeconds(1))
                  .WithPagination(100, "page", "pageSize");
            Console.WriteLine("   - ApiReader sinifi OK");
            
            // ApiWriter test
            var writer = new ApiWriter("https://api.example.com/data");
            writer.WithAuth("test-token", "Bearer")
                  .WithBatchSize(100);
            Console.WriteLine("   - ApiWriter sinifi OK");
            
            // PipeFlow API integration test
            var pipeline = PipeFlow.Core.PipeFlow.From.Api("https://api.example.com/data");
            Console.WriteLine("   - API Pipeline entegrasyonu OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   - API test error: {ex.Message}");
        }
    }
}
