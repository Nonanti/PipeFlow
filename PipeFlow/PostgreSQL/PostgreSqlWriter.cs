using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

namespace PipeFlow.Core.PostgreSQL;

public class PostgreSqlWriter
{
  private readonly string _connectionString;
  private readonly string _tableName;
  private int _batchSize = 1000;
  private int _commandTimeout = 30;
  private bool _createTableIfNotExists = false;
  private ConflictAction _onConflict = ConflictAction.None;
  private string[] _conflictColumns;

  public enum ConflictAction
  {
    None,
    DoNothing,
    Update
  }

  public PostgreSqlWriter(string connectionString, string tableName)
  {
    if (string.IsNullOrWhiteSpace(connectionString))
      throw new ArgumentNullException(nameof(connectionString));
    
    if (string.IsNullOrWhiteSpace(tableName))
      throw new ArgumentNullException(nameof(tableName));
    
    _connectionString = connectionString;
    _tableName = tableName;
  }

  public PostgreSqlWriter WithBatchSize(int batchSize)
  {
    if (batchSize <= 0)
      throw new ArgumentException("Batch size must be greater than zero", nameof(batchSize));
    
    _batchSize = batchSize;
    return this;
  }

  public PostgreSqlWriter WithTimeout(int seconds)
  {
    if (seconds <= 0)
      throw new ArgumentException("Timeout must be greater than zero", nameof(seconds));
    
    _commandTimeout = seconds;
    return this;
  }

  public PostgreSqlWriter CreateTableIfNotExists(bool create = true)
  {
    _createTableIfNotExists = create;
    return this;
  }

  public PostgreSqlWriter OnConflictDoNothing(params string[] conflictColumns)
  {
    _onConflict = ConflictAction.DoNothing;
    _conflictColumns = conflictColumns;
    return this;
  }

  public PostgreSqlWriter OnConflictUpdate(params string[] conflictColumns)
  {
    _onConflict = ConflictAction.Update;
    _conflictColumns = conflictColumns;
    return this;
  }

  public void Write(IEnumerable<DataRow> rows)
  {
    using var connection = new NpgsqlConnection(_connectionString);
    connection.Open();
    
    var rowList = rows.ToList();
    if (!rowList.Any())
      return;
    
    if (_createTableIfNotExists)
    {
      CreateTableIfNotExists(connection, rowList.First());
    }
    
    var batches = rowList.Chunk(_batchSize);
    
    foreach (var batch in batches)
    {
      WriteBatch(connection, batch);
    }
  }

  public async Task WriteAsync(IEnumerable<DataRow> rows)
  {
    using var connection = new NpgsqlConnection(_connectionString);
    await connection.OpenAsync();
    
    var rowList = rows.ToList();
    if (!rowList.Any())
      return;
    
    if (_createTableIfNotExists)
    {
      await CreateTableIfNotExistsAsync(connection, rowList.First());
    }
    
    var batches = rowList.Chunk(_batchSize);
    
    foreach (var batch in batches)
    {
      await WriteBatchAsync(connection, batch);
    }
  }

  public void BulkWrite(IEnumerable<DataRow> rows)
  {
    using var connection = new NpgsqlConnection(_connectionString);
    connection.Open();
    
    var rowList = rows.ToList();
    if (!rowList.Any())
      return;
    
    var firstRow = rowList.First();
    var columns = firstRow.GetColumnNames().ToList();
    
    if (_createTableIfNotExists)
    {
      CreateTableIfNotExists(connection, firstRow);
    }
    
    using var writer = connection.BeginBinaryImport($"COPY {_tableName} ({string.Join(", ", columns)}) FROM STDIN (FORMAT BINARY)");
    
    foreach (var row in rowList)
    {
      writer.StartRow();
      
      foreach (var column in columns)
      {
        var value = row[column];
        
        if (value == null)
        {
          writer.WriteNull();
        }
        else
        {
          writer.Write(value);
        }
      }
    }
    
    writer.Complete();
  }

  private void WriteBatch(NpgsqlConnection connection, DataRow[] batch)
  {
    if (batch.Length == 0)
      return;
    
    var firstRow = batch[0];
    var columns = firstRow.GetColumnNames().ToList();
    
    var sql = BuildInsertStatement(columns);
    
    using var command = new NpgsqlCommand(sql, connection);
    command.CommandTimeout = _commandTimeout;
    
    foreach (var row in batch)
    {
      command.Parameters.Clear();
      
      foreach (var column in columns)
      {
        var paramName = "@" + column.Replace(" ", "_");
        var value = row[column] ?? DBNull.Value;
        command.Parameters.AddWithValue(paramName, value);
      }
      
      command.ExecuteNonQuery();
    }
  }

