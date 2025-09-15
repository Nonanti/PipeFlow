using System;
using System.Threading;
using System.Threading.Tasks;
using PipeFlow.Core.PostgreSQL;

namespace PipeFlow.Core.Builder;

public static class PostgreSqlExtensions
{
  public static IPipelineBuilder<DataRow> ToPostgreSql(
    this IPipelineBuilder<DataRow> builder,
    string connectionString,
    string tableName,
    Action<PostgreSqlWriterOptions> configure = null)
  {
    var options = new PostgreSqlWriterOptions();
    configure?.Invoke(options);
    
    var pipeline = builder.Build();
    var writer = new PostgreSqlWriter(connectionString, tableName);
    
    if (options.CreateTableIfNotExists)
      writer.CreateTableIfNotExists();
    
    if (options.OnConflictColumns != null)
    {
      if (options.OnConflictAction == PostgreSqlWriterOptions.ConflictAction.Update)
        writer.OnConflictUpdate(options.OnConflictColumns);
      else if (options.OnConflictAction == PostgreSqlWriterOptions.ConflictAction.DoNothing)
        writer.OnConflictDoNothing(options.OnConflictColumns);
    }
    
    writer.WithBatchSize(options.BatchSize);
    
    if (options.UseBulkInsert)
      writer.BulkWrite(pipeline.Stream());
    else
      writer.Write(pipeline.Stream());
    
    return builder;
  }
  
  public static async Task<IPipelineBuilder<DataRow>> ToPostgreSqlAsync(
    this IPipelineBuilder<DataRow> builder,
    string connectionString,
    string tableName,
    Action<PostgreSqlWriterOptions> configure = null,
    CancellationToken cancellationToken = default)
  {
    var options = new PostgreSqlWriterOptions();
    configure?.Invoke(options);
    
    var pipeline = builder.Build();
    var writer = new PostgreSqlWriter(connectionString, tableName);
    
    if (options.CreateTableIfNotExists)
      writer.CreateTableIfNotExists();
    
    if (options.OnConflictColumns != null)
    {
      if (options.OnConflictAction == PostgreSqlWriterOptions.ConflictAction.Update)
        writer.OnConflictUpdate(options.OnConflictColumns);
      else if (options.OnConflictAction == PostgreSqlWriterOptions.ConflictAction.DoNothing)
        writer.OnConflictDoNothing(options.OnConflictColumns);
    }
    
    writer.WithBatchSize(options.BatchSize);
    
    var data = await pipeline.ToListAsync(cancellationToken);
    
    if (options.UseBulkInsert)
    {
      await Task.Run(() => writer.BulkWrite(data), cancellationToken);
    }
    else
    {
      await writer.WriteAsync(data);
    }
    
    return builder;
  }
}

public class PostgreSqlWriterOptions
{
  public enum ConflictAction
  {
    None,
    DoNothing,
    Update
  }
  
  public bool CreateTableIfNotExists { get; set; } = false;
  public bool UseBulkInsert { get; set; } = false;
  public int BatchSize { get; set; } = 1000;
  public ConflictAction OnConflictAction { get; set; } = ConflictAction.None;
  public string[] OnConflictColumns { get; set; }
  
  public void OnConflictUpdate(params string[] columns)
  {
    OnConflictAction = ConflictAction.Update;
    OnConflictColumns = columns;
  }
  
  public void OnConflictDoNothing(params string[] columns)
  {
    OnConflictAction = ConflictAction.DoNothing;
    OnConflictColumns = columns;
  }
}