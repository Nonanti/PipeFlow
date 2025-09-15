using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PipeFlow.Core.Api;
using PipeFlow.Core.Excel;
using PipeFlow.Core.Json;
using PipeFlow.Core.MongoDB;
using PipeFlow.Core.Sql;

namespace PipeFlow.Core.Builder;

public static class PipelineDestinationExtensions
{
  public static IPipelineBuilder<DataRow> ToCsv(this IPipelineBuilder<DataRow> builder, string filePath, Action<CsvWriterOptions> configure = null)
  {
    var options = new CsvWriterOptions();
    configure?.Invoke(options);
    
    var pipeline = builder.Build();
    pipeline.ForEach(row =>
    {
      var writer = new CsvWriter(filePath);
      writer.Write(new[] { row });
    });
    
    return builder;
  }
  
  public static async Task<IPipelineBuilder<DataRow>> ToCsvAsync(
    this IPipelineBuilder<DataRow> builder, 
    string filePath, 
    Action<CsvWriterOptions> configure = null,
    CancellationToken cancellationToken = default)
  {
    var options = new CsvWriterOptions();
    configure?.Invoke(options);
    
    var pipeline = builder.Build();
    var writer = new CsvWriter(filePath);
    
    await pipeline.ForEachAsync(async row =>
    {
      await Task.Run(() => writer.Write(new[] { row }), cancellationToken);
    }, cancellationToken);
    
    return builder;
  }
  
  public static IPipelineBuilder<DataRow> ToJson(this IPipelineBuilder<DataRow> builder, string filePath, Action<JsonWriterOptions> configure = null)
  {
    var options = new JsonWriterOptions();
    configure?.Invoke(options);
    
    var pipeline = builder.Build();
    var data = pipeline.ToList();
    var writer = new JsonWriter(filePath);
    writer.Write(data);
    
    return builder;
  }
  
  public static async Task<IPipelineBuilder<DataRow>> ToJsonAsync(
    this IPipelineBuilder<DataRow> builder, 
    string filePath, 
    Action<JsonWriterOptions> configure = null,
    CancellationToken cancellationToken = default)
  {
    var options = new JsonWriterOptions();
    configure?.Invoke(options);
    
    var pipeline = builder.Build();
    var data = await pipeline.ToListAsync(cancellationToken);
    var writer = new JsonWriter(filePath);
    await Task.Run(() => writer.Write(data), cancellationToken);
    
    return builder;
  }
  
  public static IPipelineBuilder<DataRow> ToExcel(this IPipelineBuilder<DataRow> builder, string filePath, Action<ExcelWriterOptions> configure = null)
  {
    var options = new ExcelWriterOptions();
    configure?.Invoke(options);
    
    var pipeline = builder.Build();
    var data = pipeline.ToList();
    var writer = new ExcelWriter(filePath);
    writer.Write(data);
    
    return builder;
  }
  
  public static async Task<IPipelineBuilder<DataRow>> ToExcelAsync(
    this IPipelineBuilder<DataRow> builder, 
    string filePath, 
    Action<ExcelWriterOptions> configure = null,
    CancellationToken cancellationToken = default)
  {
    var options = new ExcelWriterOptions();
    configure?.Invoke(options);
    
    var pipeline = builder.Build();
    var data = await pipeline.ToListAsync(cancellationToken);
    var writer = new ExcelWriter(filePath);
    await Task.Run(() => writer.Write(data), cancellationToken);
    
    return builder;
  }
  
  public static IPipelineBuilder<DataRow> ToSql(
    this IPipelineBuilder<DataRow> builder, 
    string connectionString, 
    string tableName, 
    Action<SqlWriterOptions> configure = null)
  {
    var options = new SqlWriterOptions();
    configure?.Invoke(options);
    
    var pipeline = builder.Build();
    var writer = new SqlWriter(connectionString, tableName);
    
    if (options.UseBulkInsert)
    {
      writer.BulkWrite(pipeline.Stream());
    }
    else
    {
      writer.Write(pipeline.Stream());
    }
    
    return builder;
  }
  
  public static async Task<IPipelineBuilder<DataRow>> ToSqlAsync(
    this IPipelineBuilder<DataRow> builder, 
    string connectionString, 
    string tableName, 
    Action<SqlWriterOptions> configure = null,
    CancellationToken cancellationToken = default)
  {
    var options = new SqlWriterOptions();
    configure?.Invoke(options);
    
    var pipeline = builder.Build();
    var writer = new SqlWriter(connectionString, tableName);
    
    var data = await pipeline.ToListAsync(cancellationToken);
    
    await Task.Run(() =>
    {
      if (options.UseBulkInsert)
      {
        writer.BulkWrite(data);
      }
      else
      {
        writer.Write(data);
      }
    }, cancellationToken);
    
    return builder;
  }
  
  public static IPipelineBuilder<DataRow> ToApi(
    this IPipelineBuilder<DataRow> builder, 
    string endpoint, 
    Action<ApiWriterOptions> configure = null)
  {
    var options = new ApiWriterOptions();
    configure?.Invoke(options);
    
    var pipeline = builder.Build();
    var writer = new ApiWriter(endpoint);
    
    if (options.BatchSize > 0)
    {
      var batch = new List<DataRow>();
      pipeline.ForEach(row =>
      {
        batch.Add(row);
        if (batch.Count >= options.BatchSize)
        {
          writer.Write(batch);
          batch.Clear();
        }
      });
      
      if (batch.Any())
      {
        writer.Write(batch);
      }
    }
    else
    {
      writer.Write(pipeline.Stream());
    }
    
    return builder;
  }
  
