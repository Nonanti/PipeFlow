using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DataFlow.Core;

public class CsvWriter
{
    private readonly string _filePath;
    private string _delimiter = ",";
    private bool _includeHeaders = true;
    private Encoding _encoding = Encoding.UTF8;
    private bool _quoteAllFields = false;
    private bool _append = false;

    public CsvWriter(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));
        
        _filePath = filePath;
    }

    public CsvWriter WithDelimiter(string delimiter)
    {
        _delimiter = delimiter ?? throw new ArgumentNullException(nameof(delimiter));
        return this;
    }

    public CsvWriter WithHeaders(bool includeHeaders = true)
    {
        _includeHeaders = includeHeaders;
        return this;
    }

    public CsvWriter WithEncoding(Encoding encoding)
    {
        _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        return this;
    }

    public CsvWriter WithQuoting(bool quoteAll = false)
    {
        _quoteAllFields = quoteAll;
        return this;
    }

    public CsvWriter WithAppend(bool append = false)
    {
        _append = append;
        return this;
    }

    public void Write(IEnumerable<DataRow> rows)
    {
        if (rows == null)
            throw new ArgumentNullException(nameof(rows));

        var rowsList = rows.ToList();
        if (!rowsList.Any())
            return;

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using var writer = new StreamWriter(_filePath, _append, _encoding);
        
        var firstRow = rowsList.First();
        var columns = firstRow.GetColumnNames().ToList();

        if (_includeHeaders && !_append)
        {
            var headerLine = string.Join(_delimiter, columns.Select(c => FormatField(c)));
            writer.WriteLine(headerLine);
        }

        foreach (var row in rowsList)
        {
            var values = columns.Select(col => 
            {
                var value = row.ContainsColumn(col) ? row[col] : null;
                return FormatField(ConvertToString(value));
            });
            
            writer.WriteLine(string.Join(_delimiter, values));
        }
    }

    public void Write<T>(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        var itemsList = items.ToList();
        if (!itemsList.Any())
            return;

        var properties = typeof(T).GetProperties()
            .Where(p => p.CanRead)
            .ToList();

        var rows = itemsList.Select(item =>
        {
            var row = new DataRow();
            foreach (var prop in properties)
            {
                row[prop.Name] = prop.GetValue(item);
            }
            return row;
        });

        Write(rows);
    }

    private string FormatField(string value)
    {
        if (value == null)
            return string.Empty;

        bool needsQuoting = _quoteAllFields || 
                          value.Contains(_delimiter) || 
                          value.Contains("\"") || 
                          value.Contains("\n") || 
                          value.Contains("\r");

        if (!needsQuoting)
            return value;

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private string ConvertToString(object value)
    {
        if (value == null)
            return string.Empty;

        if (value is DateTime dt)
            return dt.ToString("yyyy-MM-dd HH:mm:ss");

        if (value is bool b)
            return b.ToString().ToLower();

        return value.ToString();
    }
}