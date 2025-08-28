using System;
using System.Collections.Generic;

namespace DataFlow.Core;

public interface IDataRow
{
    object this[string columnName] { get; set; }
    object this[int columnIndex] { get; set; }
    
    bool ContainsColumn(string columnName);
    T GetValue<T>(string columnName);
    bool TryGetValue<T>(string columnName, out T value);
    IEnumerable<string> GetColumnNames();
    Dictionary<string, object> ToDictionary();
}