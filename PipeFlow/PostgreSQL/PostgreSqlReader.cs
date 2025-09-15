using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Npgsql;

namespace PipeFlow.Core.PostgreSQL;

public class PostgreSqlReader
{
  private readonly string _connectionString;
  private string _query;
  private NpgsqlCommand _command;
  private readonly Dictionary<string, object> _parameters = new Dictionary<string, object>();
  private int _commandTimeout = 30;
  private int _batchSize = 1000;

  public PostgreSqlReader(string connectionString)
  {
    if (string.IsNullOrWhiteSpace(connectionString))
      throw new ArgumentNullException(nameof(connectionString));
    
    _connectionString = connectionString;
  }

  public PostgreSqlReader Query(string sql)
  {
    if (string.IsNullOrWhiteSpace(sql))
      throw new ArgumentNullException(nameof(sql));
    
    _query = sql;
    return this;
  }

  public PostgreSqlReader Query(string sql, object parameters)
  {
    Query(sql);
    
    if (parameters != null)
    {
      var properties = parameters.GetType().GetProperties();
      foreach (var prop in properties)
      {
        _parameters[$"@{prop.Name}"] = prop.GetValue(parameters);
      }
    }
    
    return this;
  }

  public PostgreSqlReader WithTimeout(int seconds)
  {
    if (seconds <= 0)
      throw new ArgumentException("Timeout must be greater than zero", nameof(seconds));
    
    _commandTimeout = seconds;
    return this;
  }

  public PostgreSqlReader WithBatchSize(int batchSize)
  {
    if (batchSize <= 0)
      throw new ArgumentException("Batch size must be greater than zero", nameof(batchSize));
    
    _batchSize = batchSize;
    return this;
  }

  public PostgreSqlReader AddParameter(string name, object value)
  {
    if (string.IsNullOrWhiteSpace(name))
      throw new ArgumentNullException(nameof(name));
    
    if (!name.StartsWith("@"))
      name = "@" + name;
    
    _parameters[name] = value;
    return this;
  }

  public IEnumerable<DataRow> Read()
  {
    if (string.IsNullOrWhiteSpace(_query))
      throw new InvalidOperationException("Query has not been set. Call Query() method first.");
    
    using var connection = new NpgsqlConnection(_connectionString);
    connection.Open();
    
    using var command = new NpgsqlCommand(_query, connection);
    command.CommandTimeout = _commandTimeout;
    
    foreach (var param in _parameters)
    {
      command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
    }
    
    using var reader = command.ExecuteReader();
    var columnNames = GetColumnNames(reader);
    
    while (reader.Read())
    {
      var row = new DataRow();
      
      foreach (var columnName in columnNames)
      {
        var value = reader[columnName];
        row[columnName] = value == DBNull.Value ? null : value;
      }
      
      yield return row;
    }
  }

  public async IAsyncEnumerable<DataRow> ReadAsync()
  {
    if (string.IsNullOrWhiteSpace(_query))
      throw new InvalidOperationException("Query has not been set. Call Query() method first.");
    
    using var connection = new NpgsqlConnection(_connectionString);
    await connection.OpenAsync();
    
    using var command = new NpgsqlCommand(_query, connection);
    command.CommandTimeout = _commandTimeout;
    
    foreach (var param in _parameters)
    {
      command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
    }
    
    using var reader = await command.ExecuteReaderAsync();
    var columnNames = GetColumnNames(reader);
    
    while (await reader.ReadAsync())
    {
      var row = new DataRow();
      
      foreach (var columnName in columnNames)
      {
        var value = reader[columnName];
        row[columnName] = value == DBNull.Value ? null : value;
      }
      
      yield return row;
    }
  }

  public DataTable ReadToDataTable()
  {
    if (string.IsNullOrWhiteSpace(_query))
      throw new InvalidOperationException("Query has not been set. Call Query() method first.");
    
    using var connection = new NpgsqlConnection(_connectionString);
    using var adapter = new NpgsqlDataAdapter(_query, connection);
    
    foreach (var param in _parameters)
    {
      adapter.SelectCommand.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
    }
    
    adapter.SelectCommand.CommandTimeout = _commandTimeout;
    
    var dataTable = new DataTable();
    adapter.Fill(dataTable);
    
    return dataTable;
  }

  private List<string> GetColumnNames(NpgsqlDataReader reader)
  {
    var columnNames = new List<string>();
    
    for (int i = 0; i < reader.FieldCount; i++)
    {
      columnNames.Add(reader.GetName(i));
    }
    
    return columnNames;
  }

  public T ExecuteScalar<T>()
  {
    if (string.IsNullOrWhiteSpace(_query))
      throw new InvalidOperationException("Query has not been set. Call Query() method first.");
    
    using var connection = new NpgsqlConnection(_connectionString);
    connection.Open();
    
    using var command = new NpgsqlCommand(_query, connection);
    command.CommandTimeout = _commandTimeout;
    
    foreach (var param in _parameters)
    {
      command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
    }
    
    var result = command.ExecuteScalar();
    
    if (result == null || result == DBNull.Value)
      return default(T);
    
    return (T)Convert.ChangeType(result, typeof(T));
  }
}