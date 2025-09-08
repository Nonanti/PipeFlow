using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PipeFlow.Core.Json;

public class JsonReader
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;
    private string _jsonPath;

    public JsonReader(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));
        
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"JSON file not found: {filePath}");
        
        _filePath = filePath;
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }

    public JsonReader WithOptions(Action<JsonSerializerOptions> configure)
    {
        configure?.Invoke(_options);
        return this;
    }

    public JsonReader SelectPath(string jsonPath)
    {
        _jsonPath = jsonPath;
        return this;
    }

    public IEnumerable<DataRow> Read()
    {
        var json = File.ReadAllText(_filePath);
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        var element = doc.RootElement;
        
        if (!string.IsNullOrEmpty(_jsonPath))
        {
            element = NavigateToPath(element, _jsonPath);
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                yield return ConvertToDataRow(item);
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            yield return ConvertToDataRow(element);
        }
    }

    private JsonElement NavigateToPath(JsonElement root, string path)
    {
        if (path.StartsWith("$"))
            path = path.Substring(1);
        
        if (path.StartsWith("."))
            path = path.Substring(1);

        var segments = ParseJsonPath(path);
        var current = root;

        foreach (var segment in segments)
        {
            if (segment.StartsWith("[") && segment.EndsWith("]"))
            {
                var indexStr = segment.Substring(1, segment.Length - 2);
                
                if (indexStr == "*")
                {
                    continue;
                }
                
                if (int.TryParse(indexStr, out int index))
                {
                    if (current.ValueKind == JsonValueKind.Array)
                    {
                        var array = current.EnumerateArray().ToList();
                        if (index < array.Count)
                            current = array[index];
                    }
                }
            }
            else
            {
                if (current.TryGetProperty(segment, out JsonElement property))
                {
                    current = property;
                }
            }
        }

        return current;
    }

    private List<string> ParseJsonPath(string path)
    {
        var segments = new List<string>();
        var current = string.Empty;
        bool inBrackets = false;

        for (int i = 0; i < path.Length; i++)
        {
            char c = path[i];
            
            if (c == '[')
            {
                if (!string.IsNullOrEmpty(current))
                {
                    segments.Add(current);
                    current = string.Empty;
                }
                inBrackets = true;
                current += c;
            }
            else if (c == ']')
            {
                current += c;
                segments.Add(current);
                current = string.Empty;
                inBrackets = false;
            }
            else if (c == '.' && !inBrackets)
            {
                if (!string.IsNullOrEmpty(current))
                {
                    segments.Add(current);
                    current = string.Empty;
                }
            }
            else
            {
                current += c;
            }
        }

        if (!string.IsNullOrEmpty(current))
        {
            segments.Add(current);
        }

        return segments;
    }

    private DataRow ConvertToDataRow(JsonElement element)
    {
        var row = new DataRow();
        
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                row[property.Name] = GetValue(property.Value);
            }
        }

        return row;
    }

    private object GetValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            
            case JsonValueKind.Number:
                if (element.TryGetInt32(out int intValue))
                    return intValue;
                if (element.TryGetInt64(out long longValue))
                    return longValue;
                if (element.TryGetDouble(out double doubleValue))
                    return doubleValue;
                return element.GetDecimal();
            
            case JsonValueKind.True:
                return true;
            
            case JsonValueKind.False:
                return false;
            
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            
            case JsonValueKind.Array:
                return element.EnumerateArray().Select(e => GetValue(e)).ToList();
            
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    dict[property.Name] = GetValue(property.Value);
                }
                return dict;
            
            default:
                return element.ToString();
        }
    }
}