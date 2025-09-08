using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using PipeFlow.Core;

namespace PipeFlow.Benchmarks;

[SimpleJob(RuntimeMoniker.Net90, warmupCount: 3, iterationCount: 5)]
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class QuickBenchmark
{
    private List<BenchmarkData> _data;
    private Pipeline<BenchmarkData> _pipeline;

    [Params(1000, 10000)]
    public int DataSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        _data = new List<BenchmarkData>(DataSize);
        
        for (int i = 0; i < DataSize; i++)
        {
            _data.Add(new BenchmarkData
            {
                Id = i,
                Value = random.Next(1, 100),
                Category = random.Next(1, 6),
                IsActive = random.Next(2) == 1
            });
        }
        
        _pipeline = new Pipeline<BenchmarkData>(_data);
    }

    [Benchmark(Baseline = true)]
    public int StandardLinq()
    {
        return _data
            .Where(x => x.IsActive)
            .Where(x => x.Value > 50)
            .Select(x => x.Category)
            .Distinct()
            .Count();
    }

    [Benchmark]
    public int PipeFlowPipeline()
    {
        return _pipeline
            .Filter(x => x.IsActive)
            .Filter(x => x.Value > 50)
            .Map(x => x.Category)
            .Distinct()
            .Count();
    }

    [Benchmark]
    public double LinqAggregation()
    {
        return _data
            .Where(x => x.Value > 25)
            .Select(x => x.Value * 1.5)
            .Sum();
    }

    [Benchmark]
    public double PipelineAggregation()
    {
        return _pipeline
            .Filter(x => x.Value > 25)
            .Map(x => x.Value * 1.5)
            .ToList()
            .Sum();
    }

    public class BenchmarkData
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public int Category { get; set; }
        public bool IsActive { get; set; }
    }
}