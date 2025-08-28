using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DataFlow.Core.Json;

public class JsonWriter
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;
    private bool _append = false;

    public JsonWriter(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));
        
        _filePath = filePath;
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public JsonWriter WithOptions(Action<JsonSerializerOptions> configure)
    {
        configure?.Invoke(_options);
        return this;
    }

    public JsonWriter WithIndentation(bool indented = true)
    {
        _options.WriteIndented = indented;
        return this;
    }

    public JsonWriter WithAppend(bool append = false)
    {
        _append = append;
        return this;
    }

    public void Write(IEnumerable<DataRow> rows)
    {
        if (rows == null)
            throw new ArgumentNullException(nameof(rows));

        var rowsList = rows.ToList();
        if (!rowsList.Any() && !_append)
            return;

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var objects = rowsList.Select(row => row.ToDictionary()).ToList();

        if (_append && File.Exists(_filePath))
        {
            var existingJson = File.ReadAllText(_filePath);
            var existingData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(existingJson, _options);
            
            if (existingData != null)
            {
                objects.InsertRange(0, existingData);
            }
        }

        var json = JsonSerializer.Serialize(objects, _options);
        File.WriteAllText(_filePath, json);
    }

    public void WriteObject(DataRow row)
    {
        if (row == null)
            throw new ArgumentNullException(nameof(row));

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var obj = row.ToDictionary();
        var json = JsonSerializer.Serialize(obj, _options);
        File.WriteAllText(_filePath, json);
    }

    public void Write<T>(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        var itemsList = items.ToList();
        if (!itemsList.Any())
            return;

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(itemsList, _options);
        File.WriteAllText(_filePath, json);
    }
}