  public static async Task<IPipelineBuilder<DataRow>> ToApiAsync(
    this IPipelineBuilder<DataRow> builder, 
    string endpoint, 
    Action<ApiWriterOptions> configure = null,
    CancellationToken cancellationToken = default)
  {
    var options = new ApiWriterOptions();
    configure?.Invoke(options);
    
    var pipeline = builder.Build();
    var writer = new ApiWriter(endpoint);
    
    if (options.BatchSize > 0)
    {
      var batch = new List<DataRow>();
      await pipeline.ForEachAsync(async row =>
      {
        batch.Add(row);
        if (batch.Count >= options.BatchSize)
        {
          await Task.Run(() => writer.Write(batch), cancellationToken);
          batch.Clear();
        }
      }, cancellationToken);
      
      if (batch.Any())
      {
        await Task.Run(() => writer.Write(batch), cancellationToken);
      }
    }
    else
    {
      var data = await pipeline.ToListAsync(cancellationToken);
      await Task.Run(() => writer.Write(data), cancellationToken);
    }
    
    return builder;
  }
  
  public static IPipelineBuilder<DataRow> ToMongoDB(
    this IPipelineBuilder<DataRow> builder, 
    string connectionString, 
    string database, 
    string collection,
    Action<MongoWriterOptions> configure = null)
  {
    var options = new MongoWriterOptions();
    configure?.Invoke(options);
    
    var pipeline = builder.Build();
    var writer = new MongoWriter(connectionString, database, collection);
    writer.Write(pipeline.Stream());
    
    return builder;
  }
  
  public static async Task<IPipelineBuilder<DataRow>> ToMongoDBAsync(
    this IPipelineBuilder<DataRow> builder, 
    string connectionString, 
    string database, 
    string collection,
    Action<MongoWriterOptions> configure = null,
    CancellationToken cancellationToken = default)
  {
    var options = new MongoWriterOptions();
    configure?.Invoke(options);
    
    var pipeline = builder.Build();
    var data = await pipeline.ToListAsync(cancellationToken);
    var writer = new MongoWriter(connectionString, database, collection);
    await Task.Run(() => writer.Write(data), cancellationToken);
    
    return builder;
  }
  
  public static async Task<IPipelineBuilder<T>> ToEntityFrameworkAsync<T>(
    this IPipelineBuilder<T> builder,
    DbContext context,
    Action<EntityFrameworkWriterOptions<T>> configure = null,
    CancellationToken cancellationToken = default) where T : class
  {
    var options = new EntityFrameworkWriterOptions<T>();
    configure?.Invoke(options);
    
    var pipeline = builder.Build();
    var data = await pipeline.ToListAsync(cancellationToken);
    
    if (options.UseTransaction)
    {
      using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
      try
      {
        await ProcessEntityFrameworkData(context, data, options, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
      }
      catch
      {
        await transaction.RollbackAsync(cancellationToken);
        throw;
      }
    }
    else
    {
      await ProcessEntityFrameworkData(context, data, options, cancellationToken);
    }
    
    return builder;
  }
  
  private static async Task ProcessEntityFrameworkData<T>(
    DbContext context, 
    List<T> data, 
    EntityFrameworkWriterOptions<T> options,
    CancellationToken cancellationToken) where T : class
  {
    var dbSet = context.Set<T>();
    var processedCount = 0;
    
    foreach (var item in data)
    {
      if (options.UpsertPredicate != null)
      {
        var existing = await dbSet.FirstOrDefaultAsync(options.UpsertPredicate(item), cancellationToken);
        if (existing != null)
        {
          context.Entry(existing).CurrentValues.SetValues(item);
        }
        else
        {
          await dbSet.AddAsync(item, cancellationToken);
        }
      }
      else
      {
        await dbSet.AddAsync(item, cancellationToken);
      }
      
      processedCount++;
      
      if (processedCount % options.BatchSize == 0)
      {
        await context.SaveChangesAsync(cancellationToken);
      }
    }
    
    if (processedCount % options.BatchSize != 0)
    {
      await context.SaveChangesAsync(cancellationToken);
    }
  }
}

public class CsvWriterOptions
{
  public char Delimiter { get; set; } = ',';
  public bool WriteHeaders { get; set; } = true;
  public string Encoding { get; set; } = "UTF-8";
}

public class JsonWriterOptions
{
  public bool Indented { get; set; } = true;
  public bool CamelCase { get; set; } = false;
}

public class ExcelWriterOptions
{
  public string SheetName { get; set; } = "Sheet1";
  public bool AutoFitColumns { get; set; } = true;
}

public class SqlWriterOptions
{
  public bool UseBulkInsert { get; set; } = true;
  public int BatchSize { get; set; } = 1000;
  public int CommandTimeout { get; set; } = 30;
}

public class ApiWriterOptions
{
  public string AuthToken { get; set; }
  public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
  public int BatchSize { get; set; } = 100;
  public int Timeout { get; set; } = 30000;
}

public class MongoWriterOptions
{
  public string UpsertKey { get; set; }
  public int BatchSize { get; set; } = 500;
}

public class EntityFrameworkWriterOptions<T> where T : class
{
  public Func<T, System.Linq.Expressions.Expression<Func<T, bool>>> UpsertPredicate { get; set; }
  public int BatchSize { get; set; } = 100;
  public bool UseTransaction { get; set; } = true;
}