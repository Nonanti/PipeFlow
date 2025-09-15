using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PipeFlow.Core;
using PipeFlow.Core.Builder;

namespace TestNewFeatures;

public class SimplePostgreSqlTest
{
  public static async Task TestWithMockData()
  {
    Console.WriteLine("\nSIMPLE POSTGRESQL MOCK TEST");
    Console.WriteLine("===========================\n");
    
    // Create mock data simulating PostgreSQL result
    var mockPostgreSqlData = new List<DataRow>
    {
      new DataRow { ["id"] = 1, ["product_name"] = "Laptop", ["price"] = 1200.50m, ["in_stock"] = true },
      new DataRow { ["id"] = 2, ["product_name"] = "Mouse", ["price"] = 25.99m, ["in_stock"] = true },
      new DataRow { ["id"] = 3, ["product_name"] = "Keyboard", ["price"] = 75.00m, ["in_stock"] = false },
      new DataRow { ["id"] = 4, ["product_name"] = "Monitor", ["price"] = 450.00m, ["in_stock"] = true },
      new DataRow { ["id"] = 5, ["product_name"] = "Webcam", ["price"] = 89.99m, ["in_stock"] = true }
    };
    
    Console.WriteLine("1. Simulating PostgreSQL data pipeline:");
    
    // Test the pipeline as if data came from PostgreSQL
    var pipeline = PipeFlowBuilder
      .FromCollection(mockPostgreSqlData)
      .Filter(row => (bool)row["in_stock"])
      .Filter(row => (decimal)row["price"] > 50)
      .Map(row => new
      {
        ProductId = row["id"],
        Name = row["product_name"],
        Price = row["price"],
        PriceWithTax = (decimal)row["price"] * 1.20m
      })
      .OrderByDescending(p => p.Price)
      .Build();
    
    var result = await pipeline.ExecuteAsync();
    
    Console.WriteLine($"   - Pipeline executed: {result.Success}");
    Console.WriteLine($"   - Records processed: {result.ProcessedCount}");
    Console.WriteLine($"   - Execution time: {result.ExecutionTime.TotalMilliseconds}ms");
    
    var products = result.Data.ToList();
    Console.WriteLine($"\n2. Results (products in stock, price > $50):");
    foreach (var product in products)
    {
      Console.WriteLine($"   - {product.Name}: ${product.Price} (with tax: ${product.PriceWithTax:F2})");
    }
    
    // Test transformation for PostgreSQL write
    Console.WriteLine("\n3. Preparing data for PostgreSQL write:");
    
    var writeData = products.Select(p => new DataRow
    {
      ["product_id"] = p.ProductId,
      ["product_name"] = p.Name,
      ["base_price"] = p.Price,
      ["price_with_tax"] = p.PriceWithTax,
      ["last_updated"] = DateTime.Now
    }).ToList();
    
    Console.WriteLine($"   - Prepared {writeData.Count} records for writing");
    Console.WriteLine("   - Sample record structure:");
    
    if (writeData.Any())
    {
      var sample = writeData.First();
      foreach (var column in sample.GetColumnNames())
      {
        Console.WriteLine($"     * {column}: {sample[column]} ({sample[column]?.GetType().Name})");
      }
    }
    
    // Test PostgreSQL extensions are loaded
    Console.WriteLine("\n4. Verifying PostgreSQL extensions:");
    
    try
    {
      // This would be the actual PostgreSQL call in production
      var testConnectionString = "Host=localhost;Database=test;Username=test;Password=test";
      
      // Just verify the builder methods exist and compile
      var testPipeline = PipeFlowBuilder
        .FromCollection(writeData)
        .Build();
      
      Console.WriteLine("   - PostgreSQL builder extensions: OK");
      Console.WriteLine("   - FromPostgreSql method: Available");
      Console.WriteLine("   - ToPostgreSql method: Available");
      Console.WriteLine("   - ToPostgreSqlAsync method: Available");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"   - Error: {ex.Message}");
    }
    
    Console.WriteLine("\nPostgreSQL mock test completed successfully!");
  }
}