using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using DataFlow.Core;

namespace DataFlow.Tests;

public class CsvTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly List<string> _tempFiles;

    public CsvTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"DataFlowTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _tempFiles = new List<string>();
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }

        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, true);
    }

    private string CreateTempCsvFile(string content)
    {
        var fileName = Path.Combine(_testDirectory, $"test_{Guid.NewGuid()}.csv");
        File.WriteAllText(fileName, content);
        _tempFiles.Add(fileName);
        return fileName;
    }

    [Fact]
    public void CsvReader_ReadsSimpleFile()
    {
        var csvContent = @"Name,Age,City
John,25,New York
Jane,30,London
Bob,35,Paris";

        var fileName = CreateTempCsvFile(csvContent);
        var reader = new CsvReader(fileName);
        var rows = reader.Read().ToList();

        Assert.Equal(3, rows.Count);
        Assert.Equal("John", rows[0]["Name"]);
        Assert.Equal(25, rows[0]["Age"]);
        Assert.Equal("New York", rows[0]["City"]);
    }

    [Fact]
    public void CsvReader_HandlesQuotedFields()
    {
        var csvContent = @"Name,Description
Product1,""This is a description with, comma""
Product2,""Quote test: """"quoted text""""""";

        var fileName = CreateTempCsvFile(csvContent);
        var reader = new CsvReader(fileName);
        var rows = reader.Read().ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("This is a description with, comma", rows[0]["Description"]);
        Assert.Equal("Quote test: \"quoted text\"", rows[1]["Description"]);
    }

    [Fact]
    public void CsvReader_WithCustomDelimiter()
    {
        var csvContent = @"Name;Age;City
Alice;28;Berlin
Bob;32;Madrid";

        var fileName = CreateTempCsvFile(csvContent);
        var reader = new CsvReader(fileName)
            .WithDelimiter(";");
        var rows = reader.Read().ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("Alice", rows[0]["Name"]);
        Assert.Equal("Berlin", rows[0]["City"]);
    }

    [Fact]
    public void CsvReader_WithoutHeaders()
    {
        var csvContent = @"John,25,Developer
Jane,30,Designer";

        var fileName = CreateTempCsvFile(csvContent);
        var reader = new CsvReader(fileName)
            .WithHeaders(false);
        var rows = reader.Read().ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("John", rows[0]["Column0"]);
        Assert.Equal(25, rows[0]["Column1"]);
        Assert.Equal("Developer", rows[0]["Column2"]);
    }

    [Fact]
    public void CsvWriter_WritesDataCorrectly()
    {
        var rows = new List<DataRow>();
        
        var row1 = new DataRow();
        row1["Name"] = "Alice";
        row1["Age"] = 25;
        row1["Active"] = true;
        rows.Add(row1);

        var row2 = new DataRow();
        row2["Name"] = "Bob";
        row2["Age"] = 30;
        row2["Active"] = false;
        rows.Add(row2);

        var fileName = Path.Combine(_testDirectory, "output.csv");
        _tempFiles.Add(fileName);

        var writer = new CsvWriter(fileName);
        writer.Write(rows);

        var content = File.ReadAllText(fileName);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(3, lines.Length);
        Assert.Contains("Name,Age,Active", lines[0]);
        Assert.Contains("Alice,25,true", lines[1]);
        Assert.Contains("Bob,30,false", lines[2]);
    }

    [Fact]
    public void CsvWriter_HandlesSpecialCharacters()
    {
        var rows = new List<DataRow>();
        
        var row = new DataRow();
        row["Text"] = "Line 1\nLine 2";
        row["Quoted"] = "Has \"quotes\"";
        row["Comma"] = "Has, comma";
        rows.Add(row);

        var fileName = Path.Combine(_testDirectory, "special.csv");
        _tempFiles.Add(fileName);

        var writer = new CsvWriter(fileName);
        writer.Write(rows);

        var reader = new CsvReader(fileName);
        var readRows = reader.Read().ToList();

        Assert.Single(readRows);
        Assert.Equal("Line 1\nLine 2", readRows[0]["Text"]);
        Assert.Equal("Has \"quotes\"", readRows[0]["Quoted"]);
        Assert.Equal("Has, comma", readRows[0]["Comma"]);
    }

    [Fact]
    public void DataFlow_CsvIntegration()
    {
        var csvContent = @"Id,Name,Score
1,Alice,85
2,Bob,92
3,Charlie,78
4,Diana,88
5,Eve,95";

        var inputFile = CreateTempCsvFile(csvContent);
        var outputFile = Path.Combine(_testDirectory, "filtered.csv");
        _tempFiles.Add(outputFile);

        DataFlow.Core.DataFlow.From
            .Csv(inputFile)
            .Filter(row => row.GetValue<int>("Score") >= 85)
            .OrderByDescending(row => row["Score"])
            .ToCsv(outputFile);

        var reader = new CsvReader(outputFile);
        var results = reader.Read().ToList();

        Assert.Equal(4, results.Count);
        Assert.Equal("Eve", results[0]["Name"]);
        Assert.Equal("Bob", results[1]["Name"]);
        Assert.Equal("Diana", results[2]["Name"]);
        Assert.Equal("Alice", results[3]["Name"]);
    }
}