  private async Task WriteBatchAsync(NpgsqlConnection connection, DataRow[] batch)
  {
    if (batch.Length == 0)
      return;
    
    var firstRow = batch[0];
    var columns = firstRow.GetColumnNames().ToList();
    
    var sql = BuildInsertStatement(columns);
    
    using var command = new NpgsqlCommand(sql, connection);
    command.CommandTimeout = _commandTimeout;
    
    foreach (var row in batch)
    {
      command.Parameters.Clear();
      
      foreach (var column in columns)
      {
        var paramName = "@" + column.Replace(" ", "_");
        var value = row[column] ?? DBNull.Value;
        command.Parameters.AddWithValue(paramName, value);
      }
      
      await command.ExecuteNonQueryAsync();
    }
  }

  private string BuildInsertStatement(List<string> columns)
  {
    var columnList = string.Join(", ", columns);
    var paramList = string.Join(", ", columns.Select(c => "@" + c.Replace(" ", "_")));
    
    var sql = new StringBuilder();
    sql.Append($"INSERT INTO {_tableName} ({columnList}) VALUES ({paramList})");
    
    if (_onConflict != ConflictAction.None && _conflictColumns?.Length > 0)
    {
      var conflictList = string.Join(", ", _conflictColumns);
      sql.Append($" ON CONFLICT ({conflictList})");
      
      if (_onConflict == ConflictAction.DoNothing)
      {
        sql.Append(" DO NOTHING");
      }
      else if (_onConflict == ConflictAction.Update)
      {
        sql.Append(" DO UPDATE SET ");
        var updateColumns = columns.Where(c => !_conflictColumns.Contains(c));
        var updateList = string.Join(", ", updateColumns.Select(c => $"{c} = EXCLUDED.{c}"));
        sql.Append(updateList);
      }
    }
    
    return sql.ToString();
  }

  private void CreateTableIfNotExists(NpgsqlConnection connection, DataRow sampleRow)
  {
    var columns = sampleRow.GetColumnNames();
    var columnDefinitions = new List<string>();
    
    foreach (var column in columns)
    {
      var value = sampleRow[column];
      var dataType = GetPostgreSqlDataType(value);
      columnDefinitions.Add($"{column} {dataType}");
    }
    
    var sql = $@"
      CREATE TABLE IF NOT EXISTS {_tableName} (
        {string.Join(",\n        ", columnDefinitions)}
      )";
    
    using var command = new NpgsqlCommand(sql, connection);
    command.ExecuteNonQuery();
  }

  private async Task CreateTableIfNotExistsAsync(NpgsqlConnection connection, DataRow sampleRow)
  {
    var columns = sampleRow.GetColumnNames();
    var columnDefinitions = new List<string>();
    
    foreach (var column in columns)
    {
      var value = sampleRow[column];
      var dataType = GetPostgreSqlDataType(value);
      columnDefinitions.Add($"{column} {dataType}");
    }
    
    var sql = $@"
      CREATE TABLE IF NOT EXISTS {_tableName} (
        {string.Join(",\n        ", columnDefinitions)}
      )";
    
    using var command = new NpgsqlCommand(sql, connection);
    await command.ExecuteNonQueryAsync();
  }

  private string GetPostgreSqlDataType(object value)
  {
    if (value == null)
      return "TEXT";
    
    return value switch
    {
      int _ => "INTEGER",
      long _ => "BIGINT",
      short _ => "SMALLINT",
      byte _ => "SMALLINT",
      decimal _ => "DECIMAL(18,4)",
      double _ => "DOUBLE PRECISION",
      float _ => "REAL",
      bool _ => "BOOLEAN",
      DateTime _ => "TIMESTAMP",
      DateOnly _ => "DATE",
      TimeOnly _ => "TIME",
      Guid _ => "UUID",
      byte[] _ => "BYTEA",
      _ => "TEXT"
    };
  }

  public void Truncate()
  {
    using var connection = new NpgsqlConnection(_connectionString);
    connection.Open();
    
    using var command = new NpgsqlCommand($"TRUNCATE TABLE {_tableName}", connection);
    command.ExecuteNonQuery();
  }

  public async Task TruncateAsync()
  {
    using var connection = new NpgsqlConnection(_connectionString);
    await connection.OpenAsync();
    
    using var command = new NpgsqlCommand($"TRUNCATE TABLE {_tableName}", connection);
    await command.ExecuteNonQueryAsync();
  }
}