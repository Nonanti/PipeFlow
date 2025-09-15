using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PipeFlow.Core;
using PipeFlow.Core.Builder;
using System.Text.Json;

namespace TestNewFeatures;

public class RealWorldTest
{
  public static async Task RunRealWorldTests()
  {
    Console.WriteLine("\nREAL WORLD TESTS");
    Console.WriteLine("===================\n");
    
    await TestCsvToJsonConversion();
    await TestJsonProcessing();
    await TestLargeDatasetProcessing();
    await TestParallelProcessing();
    
    Console.WriteLine("\nAll real-world tests completed!");
  }
  
  static async Task TestCsvToJsonConversion()
  {
    Console.WriteLine("Test: CSV to JSON Conversion");
    
    var csvPath = "employees.csv";
    var csvContent = @"EmployeeId,Name,Department,Salary,HireDate,IsActive
1,John Doe,Engineering,95000,2020-01-15,true
2,Jane Smith,Marketing,75000,2019-06-20,true
3,Bob Johnson,Sales,85000,2021-03-10,true
4,Alice Brown,Engineering,105000,2018-11-05,true
5,Charlie Wilson,HR,65000,2022-02-28,false
6,Diana Prince,Engineering,115000,2017-09-12,true
7,Eve Anderson,Marketing,70000,2020-07-01,true
8,Frank Miller,Sales,90000,2019-12-15,false";
    
    await File.WriteAllTextAsync(csvPath, csvContent);
    
    var jsonPath = "employees.json";
    
    var pipeline = PipeFlowBuilder
      .FromCsv(csvPath)
      .Filter(row => row["IsActive"]?.ToString()?.ToLower() == "true")
      .Map(row => new
      {
        Id = Convert.ToInt32(row["EmployeeId"]),
        Name = row["Name"]?.ToString(),
        Department = row["Department"]?.ToString(),
        Salary = Convert.ToDecimal(row["Salary"]),
        HireDate = row["HireDate"]?.ToString(),
        YearsOfService = DateTime.Now.Year - DateTime.Parse(row["HireDate"]?.ToString() ?? "2024-01-01").Year
      })
      .OrderByDescending(emp => emp.Salary)
      .Build();
    
    var result = await pipeline.ExecuteAsync();
    
    var json = JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync(jsonPath, json);
    
    Console.WriteLine($"   Processed {result.ProcessedCount} active employees");
    Console.WriteLine($"   Saved to {jsonPath}");
    
    if (File.Exists(jsonPath))
    {
      var jsonContent = await File.ReadAllTextAsync(jsonPath);
      Console.WriteLine($"   JSON file created ({jsonContent.Length} bytes)");
    }
    
    File.Delete(csvPath);
    File.Delete(jsonPath);
  }
  
