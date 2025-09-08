using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using PipeFlow.Core;

namespace PipeFlow.Benchmarks;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class PipelineBenchmarks
{
    private List<TestData> _data;
    private Pipeline<TestData> _pipeline;

    [Params(100, 1000, 10000, 100000)]
    public int DataSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = GenerateTestData(DataSize);
        _pipeline = new Pipeline<TestData>(_data);
    }

    private List<TestData> GenerateTestData(int count)
    {
        var random = new Random(42);
        var data = new List<TestData>(count);
        
        for (int i = 0; i < count; i++)
        {
            data.Add(new TestData
            {
                Id = i,
                Name = $"Name_{i}",
                Age = random.Next(18, 80),
                Salary = random.Next(30000, 150000),
                Department = random.Next(1, 6) switch
                {
                    1 => "Engineering",
                    2 => "Sales",
                    3 => "Marketing",
                    4 => "HR",
                    _ => "Operations"
                },
                IsActive = random.Next(2) == 1
            });
        }
        
        return data;
    }

    [Benchmark(Baseline = true)]
    public int LinqWhere()
    {
        return _data
            .Where(x => x.Age > 30)
            .Where(x => x.Salary > 50000)
            .Where(x => x.IsActive)
            .Count();
    }

    [Benchmark]
    public int PipelineFilter()
    {
        return _pipeline
            .Filter(x => x.Age > 30)
            .Filter(x => x.Salary > 50000)
            .Filter(x => x.IsActive)
            .Count();
    }

    [Benchmark]
    public List<string> LinqSelectTransform()
    {
        return _data
            .Where(x => x.Age > 30)
            .Select(x => new { x.Name, x.Department })
            .Select(x => $"{x.Name} - {x.Department}")
            .ToList();
    }

    [Benchmark]
    public List<string> PipelineMapTransform()
    {
        return _pipeline
            .Filter(x => x.Age > 30)
            .Map(x => new { x.Name, x.Department })
            .Map(x => $"{x.Name} - {x.Department}")
            .ToList();
    }

    [Benchmark]
    public List<TestData> LinqOrderBy()
    {
        return _data
            .Where(x => x.IsActive)
            .OrderBy(x => x.Age)
            .ThenBy(x => x.Name)
            .Take(100)
            .ToList();
    }

    [Benchmark]
    public List<TestData> PipelineOrderBy()
    {
        return _pipeline
            .Filter(x => x.IsActive)
            .OrderBy(x => x.Age)
            .Take(100)
            .ToList();
    }

    [Benchmark]
    public List<TestData> LinqComplexQuery()
    {
        return _data
            .Where(x => x.Age > 25)
            .Where(x => x.Salary > 40000)
            .OrderByDescending(x => x.Salary)
            .Take(50)
            .Skip(10)
            .Distinct()
            .ToList();
    }

    [Benchmark]
    public List<TestData> PipelineComplexQuery()
    {
        return _pipeline
            .Filter(x => x.Age > 25)
            .Filter(x => x.Salary > 40000)
            .OrderByDescending(x => x.Salary)
            .Take(50)
            .Skip(10)
            .Distinct()
            .ToList();
    }

    [Benchmark]
    public void LinqForEach()
    {
        var sum = 0;
        _data
            .Where(x => x.IsActive)
            .ToList()
            .ForEach(x => sum += x.Age);
    }

    [Benchmark]
    public void PipelineForEach()
    {
        var sum = 0;
        _pipeline
            .Filter(x => x.IsActive)
            .ForEach(x => sum += x.Age);
    }

    public class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public int Salary { get; set; }
        public string Department { get; set; }
        public bool IsActive { get; set; }
    }
}