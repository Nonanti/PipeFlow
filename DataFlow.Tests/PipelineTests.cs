using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using DataFlow.Core;

namespace DataFlow.Tests;

public class PipelineTests
{
    private List<Person> GetTestData()
    {
        return new List<Person>
        {
            new Person { Id = 1, Name = "Alice", Age = 25, City = "New York" },
            new Person { Id = 2, Name = "Bob", Age = 30, City = "London" },
            new Person { Id = 3, Name = "Charlie", Age = 35, City = "New York" },
            new Person { Id = 4, Name = "Diana", Age = 28, City = "Paris" },
            new Person { Id = 5, Name = "Eve", Age = 22, City = "London" }
        };
    }

    [Fact]
    public void Filter_WithPredicate_FiltersCorrectly()
    {
        var data = GetTestData();
        var pipeline = new Pipeline<Person>(data);

        var result = pipeline
            .Filter(p => p.Age > 25)
            .ToList();

        Assert.Equal(3, result.Count);
        Assert.All(result, p => Assert.True(p.Age > 25));
    }

    [Fact]
    public void Map_TransformsDataCorrectly()
    {
        var data = GetTestData();
        var pipeline = new Pipeline<Person>(data);

        var result = pipeline
            .Map(p => new { p.Name, p.Age })
            .ToList();

        Assert.Equal(5, result.Count);
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal(25, result[0].Age);
    }

    [Fact]
    public void Take_ReturnsCorrectNumberOfItems()
    {
        var data = GetTestData();
        var pipeline = new Pipeline<Person>(data);

        var result = pipeline.Take(3).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal("Bob", result[1].Name);
        Assert.Equal("Charlie", result[2].Name);
    }

    [Fact]
    public void Skip_SkipsCorrectNumberOfItems()
    {
        var data = GetTestData();
        var pipeline = new Pipeline<Person>(data);

        var result = pipeline.Skip(2).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal("Charlie", result[0].Name);
    }

    [Fact]
    public void Distinct_RemovesDuplicates()
    {
        var data = new List<int> { 1, 2, 2, 3, 3, 3, 4 };
        var pipeline = new Pipeline<int>(data);

        var result = pipeline.Distinct().ToList();

        Assert.Equal(4, result.Count);
        Assert.Equal(new[] { 1, 2, 3, 4 }, result);
    }

    [Fact]
    public void OrderBy_SortsAscending()
    {
        var data = GetTestData();
        var pipeline = new Pipeline<Person>(data);

        var result = pipeline
            .OrderBy(p => p.Age)
            .ToList();

        Assert.Equal(22, result[0].Age);
        Assert.Equal(25, result[1].Age);
        Assert.Equal(28, result[2].Age);
        Assert.Equal(30, result[3].Age);
        Assert.Equal(35, result[4].Age);
    }

    [Fact]
    public void OrderByDescending_SortsDescending()
    {
        var data = GetTestData();
        var pipeline = new Pipeline<Person>(data);

        var result = pipeline
            .OrderByDescending(p => p.Age)
            .ToList();

        Assert.Equal(35, result[0].Age);
        Assert.Equal(30, result[1].Age);
        Assert.Equal(28, result[2].Age);
        Assert.Equal(25, result[3].Age);
        Assert.Equal(22, result[4].Age);
    }

    [Fact]
    public void ChainedOperations_WorkCorrectly()
    {
        var data = GetTestData();
        var pipeline = new Pipeline<Person>(data);

        var result = pipeline
            .Filter(p => p.Age >= 25)
            .OrderBy(p => p.Name)
            .Take(3)
            .Select(p => p.Name)
            .ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal("Alice", result[0]);
        Assert.Equal("Bob", result[1]);
        Assert.Equal("Charlie", result[2]);
    }

    [Fact]
    public void First_ReturnsFirstElement()
    {
        var data = GetTestData();
        var pipeline = new Pipeline<Person>(data);

        var result = pipeline.First();

        Assert.Equal("Alice", result.Name);
    }

    [Fact]
    public void FirstOrDefault_WithEmptyCollection_ReturnsDefault()
    {
        var pipeline = new Pipeline<Person>(new List<Person>());

        var result = pipeline.FirstOrDefault();

        Assert.Null(result);
    }

    [Fact]
    public void Count_ReturnsCorrectCount()
    {
        var data = GetTestData();
        var pipeline = new Pipeline<Person>(data);

        var count = pipeline
            .Filter(p => p.City == "London")
            .Count();

        Assert.Equal(2, count);
    }

    [Fact]
    public void ForEach_ExecutesActionForEachItem()
    {
        var data = GetTestData();
        var pipeline = new Pipeline<Person>(data);
        var names = new List<string>();

        pipeline
            .Filter(p => p.Age < 30)
            .ForEach(p => names.Add(p.Name));

        Assert.Equal(3, names.Count);
        Assert.Contains("Alice", names);
        Assert.Contains("Diana", names);
        Assert.Contains("Eve", names);
    }

    private class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string City { get; set; }
    }
}