  static async Task TestJsonProcessing()
  {
    Console.WriteLine("\nTest: JSON Data Processing");
    
    var jsonPath = "products.json";
    var products = new[]
    {
      new { Id = 1, Name = "Laptop", Category = "Electronics", Price = 1200, Stock = 10 },
      new { Id = 2, Name = "Mouse", Category = "Electronics", Price = 25, Stock = 100 },
      new { Id = 3, Name = "Desk", Category = "Furniture", Price = 350, Stock = 5 },
      new { Id = 4, Name = "Chair", Category = "Furniture", Price = 150, Stock = 20 },
      new { Id = 5, Name = "Monitor", Category = "Electronics", Price = 400, Stock = 15 }
    };
    
    var jsonContent = JsonSerializer.Serialize(products, new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync(jsonPath, jsonContent);
    
    var pipeline = PipeFlowBuilder
      .FromJson(jsonPath)
      .Filter(row => row["Category"]?.ToString() == "Electronics")
      .Map(row => new
      {
        Product = row["Name"]?.ToString(),
        TotalValue = Convert.ToDecimal(row["Price"]) * Convert.ToInt32(row["Stock"]),
        Category = row["Category"]?.ToString()
      })
      .OrderByDescending(item => item.TotalValue)
      .Build();
    
    var result = await pipeline.ExecuteAsync();
    var electronics = result.Data.ToList();
    
    Console.WriteLine($"   Found {electronics.Count} electronics items");
    foreach (var item in electronics.Take(3))
    {
      Console.WriteLine($"     - {item.Product}: ${item.TotalValue:N0} inventory value");
    }
    
    File.Delete(jsonPath);
  }
  
  static async Task TestLargeDatasetProcessing()
  {
    Console.WriteLine("\nTest: Large Dataset Processing");
    
    var recordCount = 50000;
    var random = new Random(42);
    
    var largeDataset = Enumerable.Range(1, recordCount)
      .Select(i => new DataRow
      {
        ["Id"] = i,
        ["CustomerId"] = $"CUST{random.Next(1, 1000):D4}",
        ["Amount"] = random.Next(10, 10000),
        ["Date"] = DateTime.Now.AddDays(-random.Next(1, 365)).ToString("yyyy-MM-dd"),
        ["Status"] = random.Next(1, 10) > 3 ? "Completed" : "Pending",
        ["Category"] = new[] { "Electronics", "Clothing", "Food", "Books" }[random.Next(0, 4)]
      });
    
    var pipeline = PipeFlowBuilder
      .FromCollection(largeDataset)
      .Filter(row => row["Status"]?.ToString() == "Completed")
      .Filter(row => Convert.ToDecimal(row["Amount"]) > 1000)
      .Map(row => new
      {
        CustomerId = row["CustomerId"]?.ToString(),
        Amount = Convert.ToDecimal(row["Amount"]),
        Category = row["Category"]?.ToString()
      })
      .Take(1000)
      .Build();
    
    var startTime = DateTime.Now;
    var result = await pipeline.ExecuteAsync();
    var elapsed = DateTime.Now - startTime;
    
    Console.WriteLine($"   Processed {recordCount:N0} records in {elapsed.TotalMilliseconds:N0}ms");
    Console.WriteLine($"   Filtered to {result.ProcessedCount} high-value completed orders");
    Console.WriteLine($"   Processing rate: {recordCount / elapsed.TotalSeconds:N0} records/second");
  }
  
  static async Task TestParallelProcessing()
  {
    Console.WriteLine("\nTest: Parallel Processing Performance");
    
    var dataSize = 10000;
    var data = Enumerable.Range(1, dataSize)
      .Select(i => new DataRow
      {
        ["Id"] = i,
        ["Value"] = i,
        ["Category"] = i % 5
      });
    
    // Sequential processing
    var sequentialPipeline = PipeFlowBuilder
      .FromCollection(data)
      .Map(row => new
      {
        Id = row["Id"],
        ProcessedValue = SimulateHeavyProcessing(Convert.ToInt32(row["Value"]))
      })
      .Build();
    
    var startSeq = DateTime.Now;
    var seqResult = await sequentialPipeline.ExecuteAsync();
    var seqTime = DateTime.Now - startSeq;
    
    // Parallel processing
    var parallelPipeline = PipeFlowBuilder
      .FromCollection(data)
      .AsParallel(maxDegreeOfParallelism: 4)
      .Map(row => new
      {
        Id = row["Id"],
        ProcessedValue = SimulateHeavyProcessing(Convert.ToInt32(row["Value"]))
      })
      .Build();
    
    var startPar = DateTime.Now;
    var parResult = await parallelPipeline.ExecuteAsync();
    var parTime = DateTime.Now - startPar;
    
    Console.WriteLine($"   Sequential: {seqTime.TotalMilliseconds:N0}ms");
    Console.WriteLine($"   Parallel (4 threads): {parTime.TotalMilliseconds:N0}ms");
    Console.WriteLine($"   Speed improvement: {(seqTime.TotalMilliseconds / parTime.TotalMilliseconds):N1}x faster");
  }
  
  static double SimulateHeavyProcessing(int value)
  {
    // Simulate CPU-intensive operation
    double result = value;
    for (int i = 0; i < 100; i++)
    {
      result = Math.Sqrt(result * value) + Math.Sin(result);
    }
    return result;
  }
}