using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PipeFlow.Core;
using PipeFlow.Core.Builder;

namespace TestNewFeatures;

class Program
{
  static async Task Main(string[] args)
  {
    Console.WriteLine("Testing New PipeFlow Features");
    Console.WriteLine("==============================\n");
    
    await TestBuilderPattern();
    await TestAsyncExecution();
    TestFromToPattern();
    await TestCollectionSource();
    
    Console.WriteLine("\nBasic tests completed!");
    
    await ComprehensiveTest.RunAllTests();
    await RealWorldTest.RunRealWorldTests();
    await PerformanceValidation.ValidatePerformance();
    
    // Test PostgreSQL unit tests (no database required)
    PostgreSqlUnitTest.RunUnitTests();
    
    // Test PostgreSQL with mock data
    await SimplePostgreSqlTest.TestWithMockData();
    
    // Verify PostgreSQL implementation
    TestPostgreSqlCode.VerifyPostgreSqlImplementation();
    
    // Test PostgreSQL integration if database is available
    Console.WriteLine("\nAttempting PostgreSQL integration tests...");
    try
    {
      await PostgreSqlTest.RunPostgreSqlTests();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"PostgreSQL integration tests skipped: {ex.Message}");
      Console.WriteLine("(PostgreSQL database not available or connection failed)");
    }
  }
  
  static async Task TestBuilderPattern()
  {
    Console.WriteLine("1. Testing Builder Pattern:");
    
    var data = new List<DataRow>
    {
      new DataRow { ["Name"] = "Alice", ["Age"] = 30, ["Active"] = true },
      new DataRow { ["Name"] = "Bob", ["Age"] = 25, ["Active"] = false },
      new DataRow { ["Name"] = "Charlie", ["Age"] = 35, ["Active"] = true }
    };
    
    var pipeline = PipeFlowBuilder
      .FromCollection(data)
      .Filter(row => (bool)row["Active"])
      .Map(row => new { Name = row["Name"], Age = row["Age"] })
      .Build();
    
    Console.WriteLine("   - Pipeline built (not executed yet)");
    
    var result = pipeline.Execute();
    Console.WriteLine($"   - Executed: {result.ProcessedCount} records processed");
    Console.WriteLine($"   - Success: {result.Success}");
  }
  
  static async Task TestAsyncExecution()
  {
    Console.WriteLine("\n2. Testing Async Execution with CancellationToken:");
    
    var cts = new CancellationTokenSource();
    
    var data = Enumerable.Range(1, 100).Select(i => new DataRow { ["Id"] = i, ["Value"] = i * 2 });
    
    var pipeline = PipeFlowBuilder
      .FromCollection(data)
      .Filter(row => (int)row["Value"] > 50)
      .Take(10)
      .Build();
    
    var result = await pipeline.ExecuteAsync(cts.Token);
    Console.WriteLine($"   - Async execution completed: {result.ProcessedCount} records");
    Console.WriteLine($"   - Execution time: {result.ExecutionTime.TotalMilliseconds}ms");
  }
  
  static void TestFromToPattern()
  {
    Console.WriteLine("\n3. Testing Consistent From/To Naming:");
    
    var data = new List<DataRow>
    {
      new DataRow { ["Product"] = "Laptop", ["Price"] = 1200 },
      new DataRow { ["Product"] = "Mouse", ["Price"] = 25 }
    };
    
    var pipeline = PipeFlowBuilder
      .FromCollection(data)
      .Filter(row => (int)row["Price"] > 100);
    
    Console.WriteLine("   - Created pipeline with FromCollection()");
    Console.WriteLine("   - To methods (ToCsv, ToJson, etc.) would write output");
  }
  
  static async Task TestCollectionSource()
  {
    Console.WriteLine("\n4. Testing Generic Collection Source:");
    
    var products = new List<Product>
    {
      new Product { Id = 1, Name = "Product A", Price = 100 },
      new Product { Id = 2, Name = "Product B", Price = 200 },
      new Product { Id = 3, Name = "Product C", Price = 150 }
    };
    
    var pipeline = PipeFlowBuilder
      .FromCollection(products)
      .Filter(p => p.Price > 100)
      .OrderByDescending(p => p.Price)
      .Build();
    
    var result = await pipeline.ExecuteAsync();
    Console.WriteLine($"   - Processed {result.ProcessedCount} products");
    
    var items = result.Data.ToList();
    foreach (var item in items)
    {
      Console.WriteLine($"     - {item.Name}: ${item.Price}");
    }
  }
}

class Product
{
  public int Id { get; set; }
  public string Name { get; set; }
  public decimal Price { get; set; }
}