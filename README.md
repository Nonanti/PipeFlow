# DataFlow

[![NuGet](https://img.shields.io/nuget/v/DataFlow.Core.svg)](https://www.nuget.org/packages/DataFlow.Core)
[![Downloads](https://img.shields.io/nuget/dt/DataFlow.Core.svg)](https://www.nuget.org/packages/DataFlow.Core)
[![Build Status](https://github.com/Nonanti/DataFlow/actions/workflows/build.yml/badge.svg)](https://github.com/Nonanti/DataFlow/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

High-performance ETL pipeline library for .NET that actually gets out of your way.

## What is this?

DataFlow is a streaming data pipeline library designed for processing large datasets without blowing up your memory. Think LINQ but for ETL operations - read data from anywhere, transform it, and write it somewhere else. No XML configs, no enterprise architect nonsense. Just simple, chainable operations that work.

## Installation

```bash
dotnet add package DataFlow.Core
```

Or grab it from NuGet if you're using Visual Studio.

## Basic Usage

Process a CSV file in three lines:

```csharp
DataFlow.From.Csv("input.csv")
    .Filter(row => row["Status"] == "Active")
    .WriteToCsv("output.csv");
```

That's the entire API philosophy. Read, transform, write.

## Common Scenarios

### Reading Data

```csharp
// CSV files
var pipeline = DataFlow.From.Csv("data.csv");

// JSON files  
var pipeline = DataFlow.From.Json("data.json");

// Excel spreadsheets
var pipeline = DataFlow.From.Excel("report.xlsx", sheet: "Sales");

// SQL databases
var pipeline = DataFlow.From.Sql(connectionString)
    .Query("SELECT * FROM Orders WHERE Created > @date", new { date = DateTime.Today });

// In-memory collections
var pipeline = DataFlow.From.Collection(myList);

// REST APIs
var pipeline = DataFlow.From.Api("https://api.example.com/data");
```

### Transforming Data

Chain operations like you would with LINQ:

```csharp
pipeline
    .Filter(row => row.GetValue<decimal>("Price") > 0)
    .Map(row => new {
        Product = row["Name"],
        Revenue = row.GetValue<decimal>("Price") * row.GetValue<int>("Quantity"),
        Category = row["Category"]
    })
    .GroupBy(x => x.Category)
    .Select(group => new {
        Category = group.Key,
        TotalRevenue = group.Sum(x => x.Revenue),
        ProductCount = group.Count()
    })
    .OrderByDescending(x => x.TotalRevenue)
    .Take(10);
```

### Writing Results

```csharp
// To CSV
pipeline.WriteToCsv("output.csv");

// To JSON
pipeline.WriteToJson("output.json");

// To Excel
pipeline.WriteToExcel("report.xlsx", "Results");

// To SQL
pipeline.WriteToSql(connectionString, "TargetTable");

// To collection
var results = pipeline.ToList();
var array = pipeline.ToArray();

// To REST API
pipeline.ToApi("https://api.example.com/import");
```

## Real World Examples

### ETL: Database to CSV

Export active customers with their order totals:

```csharp
DataFlow.From.Sql(connectionString)
    .Query(@"
        SELECT c.*, COUNT(o.Id) as OrderCount, SUM(o.Total) as TotalSpent 
        FROM Customers c
        LEFT JOIN Orders o ON c.Id = o.CustomerId
        WHERE c.IsActive = 1
        GROUP BY c.Id")
    .Map(row => new {
        CustomerId = row["Id"],
        Name = row["Name"],
        Email = row["Email"],
        OrderCount = row["OrderCount"],
        TotalSpent = row["TotalSpent"],
        CustomerValue = row.GetValue<int>("OrderCount") > 10 ? "High" : "Normal"
    })
    .OrderByDescending(x => x.TotalSpent)
    .WriteToCsv("customer_report.csv");
```

### Data Cleaning

Clean messy CSV data:

```csharp
DataFlow.From.Csv("raw_data.csv")
    .RemoveDuplicates("Id")
    .FillMissing("Email", "no-email@unknown.com")
    .FillMissing("Country", "USA")
    .Map(row => {
        row["Email"] = row["Email"].ToString().ToLower().Trim();
        row["Phone"] = Regex.Replace(row["Phone"].ToString(), @"[^\d]", "");
        return row;
    })
    .Filter(row => IsValidEmail(row["Email"].ToString()))
    .WriteToCsv("cleaned_data.csv");
```

### Parallel Processing

Speed up CPU-intensive operations:

```csharp
DataFlow.From.Csv("large_dataset.csv")
    .AsParallel(maxDegreeOfParallelism: Environment.ProcessorCount)
    .Map(row => {
        // Some expensive operation
        row["Hash"] = ComputeExpensiveHash(row["Data"]);
        row["Processed"] = true;
        return row;
    })
    .WriteToCsv("processed.csv");
```

### Data Validation

Validate and handle errors:

```csharp
var validator = new DataValidator()
    .Required("Id", "Name", "Email")
    .Email("Email")
    .Range("Age", min: 0, max: 150)
    .Regex("Phone", @"^\d{10}$")
    .Custom("StartDate", value => DateTime.Parse(value) <= DateTime.Now);

DataFlow.From.Csv("users.csv")
    .Validate(validator)
    .OnInvalid(ErrorStrategy.LogAndSkip)  // or ThrowException, Fix, Collect
    .WriteToCsv("valid_users.csv");

// Access validation errors
var errors = pipeline.ValidationErrors;
```

### Streaming Large Files

Process multi-gigabyte files with constant memory usage:

```csharp
DataFlow.From.Csv("10gb_log_file.csv")
    .Filter(row => row["Level"] == "ERROR")
    .Select(row => new {
        Timestamp = row["Timestamp"],
        Message = row["Message"],
        Source = row["Source"]
    })
    .WriteToCsv("errors_only.csv");  // Streams directly, uses ~50MB RAM
```

### Working with REST APIs

Fetch data from APIs with pagination and authentication:

```csharp
// Read from API with pagination
DataFlow.From.Api("https://api.github.com/users/microsoft/repos", api => api
    .WithAuth("your-github-token")
    .WithPagination(pageSize: 30)
    .WithRetry(maxRetries: 3, TimeSpan.FromSeconds(2)))
    .Filter(repo => repo.GetValue<int>("stargazers_count") > 1000)
    .OrderByDescending(repo => repo["stargazers_count"])
    .ToCsv("popular_microsoft_repos.csv");

// Send data to API endpoint
DataFlow.From.Csv("products.csv")
    .Map(row => new {
        name = row["ProductName"],
        price = row.GetValue<decimal>("Price"),
        category = row["Category"]
    })
    .ToApi("https://api.store.com/products", writer => writer
        .WithAuth("api-key-here")
        .WithBatchSize(50)
        .WithMethod(HttpMethod.Post));

// ETL: API to Database
DataFlow.From.Api("https://jsonplaceholder.typicode.com/users")
    .Map(row => new {
        Id = row["id"],
        Username = row["username"],
        Email = row["email"],
        Company = row.GetValue<Dictionary<string, object>>("company")["name"]
    })
    .ToSql(connectionString, "Users");
```

## Advanced Features

### Custom Data Sources

Implement `IDataSource` for custom sources:

```csharp
public class MongoDataSource : IDataSource
{
    public IEnumerable<DataRow> Read()
    {
        // Your MongoDB reading logic
        foreach (var doc in collection.Find(filter))
        {
            yield return new DataRow(doc.ToDictionary());
        }
    }
}

// Use it
var pipeline = DataFlow.From.Custom(new MongoDataSource());
```

### Custom Transformations

Create reusable transformations:

```csharp
public static class MyTransformations
{
    public static IPipeline<T> NormalizePhoneNumbers<T>(this IPipeline<T> pipeline)
    {
        return pipeline.Map(row => {
            if (row.ContainsKey("Phone"))
            {
                row["Phone"] = NormalizePhone(row["Phone"].ToString());
            }
            return row;
        });
    }
}

// Use it
pipeline.NormalizePhoneNumbers().WriteToCsv("output.csv");
```

### Progress Tracking

Monitor long-running operations:

```csharp
var progress = new Progress<int>(percent => 
    Console.WriteLine($"Processing: {percent}%"));

DataFlow.From.Csv("large_file.csv")
    .WithProgress(progress)
    .Filter(row => ComplexFilter(row))
    .WriteToCsv("filtered.csv");
```

## Performance

Benchmarks on 1M records (Intel i7, 16GB RAM):

| Operation | Memory Usage | Time | Records/sec |
|-----------|-------------|------|-------------|
| CSV Read + Filter + Write | 42 MB | 3.2s | 312,500 |
| JSON Parse + Transform | 156 MB | 5.1s | 196,078 |
| SQL Read + Group + Export | 89 MB | 4.7s | 212,765 |
| Parallel Transform (8 cores) | 203 MB | 1.4s | 714,285 |

Memory usage stays constant regardless of file size when streaming.

## Configuration

Global settings via `DataFlowConfig`:

```csharp
DataFlowConfig.Configure(config => {
    config.DefaultCsvDelimiter = ';';
    config.DefaultDateFormat = "yyyy-MM-dd";
    config.BufferSize = 8192;
    config.EnableAutoTypeConversion = true;
    config.ThrowOnMissingColumns = false;
});
```
## License

MIT - Do whatever you want with it.

---

Built because I got tired of writing the same ETL code over and over.
