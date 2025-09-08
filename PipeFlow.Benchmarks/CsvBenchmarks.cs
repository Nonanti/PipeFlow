using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using PipeFlow.Core;
using System.Text;

namespace PipeFlow.Benchmarks;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class CsvBenchmarks
{
    private string _csvFilePath;
    private string _largeCsvFilePath;

    [Params(1000, 10000, 50000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _csvFilePath = Path.Combine(Path.GetTempPath(), "benchmark_data.csv");
        _largeCsvFilePath = Path.Combine(Path.GetTempPath(), "benchmark_large_data.csv");
        
        GenerateCsvFile(_csvFilePath, RowCount);
        GenerateLargeCsvFile(_largeCsvFilePath, RowCount);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_csvFilePath))
            File.Delete(_csvFilePath);
        if (File.Exists(_largeCsvFilePath))
            File.Delete(_largeCsvFilePath);
    }

    private void GenerateCsvFile(string path, int rows)
    {
        var random = new Random(42);
        using var writer = new StreamWriter(path);
        
        writer.WriteLine("Id,Name,Age,Salary,Department,Email,City,IsActive");
        
        for (int i = 0; i < rows; i++)
        {
            writer.WriteLine($"{i},Person_{i},{random.Next(18, 80)},{random.Next(30000, 150000)}," +
                           $"Dept_{random.Next(1, 11)},person{i}@example.com,City_{random.Next(1, 51)}," +
                           $"{(random.Next(2) == 1 ? "true" : "false")}");
        }
    }

    private void GenerateLargeCsvFile(string path, int rows)
    {
        var random = new Random(42);
        using var writer = new StreamWriter(path);
        
        writer.WriteLine("Id,Name,Age,Salary,Department,Email,City,IsActive,Description,Notes,Tags,Category,Status,Priority,CreatedDate,ModifiedDate");
        
        for (int i = 0; i < rows; i++)
        {
            var description = GenerateRandomText(random, 50);
            var notes = GenerateRandomText(random, 30);
            var tags = string.Join(",", Enumerable.Range(0, random.Next(3, 8)).Select(x => $"tag{x}"));
            
            writer.WriteLine($"{i},Person_{i},{random.Next(18, 80)},{random.Next(30000, 150000)}," +
                           $"Dept_{random.Next(1, 11)},person{i}@example.com,City_{random.Next(1, 51)}," +
                           $"{(random.Next(2) == 1 ? "true" : "false")},\"{description}\",\"{notes}\"," +
                           $"\"{tags}\",Category_{random.Next(1, 6)},Status_{random.Next(1, 4)}," +
                           $"Priority_{random.Next(1, 4)},2024-01-{random.Next(1, 29):00},2024-08-{random.Next(1, 29):00}");
        }
    }

    private string GenerateRandomText(Random random, int wordCount)
    {
        var words = new[] { "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", 
                            "elit", "sed", "do", "eiusmod", "tempor", "incididunt", "labore" };
        var result = new StringBuilder();
        
        for (int i = 0; i < wordCount; i++)
        {
            if (i > 0) result.Append(' ');
            result.Append(words[random.Next(words.Length)]);
        }
        
        return result.ToString();
    }

    [Benchmark(Baseline = true)]
    public int ReadCsvWithStreamReader()
    {
        var count = 0;
        using var reader = new StreamReader(_csvFilePath);
        
        var header = reader.ReadLine();
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            var parts = line.Split(',');
            if (int.Parse(parts[2]) > 30)
                count++;
        }
        
        return count;
    }

    [Benchmark]
    public int ReadCsvWithPipeFlow()
    {
        var csvReader = new CsvReader(_csvFilePath);
        var pipeline = PipeFlow.Core.PipeFlow.From.Csv(_csvFilePath);
        
        return pipeline
            .Filter(row => row.GetValue<int>("Age") > 30)
            .Count();
    }

    [Benchmark]
    public List<DataRow> TransformCsvWithPipeFlow()
    {
        var csvReader = new CsvReader(_csvFilePath);
        var pipeline = PipeFlow.Core.PipeFlow.From.Csv(_csvFilePath);
        
        return pipeline
            .Filter(row => row.GetValue<int>("Age") > 30)
            .Filter(row => row.GetValue<int>("Salary") > 50000)
            .OrderBy(row => row["Name"])
            .Take(100)
            .ToList();
    }

    [Benchmark]
    public int LargeCsvProcessing()
    {
        var csvReader = new CsvReader(_largeCsvFilePath);
        var pipeline = PipeFlow.Core.PipeFlow.From.Csv(_largeCsvFilePath);
        
        return pipeline
            .Filter(row => row.GetValue<int>("Age") > 25)
            .Filter(row => row.GetValue<bool>("IsActive"))
            .Map(row => new
            {
                Id = row.GetValue<int>("Id"),
                Name = row["Name"],
                Salary = row.GetValue<int>("Salary"),
                Category = row["Category"]
            })
            .Where(x => x.Salary > 40000)
            .Count();
    }

    [Benchmark]
    public void WriteCsvWithPipeFlow()
    {
        var data = new List<DataRow>();
        var random = new Random(42);
        
        for (int i = 0; i < 1000; i++)
        {
            var row = new DataRow();
            row["Id"] = i;
            row["Value"] = random.Next(1, 1000);
            row["Category"] = $"Cat_{random.Next(1, 6)}";
            data.Add(row);
        }

        var outputPath = Path.Combine(Path.GetTempPath(), "benchmark_output.csv");
        var csvWriter = new CsvWriter(outputPath);
        
        var pipeline = new Pipeline<DataRow>(data);
        csvWriter.Write(pipeline.Execute());
        
        if (File.Exists(outputPath))
            File.Delete(outputPath);
    }
}