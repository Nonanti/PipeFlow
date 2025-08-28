using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using DataFlow.Core;
using DataFlow.Core.Parallel;

namespace DataFlow.Benchmarks;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class ParallelBenchmarks
{
    private List<ComputeData> _data;
    private Pipeline<ComputeData> _regularPipeline;
    private ParallelPipeline<ComputeData> _parallelPipeline2;
    private ParallelPipeline<ComputeData> _parallelPipeline4;
    private ParallelPipeline<ComputeData> _parallelPipeline8;

    [Params(1000, 10000, 50000)]
    public int DataSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = GenerateTestData(DataSize);
        _regularPipeline = new Pipeline<ComputeData>(_data);
        _parallelPipeline2 = new ParallelPipeline<ComputeData>(_regularPipeline, 2);
        _parallelPipeline4 = new ParallelPipeline<ComputeData>(_regularPipeline, 4);
        _parallelPipeline8 = new ParallelPipeline<ComputeData>(_regularPipeline, 8);
    }

    private List<ComputeData> GenerateTestData(int count)
    {
        var random = new Random(42);
        var data = new List<ComputeData>(count);
        
        for (int i = 0; i < count; i++)
        {
            data.Add(new ComputeData
            {
                Id = i,
                Value = random.Next(1, 1000),
                Factor = random.NextDouble() * 10,
                Category = random.Next(1, 11)
            });
        }
        
        return data;
    }

    private double HeavyComputation(ComputeData data)
    {
        double result = data.Value;
        for (int i = 0; i < 100; i++)
        {
            result = Math.Sqrt(result * data.Factor) + Math.Sin(result);
            result = Math.Cos(result) * Math.Log(Math.Abs(result) + 1);
        }
        return result;
    }

    [Benchmark(Baseline = true)]
    public double RegularPipelineCompute()
    {
        return _regularPipeline
            .Filter(x => x.Category > 5)
            .Map(x => HeavyComputation(x))
            .ToList()
            .Sum();
    }

    [Benchmark]
    public double ParallelPipeline2Threads()
    {
        return _parallelPipeline2
            .Filter(x => x.Category > 5)
            .Map(x => HeavyComputation(x))
            .ToList()
            .Sum();
    }

    [Benchmark]
    public double ParallelPipeline4Threads()
    {
        return _parallelPipeline4
            .Filter(x => x.Category > 5)
            .Map(x => HeavyComputation(x))
            .ToList()
            .Sum();
    }

    [Benchmark]
    public double ParallelPipeline8Threads()
    {
        return _parallelPipeline8
            .Filter(x => x.Category > 5)
            .Map(x => HeavyComputation(x))
            .ToList()
            .Sum();
    }

    [Benchmark]
    public double ParallelLinq()
    {
        return _data
            .AsParallel()
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .Where(x => x.Category > 5)
            .Select(x => HeavyComputation(x))
            .Sum();
    }

    [Benchmark]
    public void RegularForEach()
    {
        double sum = 0;
        _regularPipeline
            .Filter(x => x.Category > 5)
            .ForEach(x => sum += HeavyComputation(x));
    }

    [Benchmark]
    public void ParallelForEach4Threads()
    {
        double sum = 0;
        _parallelPipeline4
            .Filter(x => x.Category > 5)
            .ForEach(x => sum += HeavyComputation(x));
    }

    public class ComputeData
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public double Factor { get; set; }
        public int Category { get; set; }
    }
}