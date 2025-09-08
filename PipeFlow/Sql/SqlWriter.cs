using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace PipeFlow.Core.Sql;

public class SqlWriter
{
    private readonly string _connectionString;
    private readonly string _tableName;
    private int _batchSize = 1000;
    private int _commandTimeout = 30;
    private bool _truncateTable = false;
    private bool _useTransaction = true;

    public SqlWriter(string connectionString, string tableName)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException(nameof(connectionString));
        
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentNullException(nameof(tableName));
        
        _connectionString = connectionString;
        _tableName = tableName;
    }

    public SqlWriter WithBatchSize(int batchSize)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than zero", nameof(batchSize));
        
        _batchSize = batchSize;
        return this;
    }

    public SqlWriter WithTimeout(int seconds)
    {
        _commandTimeout = seconds;
        return this;
    }

    public SqlWriter TruncateFirst(bool truncate = true)
    {
        _truncateTable = truncate;
        return this;
    }

    public SqlWriter WithTransaction(bool useTransaction = true)
    {
        _useTransaction = useTransaction;
        return this;
    }

    public void Write(IEnumerable<DataRow> rows)
    {
        if (rows == null)
            throw new ArgumentNullException(nameof(rows));

        var rowsList = rows.ToList();
        if (!rowsList.Any())
            return;

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        SqlTransaction transaction = null;
        if (_useTransaction)
        {
            transaction = connection.BeginTransaction();
        }

        try
        {
            if (_truncateTable)
            {
                TruncateTable(connection, transaction);
            }

            var firstRow = rowsList.First();
            var columnNames = firstRow.GetColumnNames().ToList();

            foreach (var batch in rowsList.Chunk(_batchSize))
            {
                InsertBatch(connection, transaction, batch, columnNames);
            }

            transaction?.Commit();
        }
        catch
        {
            transaction?.Rollback();
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    public void BulkWrite(IEnumerable<DataRow> rows)
    {
        if (rows == null)
            throw new ArgumentNullException(nameof(rows));

        var rowsList = rows.ToList();
        if (!rowsList.Any())
            return;

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        if (_truncateTable)
        {
            TruncateTable(connection, null);
        }

        var firstRow = rowsList.First();
        var columnNames = firstRow.GetColumnNames().ToList();

        using var bulkCopy = new SqlBulkCopy(connection)
        {
            DestinationTableName = _tableName,
            BatchSize = _batchSize,
            BulkCopyTimeout = _commandTimeout
        };

        var dataTable = new DataTable();
        
        foreach (var columnName in columnNames)
        {
            dataTable.Columns.Add(columnName);
            bulkCopy.ColumnMappings.Add(columnName, columnName);
        }

        foreach (var row in rowsList)
        {
            var dataRow = dataTable.NewRow();
            foreach (var columnName in columnNames)
            {
                dataRow[columnName] = row[columnName] ?? DBNull.Value;
            }
            dataTable.Rows.Add(dataRow);
        }

        bulkCopy.WriteToServer(dataTable);
    }

    private void TruncateTable(SqlConnection connection, SqlTransaction transaction)
    {
        using var command = new SqlCommand($"TRUNCATE TABLE {_tableName}", connection, transaction)
        {
            CommandTimeout = _commandTimeout
        };
        command.ExecuteNonQuery();
    }

    private void InsertBatch(SqlConnection connection, SqlTransaction transaction, 
                            IEnumerable<DataRow> batch, List<string> columnNames)
    {
        var parameterIndex = 0;
        var command = new SqlCommand
        {
            Connection = connection,
            Transaction = transaction,
            CommandTimeout = _commandTimeout
        };

        var commandText = new System.Text.StringBuilder();
        commandText.Append($"INSERT INTO {_tableName} ({string.Join(", ", columnNames)}) VALUES ");

        var valuesClauses = new List<string>();
        
        foreach (var row in batch)
        {
            var paramNames = new List<string>();
            
            foreach (var columnName in columnNames)
            {
                var paramName = $"@p{parameterIndex++}";
                paramNames.Add(paramName);
                command.Parameters.AddWithValue(paramName, row[columnName] ?? DBNull.Value);
            }
            
            valuesClauses.Add($"({string.Join(", ", paramNames)})");
        }

        commandText.Append(string.Join(", ", valuesClauses));
        command.CommandText = commandText.ToString();
        
        command.ExecuteNonQuery();
    }
}