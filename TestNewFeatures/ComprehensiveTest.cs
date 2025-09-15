using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PipeFlow.Core;
using PipeFlow.Core.Builder;

namespace TestNewFeatures;

public class ComprehensiveTest
{
  public static async Task RunAllTests()
  {
    Console.WriteLine("\nCOMPREHENSIVE TESTING");
    Console.WriteLine("=========================\n");
    
    var allTestsPassed = true;
    
    try
    {
      await TestCsvReadWrite();
      await TestAsyncPipelineExecution();
      await TestFilterAndMap();
      await TestErrorHandling();
      await TestComplexPipeline();
      await TestStreamingMode();
      
      if (allTestsPassed)
      {
        Console.WriteLine("\nALL COMPREHENSIVE TESTS PASSED!");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"\nTEST FAILED: {ex.Message}");
      Console.WriteLine($"Stack: {ex.StackTrace}");
      allTestsPassed = false;
    }
  }
  
  static async Task TestCsvReadWrite()
  {
    Console.WriteLine("Test 1: CSV Read/Write Operations");
    
    var testCsvPath = "test_data.csv";
    var csvContent = @"Name,Age,Department,Salary
John Doe,30,Engineering,75000
Jane Smith,25,Marketing,65000
Bob Johnson,35,Sales,80000
Alice Brown,28,Engineering,72000";
    
    await File.WriteAllTextAsync(testCsvPath, csvContent);
    Console.WriteLine("   Created test CSV file");
    
    var pipeline = PipeFlowBuilder
      .FromCsv(testCsvPath)
      .Filter(row => row["Department"]?.ToString() == "Engineering")
      .Map(row => new 
      { 
        Name = row["Name"], 
        Salary = Convert.ToDecimal(row["Salary"])
      })
      .Build();
    
    var result = await pipeline.ExecuteAsync();
    Console.WriteLine($"   Pipeline executed: {result.Success}");
    Console.WriteLine($"   Records processed: {result.ProcessedCount}");
    
    var engineers = result.Data.ToList();
    if (engineers.Count == 2)
    {
      Console.WriteLine($"   Correct filter count: {engineers.Count} engineers");
    }
    else
    {
      throw new Exception($"Expected 2 engineers, got {engineers.Count}");
    }
    
    File.Delete(testCsvPath);
  }
  
  static async Task TestAsyncPipelineExecution()
  {
    Console.WriteLine("\nTest 2: Async Pipeline with CancellationToken");
    
    var cts = new CancellationTokenSource();
    
    var largeData = Enumerable.Range(1, 10000)
      .Select(i => new DataRow 
      { 
        ["Id"] = i, 
        ["Value"] = i * 2,
        ["Category"] = i % 3 == 0 ? "A" : "B"
      });
    
    var pipeline = PipeFlowBuilder
      .FromCollection(largeData)
      .Filter(row => row["Category"].ToString() == "A")
      .Take(100)
      .Build();
    
    var startTime = DateTime.Now;
    var result = await pipeline.ExecuteAsync(cts.Token);
    var elapsed = DateTime.Now - startTime;
    
    Console.WriteLine($"   Async execution completed in {elapsed.TotalMilliseconds}ms");
    Console.WriteLine($"   Processed {result.ProcessedCount} records");
    
    if (result.ProcessedCount != 100)
    {
      throw new Exception($"Expected 100 records, got {result.ProcessedCount}");
    }
  }
  
  static async Task TestFilterAndMap()
  {
    Console.WriteLine("\nTest 3: Complex Filter and Map Operations");
    
    var testData = new List<DataRow>
    {
      new DataRow { ["Product"] = "Laptop", ["Price"] = 1200, ["Stock"] = 5 },
      new DataRow { ["Product"] = "Mouse", ["Price"] = 25, ["Stock"] = 50 },
      new DataRow { ["Product"] = "Keyboard", ["Price"] = 75, ["Stock"] = 0 },
      new DataRow { ["Product"] = "Monitor", ["Price"] = 300, ["Stock"] = 10 }
    };
    
    var pipeline = PipeFlowBuilder
      .FromCollection(testData)
      .Filter(row => Convert.ToInt32(row["Stock"]) > 0)
      .Filter(row => Convert.ToDecimal(row["Price"]) >= 50)
      .Map(row => new
      {
        Item = row["Product"].ToString().ToUpper(),
        TotalValue = Convert.ToDecimal(row["Price"]) * Convert.ToInt32(row["Stock"])
      })
      .OrderByDescending(x => x.TotalValue)
      .Build();
    
    var result = await pipeline.ExecuteAsync();
    var items = result.Data.ToList();
    
    Console.WriteLine($"   Filtered to {items.Count} items");
    
    if (items.Count != 2)
    {
      throw new Exception($"Expected 2 items after filters, got {items.Count}");
    }
    
    if (items[0].Item != "LAPTOP")
    {
      throw new Exception($"Expected LAPTOP as first item, got {items[0].Item}");
    }
    
    Console.WriteLine($"   Correct ordering: {items[0].Item} (${items[0].TotalValue})");
  }
  
