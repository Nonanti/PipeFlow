using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PipeFlow.Core;
using PipeFlow.Core.Builder;
using PipeFlow.Core.PostgreSQL;

namespace TestNewFeatures;

public class PostgreSqlTest
{
  private const string ConnectionString = "Host=localhost;Database=testdb;Username=postgres;Password=postgres";
  
  public static async Task RunPostgreSqlTests()
  {
    Console.WriteLine("\nPOSTGRESQL SUPPORT TESTS");
    Console.WriteLine("========================\n");
    
    try
    {
      TestBasicConnection();
      await TestReadOperations();
      await TestWriteOperations();
      await TestBulkOperations();
      await TestBuilderPattern();
      await TestUpsertOperations();
      
      Console.WriteLine("\nAll PostgreSQL tests completed successfully!");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"\nPostgreSQL test failed: {ex.Message}");
      Console.WriteLine("Make sure PostgreSQL is running and connection string is correct.");
    }
  }
  
  static void TestBasicConnection()
  {
    Console.WriteLine("1. Testing Basic PostgreSQL Connection:");
    
    try
    {
      var reader = new PostgreSqlReader(ConnectionString);
      reader.Query("SELECT 1 as test");
      var result = reader.ExecuteScalar<int>();
      Console.WriteLine($"   Connection test: {(result == 1 ? "SUCCESS" : "FAILED")}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"   Connection failed: {ex.Message}");
      throw;
    }
  }
  
  static async Task TestReadOperations()
  {
    Console.WriteLine("\n2. Testing Read Operations:");
    
    var reader = new PostgreSqlReader(ConnectionString);
    
    reader.Query(@"
      SELECT 
        generate_series(1, 10) as id,
        'User' || generate_series(1, 10) as name,
        (random() * 100)::int as age,
        CASE WHEN random() > 0.5 THEN true ELSE false END as is_active
    ");
    
    var count = 0;
    await foreach (var row in reader.ReadAsync())
    {
      count++;
      if (count == 1)
      {
        Console.WriteLine($"   First row: ID={row["id"]}, Name={row["name"]}, Age={row["age"]}");
      }
    }
    
    Console.WriteLine($"   Total rows read: {count}");
  }
  
  static async Task TestWriteOperations()
  {
    Console.WriteLine("\n3. Testing Write Operations:");
    
    var testData = new List<DataRow>();
    for (int i = 1; i <= 5; i++)
    {
      var row = new DataRow
      {
        ["id"] = i,
        ["name"] = $"Product {i}",
        ["price"] = i * 10.5m,
        ["created_at"] = DateTime.Now
      };
      testData.Add(row);
    }
    
    var writer = new PostgreSqlWriter(ConnectionString, "test_products");
    writer.CreateTableIfNotExists()
          .WithBatchSize(100);
    
    await writer.TruncateAsync();
    await writer.WriteAsync(testData);
    
    Console.WriteLine($"   Wrote {testData.Count} records to test_products table");
    
    var reader = new PostgreSqlReader(ConnectionString)
      .Query("SELECT COUNT(*) FROM test_products");
    var count = reader.ExecuteScalar<int>();
    Console.WriteLine($"   Verified: {count} records in database");
  }
  
  static async Task TestBulkOperations()
  {
    Console.WriteLine("\n4. Testing Bulk Operations:");
    
    var bulkData = new List<DataRow>();
    for (int i = 1; i <= 1000; i++)
    {
      var row = new DataRow
      {
        ["id"] = i,
        ["value"] = $"Value_{i}",
        ["random"] = new Random().Next(1, 100)
      };
      bulkData.Add(row);
    }
    
    var writer = new PostgreSqlWriter(ConnectionString, "bulk_test");
    writer.CreateTableIfNotExists();
    
    await writer.TruncateAsync();
    
    var startTime = DateTime.Now;
    writer.BulkWrite(bulkData);
    var elapsed = DateTime.Now - startTime;
    
    Console.WriteLine($"   Bulk inserted {bulkData.Count} records in {elapsed.TotalMilliseconds}ms");
    
    var reader = new PostgreSqlReader(ConnectionString)
      .Query("SELECT COUNT(*) FROM bulk_test");
    var count = reader.ExecuteScalar<int>();
    Console.WriteLine($"   Verified: {count} records in database");
  }
  
  static async Task TestBuilderPattern()
  {
    Console.WriteLine("\n5. Testing Builder Pattern with PostgreSQL:");
    
    var pipeline = PipeFlowBuilder
      .FromPostgreSql(ConnectionString, @"
        SELECT 
          generate_series(1, 100) as id,
          'Item' || generate_series(1, 100) as name,
          (random() * 1000)::decimal as price
      ")
      .Filter(row => Convert.ToDecimal(row["price"]) > 500)
      .Map(row => new
      {
        Id = row["id"],
        Name = row["name"],
        Price = Convert.ToDecimal(row["price"])
      })
      .Take(10)
      .Build();
    
    var result = await pipeline.ExecuteAsync();
    Console.WriteLine($"   Pipeline processed: {result.ProcessedCount} records");
    Console.WriteLine($"   Execution time: {result.ExecutionTime.TotalMilliseconds}ms");
  }
  
  static async Task TestUpsertOperations()
  {
    Console.WriteLine("\n6. Testing Upsert Operations:");
    
    var initialData = new List<DataRow>
    {
      new DataRow { ["id"] = 1, ["name"] = "Item1", ["quantity"] = 10 },
      new DataRow { ["id"] = 2, ["name"] = "Item2", ["quantity"] = 20 },
      new DataRow { ["id"] = 3, ["name"] = "Item3", ["quantity"] = 30 }
    };
    
    var writer = new PostgreSqlWriter(ConnectionString, "upsert_test");
    writer.CreateTableIfNotExists();
    await writer.TruncateAsync();
    
    await writer.WriteAsync(initialData);
    Console.WriteLine("   Inserted 3 initial records");
    
    var updateData = new List<DataRow>
    {
      new DataRow { ["id"] = 2, ["name"] = "Item2_Updated", ["quantity"] = 25 },
      new DataRow { ["id"] = 4, ["name"] = "Item4", ["quantity"] = 40 }
    };
    
    writer.OnConflictUpdate("id");
    await writer.WriteAsync(updateData);
    Console.WriteLine("   Performed upsert with 2 records");
    
    var reader = new PostgreSqlReader(ConnectionString)
      .Query("SELECT * FROM upsert_test ORDER BY id");
    
    var results = new List<string>();
    foreach (var row in reader.Read())
    {
      results.Add($"ID={row["id"]}, Name={row["name"]}, Qty={row["quantity"]}");
    }
    
    Console.WriteLine($"   Final records: {results.Count}");
    foreach (var result in results)
    {
      Console.WriteLine($"     {result}");
    }
  }
}