using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using DataFlow.Core;
using static DataFlow.Core.DataFlow;

namespace DataFlow.Tests;

public class ExtensionTests
{
    private List<DataRow> CreateTestData()
    {
        var rows = new List<DataRow>();

        var row1 = new DataRow();
        row1["Id"] = 1;
        row1["Name"] = "Alice";
        row1["Department"] = "Engineering";
        row1["Salary"] = 75000;
        rows.Add(row1);

        var row2 = new DataRow();
        row2["Id"] = 2;
        row2["Name"] = "Bob";
        row2["Department"] = "Engineering";
        row2["Salary"] = 82000;
        rows.Add(row2);

        var row3 = new DataRow();
        row3["Id"] = 3;
        row3["Name"] = "Charlie";
        row3["Department"] = "Sales";
        row3["Salary"] = 68000;
        rows.Add(row3);

        var row4 = new DataRow();
        row4["Id"] = 1;  
        row4["Name"] = "Alice Duplicate";
        row4["Department"] = "HR";
        row4["Salary"] = 71000;
        rows.Add(row4);

        return rows;
    }

    [Fact]
    public void RemoveDuplicates_RemovesDuplicatesByKey()
    {
        var data = CreateTestData();
        var pipeline = DataFlow.Core.DataFlow.From.DataRows(data);

        var result = pipeline
            .RemoveDuplicates("Id")
            .ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal("Alice", result[0]["Name"]);
        Assert.DoesNotContain(result, r => r["Name"].ToString() == "Alice Duplicate");
    }

    [Fact]
    public void FillMissing_FillsNullValues()
    {
        var rows = new List<DataRow>();
        
        var row1 = new DataRow();
        row1["Name"] = "John";
        row1["Email"] = null;
        rows.Add(row1);

        var row2 = new DataRow();
        row2["Name"] = "Jane";
        row2["Email"] = "jane@example.com";
        rows.Add(row2);

        var pipeline = DataFlow.Core.DataFlow.From.DataRows(rows);
        var result = pipeline
            .FillMissing("Email", "no-email@example.com")
            .ToList();

        Assert.Equal("no-email@example.com", result[0]["Email"]);
        Assert.Equal("jane@example.com", result[1]["Email"]);
    }

    [Fact]
    public void AddColumn_AddsNewColumnWithCalculatedValue()
    {
        var data = CreateTestData();
        var pipeline = DataFlow.Core.DataFlow.From.DataRows(data);

        var result = pipeline
            .AddColumn("AnnualBonus", row => row.GetValue<int>("Salary") * 0.1)
            .ToList();

        Assert.Equal(7500.0, result[0]["AnnualBonus"]);
        Assert.Equal(8200.0, result[1]["AnnualBonus"]);
    }

    [Fact]
    public void RemoveColumn_RemovesSpecifiedColumn()
    {
        var data = CreateTestData();
        var pipeline = DataFlow.Core.DataFlow.From.DataRows(data);

        var result = pipeline
            .RemoveColumn("Salary")
            .First();

        Assert.False(result.ContainsColumn("Salary"));
        Assert.True(result.ContainsColumn("Name"));
        Assert.True(result.ContainsColumn("Department"));
    }

    [Fact]
    public void RenameColumn_RenamesColumnCorrectly()
    {
        var data = CreateTestData();
        var pipeline = DataFlow.Core.DataFlow.From.DataRows(data);

        var result = pipeline
            .RenameColumn("Name", "EmployeeName")
            .First();

        Assert.False(result.ContainsColumn("Name"));
        Assert.True(result.ContainsColumn("EmployeeName"));
        Assert.Equal("Alice", result["EmployeeName"]);
    }

    [Fact]
    public void GroupBy_WithAggregations_WorksCorrectly()
    {
        var data = CreateTestData();
        var pipeline = DataFlow.Core.DataFlow.From.DataRows(data);

        var result = pipeline
            .GroupBy(
                "Department",
                ("EmployeeCount", group => group.Count()),
                ("AverageSalary", group => group.Average(r => r.GetValue<int>("Salary"))),
                ("MaxSalary", group => group.Max(r => r.GetValue<int>("Salary")))
            )
            .OrderBy(row => row["Department"])
            .ToList();

        Assert.Equal(3, result.Count);

        var engineering = result.First(r => r["Department"].ToString() == "Engineering");
        Assert.Equal(2, engineering["EmployeeCount"]);
        Assert.Equal(78500.0, engineering["AverageSalary"]);
        Assert.Equal(82000, engineering["MaxSalary"]);
    }

    [Fact]
    public void ComplexPipeline_WorksCorrectly()
    {
        var data = CreateTestData();
        var pipeline = DataFlow.Core.DataFlow.From.DataRows(data);

        var result = pipeline
            .RemoveDuplicates("Id")
            .AddColumn("Category", row => 
                row.GetValue<int>("Salary") > 70000 ? "Senior" : "Junior")
            .Filter(row => row["Department"].ToString() != "Sales")
            .RenameColumn("Name", "EmployeeName")
            .OrderBy(row => row["Salary"])
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.True(r.ContainsColumn("EmployeeName")));
        Assert.All(result, r => Assert.True(r.ContainsColumn("Category")));
        Assert.Equal("Alice", result[0]["EmployeeName"]);
        Assert.Equal("Senior", result[0]["Category"]);
    }
}