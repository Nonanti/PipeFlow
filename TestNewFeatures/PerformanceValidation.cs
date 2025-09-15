using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PipeFlow.Core;
using PipeFlow.Core.Builder;

namespace TestNewFeatures;

public class PerformanceValidation
{
  public static async Task ValidatePerformance()
  {
    Console.WriteLine("\nPERFORMANCE VALIDATION");
    Console.WriteLine("=========================\n");
    
    await TestMemoryEfficiency();
    await TestCancellationToken();
    await TestBuildVsExecutePerformance();
    
    Console.WriteLine("\nPerformance validation completed!");
  }
  
  static async Task TestMemoryEfficiency()
  {
    Console.WriteLine("Memory Efficiency Test");
    
    var initialMemory = GC.GetTotalMemory(true);
    
    var largeData = GenerateLargeDataset(100000);
    
    var pipeline = PipeFlowBuilder
      .FromCollection(largeData)
      .Filter(row => Convert.ToInt32(row["Value"]) % 100 == 0)
      .Take(10)
      .Build();
    
    // Stream processing - should use minimal memory
    var streamedItems = new List<DataRow>();
    await foreach (var item in pipeline.StreamAsync())
    {
      streamedItems.Add(item);
    }
    
    var memoryAfterStreaming = GC.GetTotalMemory(true);
    var memoryUsedKB = (memoryAfterStreaming - initialMemory) / 1024;
    
    Console.WriteLine($"   Processed 100,000 items stream");
    Console.WriteLine($"   Retrieved {streamedItems.Count} filtered items");
    Console.WriteLine($"   Memory used: ~{memoryUsedKB:N0} KB");
    
    if (memoryUsedKB > 10000) // Alert if using more than 10MB
    {
      Console.WriteLine("   Warning: High memory usage detected");
    }
  }
  
  static async Task TestCancellationToken()
  {
    Console.WriteLine("\nCancellationToken Test");
    
    var cts = new CancellationTokenSource();
    var data = GenerateLargeDataset(1000000); // 1 million records
    
    var pipeline = PipeFlowBuilder
      .FromCollection(data)
      .Filter(row => true)
      .Build();
    
    var processedCount = 0;
    var sw = Stopwatch.StartNew();
    
    // Cancel after 100ms
    _ = Task.Run(async () =>
    {
      await Task.Delay(100);
      cts.Cancel();
    });
    
    try
    {
      await foreach (var item in pipeline.StreamAsync(cts.Token))
      {
        processedCount++;
        if (processedCount % 10000 == 0)
        {
          Console.WriteLine($"     Processing... {processedCount:N0} items");
        }
      }
    }
    catch (OperationCanceledException)
    {
      Console.WriteLine($"   Operation cancelled after {sw.ElapsedMilliseconds}ms");
      Console.WriteLine($"   Processed {processedCount:N0} items before cancellation");
    }
  }
  
  static async Task TestBuildVsExecutePerformance()
  {
    Console.WriteLine("\nBuild vs Execute Performance");
    
    var data = GenerateLargeDataset(10000);
    var sw = new Stopwatch();
    
    sw.Start();
    var pipeline = PipeFlowBuilder
      .FromCollection(data)
      .Filter(row => Convert.ToInt32(row["Value"]) > 5000)
      .Map(row => new
      {
        Id = row["Id"],
        DoubledValue = Convert.ToInt32(row["Value"]) * 2
      })
      .OrderByDescending(x => x.DoubledValue)
      .Take(100)
      .Build();
    sw.Stop();
    
    var buildTime = sw.ElapsedMilliseconds;
    Console.WriteLine($"   Pipeline build time: {buildTime}ms (should be ~0ms)");
    
    sw.Restart();
    var result1 = await pipeline.ExecuteAsync();
    sw.Stop();
    var firstExecutionTime = sw.ElapsedMilliseconds;
    Console.WriteLine($"   First execution: {firstExecutionTime}ms ({result1.ProcessedCount} records)");
    
    sw.Restart();
    var result2 = await pipeline.ExecuteAsync();
    sw.Stop();
    var secondExecutionTime = sw.ElapsedMilliseconds;
    Console.WriteLine($"   Second execution: {secondExecutionTime}ms ({result2.ProcessedCount} records)");
    
    if (buildTime > 10)
    {
      Console.WriteLine("   Warning: Build took longer than expected");
    }
    
    if (Math.Abs(firstExecutionTime - secondExecutionTime) > firstExecutionTime * 0.5)
    {
      Console.WriteLine("   Warning: Execution times vary significantly");
    }
  }
  
  static IEnumerable<DataRow> GenerateLargeDataset(int count)
  {
    var random = new Random(42);
    for (int i = 0; i < count; i++)
    {
      yield return new DataRow
      {
        ["Id"] = i,
        ["Value"] = random.Next(1, 10000),
        ["Category"] = $"Cat{random.Next(1, 10)}",
        ["Description"] = $"Description for item {i}"
      };
    }
  }
}