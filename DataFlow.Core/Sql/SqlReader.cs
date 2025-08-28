using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DataFlow.Core.Sql;

public class SqlReader
{
    private readonly string _connectionString;
    private string _query;
    private CommandType _commandType = CommandType.Text;
    private readonly Dictionary<string, object> _parameters;
    private int _commandTimeout = 30;

    public SqlReader(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException(nameof(connectionString));
        
        _connectionString = connectionString;
        _parameters = new Dictionary<string, object>();
    }

    public SqlReader Query(string query)
    {
        _query = query ?? throw new ArgumentNullException(nameof(query));
        return this;
    }

    public SqlReader StoredProcedure(string procedureName)
    {
        _query = procedureName ?? throw new ArgumentNullException(nameof(procedureName));
        _commandType = CommandType.StoredProcedure;
        return this;
    }

    public SqlReader WithParameter(string name, object value)
    {
        _parameters[name] = value;
        return this;
    }

    public SqlReader WithTimeout(int seconds)
    {
        _commandTimeout = seconds;
        return this;
    }

    public IEnumerable<DataRow> Read()
    {
        if (string.IsNullOrWhiteSpace(_query))
            throw new InvalidOperationException("Query or stored procedure must be specified");

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using var command = new SqlCommand(_query, connection)
        {
            CommandType = _commandType,
            CommandTimeout = _commandTimeout
        };

        foreach (var param in _parameters)
        {
            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
        }

        using var reader = command.ExecuteReader();
        var columnNames = new List<string>();
        
        for (int i = 0; i < reader.FieldCount; i++)
        {
            columnNames.Add(reader.GetName(i));
        }

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

    public DataRow ReadSingle()
    {
        foreach (var row in Read())
        {
            return row;
        }
        return null;
    }

    public T ReadScalar<T>()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using var command = new SqlCommand(_query, connection)
        {
            CommandType = _commandType,
            CommandTimeout = _commandTimeout
        };

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