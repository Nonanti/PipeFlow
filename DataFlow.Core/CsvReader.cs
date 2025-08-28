using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DataFlow.Core;

public class CsvReader
{
    private readonly string _filePath;
    private string _delimiter = ",";
    private bool _hasHeaders = true;
    private Encoding _encoding = Encoding.UTF8;
    private bool _trimValues = true;

    public CsvReader(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));
        
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"CSV file not found: {filePath}");
        
        _filePath = filePath;
    }

    public CsvReader WithDelimiter(string delimiter)
    {
        _delimiter = delimiter ?? throw new ArgumentNullException(nameof(delimiter));
        return this;
    }

    public CsvReader WithHeaders(bool hasHeaders = true)
    {
        _hasHeaders = hasHeaders;
        return this;
    }

    public CsvReader WithEncoding(Encoding encoding)
    {
        _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        return this;
    }

    public CsvReader WithTrimming(bool trim = true)
    {
        _trimValues = trim;
        return this;
    }

    public IEnumerable<DataRow> Read()
    {
        using var reader = new StreamReader(_filePath, _encoding);
        string[] headers = null;
        int lineNumber = 0;

        while (!reader.EndOfStream)
        {
            var line = ReadCsvLine(reader);
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = ParseCsvLine(line);

            if (_hasHeaders && headers == null)
            {
                headers = values;
                continue;
            }

            if (headers == null)
            {
                headers = Enumerable.Range(0, values.Length)
                    .Select(i => $"Column{i}")
                    .ToArray();
            }

            var row = new DataRow();
            for (int i = 0; i < Math.Min(headers.Length, values.Length); i++)
            {
                var value = _trimValues ? values[i]?.Trim() : values[i];
                row[headers[i]] = ConvertValue(value);
            }

            yield return row;
        }
    }

    private string ReadCsvLine(StreamReader reader)
    {
        if (reader.EndOfStream)
            return null;

        var line = new StringBuilder();
        bool inQuotes = false;
        int c;

        while ((c = reader.Read()) != -1)
        {
            char ch = (char)c;
            line.Append(ch);

            if (ch == '"')
            {
                if (inQuotes && reader.Peek() == '"')
                {
                    line.Append((char)reader.Read());
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == '\n' && !inQuotes)
            {
                break;
            }
        }

        var result = line.ToString();
        if (result.EndsWith("\r\n"))
            return result.Substring(0, result.Length - 2);
        if (result.EndsWith("\n") || result.EndsWith("\r"))
            return result.Substring(0, result.Length - 1);
        
        return result;
    }

    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentField.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == _delimiter[0] && !inQuotes && _delimiter.Length == 1)
            {
                result.Add(currentField.ToString());
                currentField.Clear();
            }
            else if (!inQuotes && i + _delimiter.Length <= line.Length && 
                     line.Substring(i, _delimiter.Length) == _delimiter)
            {
                result.Add(currentField.ToString());
                currentField.Clear();
                i += _delimiter.Length - 1;
            }
            else
            {
                currentField.Append(c);
            }
        }

        result.Add(currentField.ToString());
        return result.ToArray();
    }

    private object ConvertValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (int.TryParse(value, out int intValue))
            return intValue;

        if (double.TryParse(value, out double doubleValue))
            return doubleValue;

        if (bool.TryParse(value, out bool boolValue))
            return boolValue;

        if (DateTime.TryParse(value, out DateTime dateValue))
            return dateValue;

        return value;
    }
}