using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PipeFlow.Core;

public class CsvReader
{
    private readonly string _filePath;
    private string _delimiter = ",";
    private bool _hasHeaders = true;
    private Encoding _encoding = Encoding.UTF8;
    private bool _trimValues = true;
    private bool _autoConvert = true;
    private int _bufferSize = 65536;

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

    public CsvReader WithAutoConvert(bool autoConvert = true)
    {
        _autoConvert = autoConvert;
        return this;
    }

    public CsvReader WithBufferSize(int bufferSize)
    {
        if (bufferSize <= 0)
            throw new ArgumentException("Buffer size must be positive", nameof(bufferSize));
        _bufferSize = bufferSize;
        return this;
    }

    public IEnumerable<DataRow> Read()
    {
        using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, _bufferSize, FileOptions.SequentialScan);
        using var reader = new StreamReader(stream, _encoding, false, _bufferSize);
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
                var value = values[i];
                if (_trimValues && value != null)
                {
                    value = value.Trim();
                }
                if (_autoConvert)
                {
                    row[headers[i]] = ConvertValue(value);
                }
                else
                {
                    row[headers[i]] = value;
                }
            }

            yield return row;
        }
    }

    private string ReadCsvLine(StreamReader reader)
    {
        if (reader.EndOfStream)
            return null;

        var line = new StringBuilder(256);
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

        var length = line.Length;
        if (length >= 2 && line[length - 2] == '\r' && line[length - 1] == '\n')
        {
            line.Length = length - 2;
        }
        else if (length >= 1 && (line[length - 1] == '\n' || line[length - 1] == '\r'))
        {
            line.Length = length - 1;
        }
        
        return line.ToString();
    }

    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>(32);
        var currentField = new StringBuilder(128);
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

        if (value.Length <= 10 && int.TryParse(value, out int intValue))
            return intValue;

        if (value.Contains('.') && double.TryParse(value, out double doubleValue))
            return doubleValue;

        if (value.Length <= 5)
        {
            if (value == "true" || value == "True" || value == "TRUE")
                return true;
            if (value == "false" || value == "False" || value == "FALSE")
                return false;
        }

        if (value.Contains('-') || value.Contains('/'))
        {
            if (DateTime.TryParse(value, out DateTime dateValue))
                return dateValue;
        }

        return value;
    }
}