  static async Task TestErrorHandling()
  {
    Console.WriteLine("\nTest 4: Error Handling");
    
    try
    {
      var pipeline = PipeFlowBuilder
        .FromCollection<DataRow>(null)
        .Build();
      
      throw new Exception("Should have thrown ArgumentNullException");
    }
    catch (ArgumentNullException)
    {
      Console.WriteLine("   Correctly handled null collection");
    }
    
    var emptyPipeline = PipeFlowBuilder
      .FromCollection(new List<DataRow>())
      .Filter(row => true)
      .Build();
    
    var result = await emptyPipeline.ExecuteAsync();
    Console.WriteLine($"   Empty pipeline handled: Success={result.Success}, Count={result.ProcessedCount}");
  }
  
  static async Task TestComplexPipeline()
  {
    Console.WriteLine("\nTest 5: Complex Multi-Step Pipeline");
    
    var salesData = new List<DataRow>
    {
      new DataRow { ["Date"] = "2024-01-01", ["Product"] = "A", ["Quantity"] = 10, ["Price"] = 100 },
      new DataRow { ["Date"] = "2024-01-01", ["Product"] = "B", ["Quantity"] = 5, ["Price"] = 200 },
      new DataRow { ["Date"] = "2024-01-02", ["Product"] = "A", ["Quantity"] = 15, ["Price"] = 100 },
      new DataRow { ["Date"] = "2024-01-02", ["Product"] = "C", ["Quantity"] = 8, ["Price"] = 150 },
      new DataRow { ["Date"] = "2024-01-03", ["Product"] = "A", ["Quantity"] = 20, ["Price"] = 100 },
    };
    
    var pipeline = PipeFlowBuilder
      .FromCollection(salesData)
      .Map(row => new
      {
        Product = row["Product"].ToString(),
        Revenue = Convert.ToInt32(row["Quantity"]) * Convert.ToDecimal(row["Price"])
      })
      .Filter(x => x.Revenue > 500)
      .OrderByDescending(x => x.Revenue)
      .Take(3)
      .Build();
    
    var result = await pipeline.ExecuteAsync();
    var topSales = result.Data.ToList();
    
    Console.WriteLine($"   Pipeline processed {salesData.Count} -> {topSales.Count} records");
    
    foreach (var sale in topSales)
    {
      Console.WriteLine($"     - Product {sale.Product}: ${sale.Revenue}");
    }
    
    if (topSales.Count != 3)
    {
      throw new Exception($"Expected top 3 sales, got {topSales.Count}");
    }
  }
  
  static async Task TestStreamingMode()
  {
    Console.WriteLine("\nTest 6: Streaming Mode");
    
    var streamData = Enumerable.Range(1, 1000)
      .Select(i => new DataRow { ["Id"] = i, ["Value"] = i * i });
    
    var pipeline = PipeFlowBuilder
      .FromCollection(streamData)
      .Filter(row => Convert.ToInt32(row["Value"]) % 2 == 0)
      .Take(10)
      .Build();
    
    var streamedCount = 0;
    await foreach (var item in pipeline.StreamAsync())
    {
      streamedCount++;
      if (streamedCount == 1)
      {
        Console.WriteLine($"   First streamed item: Id={item["Id"]}, Value={item["Value"]}");
      }
    }
    
    Console.WriteLine($"   Streamed {streamedCount} items total");
    
    if (streamedCount != 10)
    {
      throw new Exception($"Expected 10 streamed items, got {streamedCount}");
    }
  }
}