using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PipeFlow.Core;
using PipeFlow.Core.PostgreSQL;

namespace TestNewFeatures;

public class TestPostgreSqlCode
{
  public static void VerifyPostgreSqlImplementation()
  {
    Console.WriteLine("\nVERIFYING POSTGRESQL IMPLEMENTATION");
    Console.WriteLine("====================================\n");
    
    // Test 1: Check PostgreSqlReader class
    Console.WriteLine("1. PostgreSqlReader Class Verification:");
    try
    {
      var readerType = typeof(PostgreSqlReader);
      Console.WriteLine($"   - Class exists: {readerType.Name}");
      
      var methods = readerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Where(m => m.DeclaringType == readerType)
        .Select(m => m.Name)
        .Distinct()
        .OrderBy(m => m);
      
      Console.WriteLine($"   - Public methods: {string.Join(", ", methods)}");
      
      // Check specific methods
      var hasRead = readerType.GetMethod("Read") != null;
      var hasReadAsync = readerType.GetMethod("ReadAsync") != null;
      var hasQuery = readerType.GetMethod("Query", new[] { typeof(string) }) != null;
      
      Console.WriteLine($"   - Has Read(): {hasRead}");
      Console.WriteLine($"   - Has ReadAsync(): {hasReadAsync}");
      Console.WriteLine($"   - Has Query(): {hasQuery}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"   ERROR: {ex.Message}");
    }
    
    // Test 2: Check PostgreSqlWriter class
    Console.WriteLine("\n2. PostgreSqlWriter Class Verification:");
    try
    {
      var writerType = typeof(PostgreSqlWriter);
      Console.WriteLine($"   - Class exists: {writerType.Name}");
      
      var methods = writerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Where(m => m.DeclaringType == writerType)
        .Select(m => m.Name)
        .Distinct()
        .OrderBy(m => m);
      
      Console.WriteLine($"   - Public methods: {string.Join(", ", methods)}");
      
      // Check specific methods
      var hasWrite = writerType.GetMethod("Write") != null;
      var hasWriteAsync = writerType.GetMethod("WriteAsync") != null;
      var hasBulkWrite = writerType.GetMethod("BulkWrite") != null;
      var hasOnConflict = writerType.GetMethod("OnConflictUpdate") != null;
      
      Console.WriteLine($"   - Has Write(): {hasWrite}");
      Console.WriteLine($"   - Has WriteAsync(): {hasWriteAsync}");
      Console.WriteLine($"   - Has BulkWrite(): {hasBulkWrite}");
      Console.WriteLine($"   - Has OnConflictUpdate(): {hasOnConflict}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"   ERROR: {ex.Message}");
    }
    
    // Test 3: Check PipeFlow integration
    Console.WriteLine("\n3. PipeFlow Integration Verification:");
    try
    {
      var pipeflowType = typeof(PipeFlow.Core.PipeFlow);
      var builderType = pipeflowType.GetNestedType("PipeFlowBuilder");
      
      if (builderType != null)
      {
        var hasPostgreSql = builderType.GetMethod("PostgreSql", new[] { typeof(string), typeof(string) }) != null;
        Console.WriteLine($"   - PipeFlow.From.PostgreSql(): {hasPostgreSql}");
      }
      
      // Check extension methods
      var extensionTypes = Assembly.GetAssembly(typeof(PipeFlow.Core.PipeFlow))
        .GetTypes()
        .Where(t => t.Name.Contains("Extension"))
        .ToList();
      
      Console.WriteLine($"   - Extension classes found: {extensionTypes.Count}");
      
      foreach (var extType in extensionTypes.Where(t => t.Name.Contains("PostgreSql")))
      {
        Console.WriteLine($"   - Found: {extType.Name}");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"   ERROR: {ex.Message}");
    }
    
    // Test 4: Test actual instantiation
    Console.WriteLine("\n4. Object Instantiation Test:");
    try
    {
      var testConnection = "Host=test;Database=test;Username=test;Password=test";
      
      // Test Reader
      var reader = new PostgreSqlReader(testConnection);
      reader.Query("SELECT 1").WithTimeout(30);
      Console.WriteLine("   - PostgreSqlReader instantiated successfully");
      
      // Test Writer
      var writer = new PostgreSqlWriter(testConnection, "test_table");
      writer.WithBatchSize(500).CreateTableIfNotExists();
      Console.WriteLine("   - PostgreSqlWriter instantiated successfully");
      
      // Test DataRow compatibility
      var testData = new DataRow { ["id"] = 1, ["name"] = "test" };
      Console.WriteLine($"   - DataRow created with {testData.GetColumnNames().Count()} columns");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"   ERROR: {ex.Message}");
    }
    
    Console.WriteLine("\nPostgreSQL implementation verification completed!");
  }
}