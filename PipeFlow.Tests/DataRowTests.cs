using System;
using System.Collections.Generic;
using Xunit;
using PipeFlow.Core;

namespace PipeFlow.Tests;

public class DataRowTests
{
    [Fact]
    public void Constructor_WithDictionary_InitializesCorrectly()
    {
        var data = new Dictionary<string, object>
        {
            { "Name", "John" },
            { "Age", 25 },
            { "Active", true }
        };

        var row = new DataRow(data);

        Assert.Equal("John", row["Name"]);
        Assert.Equal(25, row["Age"]);
        Assert.Equal(true, row["Active"]);
    }

    [Fact]
    public void Indexer_ByColumnName_ReturnsCorrectValue()
    {
        var row = new DataRow();
        row["FirstName"] = "Jane";
        row["LastName"] = "Doe";

        Assert.Equal("Jane", row["FirstName"]);
        Assert.Equal("Doe", row["LastName"]);
    }

    [Fact]
    public void Indexer_ByColumnIndex_ReturnsCorrectValue()
    {
        var row = new DataRow();
        row["Column1"] = "Value1";
        row["Column2"] = "Value2";

        Assert.Equal("Value1", row[0]);
        Assert.Equal("Value2", row[1]);
    }

    [Fact]
    public void ContainsColumn_ReturnsCorrectResult()
    {
        var row = new DataRow();
        row["Existing"] = "Value";

        Assert.True(row.ContainsColumn("Existing"));
        Assert.False(row.ContainsColumn("NonExisting"));
    }

    [Fact]
    public void GetValue_WithTypeConversion_ReturnsCorrectType()
    {
        var row = new DataRow();
        row["StringNumber"] = "42";
        row["IntNumber"] = 100;

        int converted = row.GetValue<int>("StringNumber");
        Assert.Equal(42, converted);

        string stringValue = row.GetValue<string>("IntNumber");
        Assert.Equal("100", stringValue);
    }

    [Fact]
    public void TryGetValue_WithValidColumn_ReturnsTrue()
    {
        var row = new DataRow();
        row["Age"] = 30;

        bool result = row.TryGetValue<int>("Age", out int age);

        Assert.True(result);
        Assert.Equal(30, age);
    }

    [Fact]
    public void TryGetValue_WithInvalidColumn_ReturnsFalse()
    {
        var row = new DataRow();

        bool result = row.TryGetValue<int>("NonExisting", out int value);

        Assert.False(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void GetColumnNames_ReturnsAllColumns()
    {
        var row = new DataRow();
        row["Col1"] = "A";
        row["Col2"] = "B";
        row["Col3"] = "C";

        var columns = row.GetColumnNames();

        Assert.Contains("Col1", columns);
        Assert.Contains("Col2", columns);
        Assert.Contains("Col3", columns);
    }

    [Fact]
    public void ToDictionary_ReturnsCorrectDictionary()
    {
        var row = new DataRow();
        row["Key1"] = "Value1";
        row["Key2"] = 123;

        var dict = row.ToDictionary();

        Assert.Equal(2, dict.Count);
        Assert.Equal("Value1", dict["Key1"]);
        Assert.Equal(123, dict["Key2"]);
    }

    [Fact]
    public void CaseInsensitive_ColumnAccess_Works()
    {
        var row = new DataRow();
        row["FirstName"] = "John";

        Assert.Equal("John", row["firstname"]);
        Assert.Equal("John", row["FIRSTNAME"]);
        Assert.Equal("John", row["FirstName"]);
    }
}