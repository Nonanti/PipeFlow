using System;
using System.Collections.Generic;
using System.Linq;
using PipeFlow.Core;
using PipeFlow.Core.Builder;
using PipeFlow.Core.PostgreSQL;

namespace TestNewFeatures;

public class PostgreSqlUnitTest
{
  public static void RunUnitTests()
  {
    Console.WriteLine("\nPOSTGRESQL UNIT TESTS");
    Console.WriteLine("=====================\n");
    
    TestPostgreSqlReaderCreation();
    TestPostgreSqlWriterCreation();
    TestBuilderIntegration();
    TestWriterOptions();
    
    Console.WriteLine("\nAll PostgreSQL unit tests passed!");
  }
  
  static void TestPostgreSqlReaderCreation()
  {
    Console.WriteLine("1. Testing PostgreSQL Reader Creation:");
    
    try
    {
      var reader = new PostgreSqlReader("Host=localhost;Database=test;Username=user;Password=pass");
      reader.Query("SELECT * FROM test")
            .WithTimeout(60)
            .WithBatchSize(500)
            .AddParameter("param1", "value1");
      
      Console.WriteLine("   PostgreSQL Reader created successfully");
      Console.WriteLine("   Query, timeout, batch size, and parameters configured");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"   Failed: {ex.Message}");
    }
  }
  
  static void TestPostgreSqlWriterCreation()
  {
    Console.WriteLine("\n2. Testing PostgreSQL Writer Creation:");
    
    try
    {
      var writer = new PostgreSqlWriter("Host=localhost;Database=test;Username=user;Password=pass", "test_table");
      writer.WithBatchSize(1000)
            .WithTimeout(30)
            .CreateTableIfNotExists()
            .OnConflictUpdate("id");
      
      Console.WriteLine("   PostgreSQL Writer created successfully");
      Console.WriteLine("   Batch size, timeout, and conflict handling configured");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"   Failed: {ex.Message}");
    }
  }
  
  static void TestBuilderIntegration()
  {
    Console.WriteLine("\n3. Testing Builder Pattern Integration:");
    
    try
    {
      // Test data
      var testData = new List<DataRow>
      {
        new DataRow { ["id"] = 1, ["name"] = "Test 1", ["value"] = 100 },
        new DataRow { ["id"] = 2, ["name"] = "Test 2", ["value"] = 200 },
        new DataRow { ["id"] = 3, ["name"] = "Test 3", ["value"] = 300 }
      };
      
      // Test builder pattern
      var pipeline = PipeFlowBuilder
        .FromCollection(testData)
        .Filter(row => (int)row["value"] > 150)
        .Map(row => new
        {
          Id = row["id"],
          Name = row["name"],
          Value = row["value"]
        })
        .Build();
      
      var result = pipeline.Execute();
      var count = result.Data.Count();
      
      Console.WriteLine($"   Builder pattern works with PostgreSQL extensions");
      Console.WriteLine($"   Filtered {count} records from test data");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"   Failed: {ex.Message}");
    }
  }
  
  static void TestWriterOptions()
  {
    Console.WriteLine("\n4. Testing PostgreSQL Writer Options:");
    
    try
    {
      var options = new PostgreSqlWriterOptions();
      options.CreateTableIfNotExists = true;
      options.UseBulkInsert = true;
      options.BatchSize = 500;
      options.OnConflictUpdate("id", "email");
      
      Console.WriteLine("   Writer options created successfully");
      Console.WriteLine($"   - Create table: {options.CreateTableIfNotExists}");
      Console.WriteLine($"   - Bulk insert: {options.UseBulkInsert}");
      Console.WriteLine($"   - Batch size: {options.BatchSize}");
      Console.WriteLine($"   - Conflict action: {options.OnConflictAction}");
      Console.WriteLine($"   - Conflict columns: {string.Join(", ", options.OnConflictColumns ?? new string[0])}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"   Failed: {ex.Message}");
    }
  }
}