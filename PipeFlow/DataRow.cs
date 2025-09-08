using System;
using System.Collections.Generic;
using System.Linq;

namespace PipeFlow.Core;

public class DataRow : IDataRow
{
    private readonly Dictionary<string, object> _data;
    private readonly List<string> _columnOrder;

    public DataRow()
    {
        _data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        _columnOrder = new List<string>();
    }

    public DataRow(Dictionary<string, object> data) : this()
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        
        foreach (var kvp in data)
        {
            _data[kvp.Key] = kvp.Value;
            _columnOrder.Add(kvp.Key);
        }
    }

    public object this[string columnName]
    {
        get
        {
            if (!_data.ContainsKey(columnName))
                throw new KeyNotFoundException($"Column '{columnName}' not found");
            return _data[columnName];
        }
        set
        {
            if (!_data.ContainsKey(columnName))
                _columnOrder.Add(columnName);
            _data[columnName] = value;
        }
    }

    public object this[int columnIndex]
    {
        get
        {
            if (columnIndex < 0 || columnIndex >= _columnOrder.Count)
                throw new IndexOutOfRangeException($"Column index {columnIndex} is out of range");
            return _data[_columnOrder[columnIndex]];
        }
        set
        {
            if (columnIndex < 0 || columnIndex >= _columnOrder.Count)
                throw new IndexOutOfRangeException($"Column index {columnIndex} is out of range");
            _data[_columnOrder[columnIndex]] = value;
        }
    }

    public bool ContainsColumn(string columnName)
    {
        return _data.ContainsKey(columnName);
    }

    public T GetValue<T>(string columnName)
    {
        var value = this[columnName];
        if (value == null)
            return default(T);
        
        if (value is T typedValue)
            return typedValue;
        
        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Cannot convert column '{columnName}' value to {typeof(T).Name}", ex);
        }
    }

    public bool TryGetValue<T>(string columnName, out T value)
    {
        value = default(T);
        
        if (!ContainsColumn(columnName))
            return false;
        
        try
        {
            value = GetValue<T>(columnName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public IEnumerable<string> GetColumnNames()
    {
        return _columnOrder.AsEnumerable();
    }

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>(_data, StringComparer.OrdinalIgnoreCase);
    }

    public override string ToString()
    {
        var pairs = _columnOrder.Select(col => $"{col}: {_data[col] ?? "null"}");
        return $"{{{string.Join(", ", pairs)}}}";
    }
}