# PipeFlow

[![NuGet](https://img.shields.io/nuget/v/PipeFlowCore.svg)](https://www.nuget.org/packages/PipeFlowCore)
[![Build Status](https://github.com/Nonanti/PipeFlow/actions/workflows/build.yml/badge.svg)](https://github.com/Nonanti/PipeFlow/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-6.0%2B-512BD4)](https://dotnet.microsoft.com/download)

A modern, high-performance ETL pipeline library for .NET with builder pattern, async/await support, and Entity Framework integration. Process CSV, JSON, Excel, SQL and MongoDB data with minimal memory usage and maximum performance.

## Installation

```bash
dotnet add package PipeFlowCore
```

## Quick Start

```csharp
// Modern builder pattern with lazy execution
var pipeline = PipeFlowBuilder
    .FromCsv("input.csv")
    .Filter(row => row["Status"] == "Active")
    .Build();

// Execute with async/await and CancellationToken
var result = await pipeline.ExecuteAsync(cancellationToken);

// Consistent From/To naming
await PipeFlowBuilder
    .FromSql(connectionString, "SELECT * FROM Orders")
    .ToExcelAsync("orders.xlsx", cancellationToken);
```

## Supported Data Sources

PipeFlow can read from and write to:
- CSV files
- JSON files
- Excel files (xlsx)
- SQL Server databases
- PostgreSQL databases (NEW)
- MongoDB collections
- REST APIs
- In-memory collections

## Key Features

- **Builder Pattern**: Build complex pipelines without immediate execution
- **Async/Await**: Full async support with CancellationToken
- **Entity Framework Integration**: Direct support for IQueryable and EF Core
- **Lazy Execution**: Pipelines execute only when explicitly called
- **Consistent API**: From/To pattern for all data sources
- **Memory Efficient**: Streaming support for large datasets
- **Parallel Processing**: Built-in support for parallel execution
- **Type Safe**: Strong typing with generics support
- **Extensible**: Easy to add custom data sources and destinations

## Examples

### CSV Processing with Builder Pattern

```csharp
// Build pipeline (not executed yet)
var pipeline = PipeFlowBuilder
    .FromCsv("sales.csv")
    .Filter(row => row.GetValue<decimal>("Amount") > 1000)
    .Map(row => new {
        Product = row["ProductName"],
        Revenue = row.GetValue<decimal>("Amount") * row.GetValue<int>("Quantity")
    })
    .Build();

// Execute when ready
var result = await pipeline.ExecuteAsync();
if (result.Success)
{
    await PipeFlowBuilder
        .FromCollection(result.Data)
        .ToCsvAsync("high_value_sales.csv");
}
```

### Database Operations with Async Support

```csharp
// Export from SQL Server with async
await PipeFlowBuilder
    .FromSql(connectionString, "SELECT * FROM Products WHERE InStock = 1")
    .ToJsonAsync("products.json", cancellationToken);

// Import to SQL Server
await PipeFlowBuilder
    .FromExcel("inventory.xlsx")
    .ToSqlAsync(connectionString, "Inventory", options =>
    {
        options.UseBulkInsert = true;
        options.BatchSize = 1000;
    }, cancellationToken);
```

### PostgreSQL Support

```csharp
// Read from PostgreSQL
var pipeline = PipeFlowBuilder
    .FromPostgreSql(connectionString, "SELECT * FROM products WHERE price > @minPrice", 
        new { minPrice = 100 })
    .Filter(row => row["in_stock"] == true)
    .Build();

// Write to PostgreSQL with upsert
await PipeFlowBuilder
    .FromCsv("products.csv")
    .ToPostgreSqlAsync(connectionString, "products", options =>
    {
        options.CreateTableIfNotExists = true;
        options.OnConflictUpdate("product_id");
        options.UseBulkInsert = true;
    }, cancellationToken);
```

### Entity Framework Integration

```csharp
// Read from Entity Framework with paging
await PipeFlowBuilder
    .FromQueryable(context.Customers.Where(c => c.IsActive))
    .WithPaging(pageSize: 500)
    .Map(c => new SupplierDto
    {
        Name = c.CompanyName,
        Email = c.Email
    })
    .ToEntityFrameworkAsync(context, options =>
    {
        options.UpsertPredicate = s => x => x.Email == s.Email;
        options.BatchSize = 100;
        options.UseTransaction = true;
    }, cancellationToken);
```

### MongoDB Integration

```csharp
// Query MongoDB
PipeFlow.From.MongoDB("mongodb://localhost", "store", "products")
    .Where("category", "Electronics")
    .Sort("price", ascending: false)
    .WriteToCsv("electronics.csv");

// Update MongoDB from CSV
PipeFlow.From.Csv("product_updates.csv")
    .ToMongoDB("mongodb://localhost", "store", "products", writer => writer
        .WithUpsert("sku")
        .WithBatchSize(500));
```

### REST API Support

```csharp
// Fetch from API
PipeFlow.From.Api("https://api.example.com/data")
    .Filter(item => item["active"] == true)
    .WriteToJson("active_items.json");

// Send to API with new builder pattern
await PipeFlowBuilder
    .FromCsv("upload.csv")
    .ToApiAsync("https://api.example.com/import", options =>
    {
        options.AuthToken = "api-key";
        options.BatchSize = 100;
    }, cancellationToken);
```

### Data Transformations

```csharp
// Complex transformations
PipeFlow.From.Csv("raw_data.csv")
    .RemoveDuplicates("Id")
    .FillMissing("Email", "unknown@example.com")
    .Map(row => {
        row["Email"] = row["Email"].ToString().ToLower();
        row["FullName"] = $"{row["FirstName"]} {row["LastName"]}";
        return row;
    })
    .GroupBy(row => row["Department"])
    .Select(group => new {
        Department = group.Key,
        EmployeeCount = group.Count(),
        AverageSalary = group.Average(r => r.GetValue<decimal>("Salary"))
    })
    .WriteToJson("department_summary.json");
```

### Parallel Processing

For better performance with large datasets:

```csharp
var pipeline = PipeFlowBuilder
    .FromCsv("large_file.csv")
    .AsParallel(maxDegreeOfParallelism: 8)
    .Map(row => {
        row["Hash"] = ComputeHash(row["Data"]);
        return row;
    })
    .Build();

var result = await pipeline.ExecuteAsync(cancellationToken);
await PipeFlowBuilder
    .FromCollection(result.Data)
    .ToCsvAsync("processed.csv", cancellationToken);
```

### Data Validation

```csharp
var validator = new DataValidator()
    .Required("Id", "Email")
    .Email("Email")
    .Range("Age", min: 0, max: 120);

PipeFlow.From.Csv("users.csv")
    .Validate(validator)
    .OnInvalid(ErrorStrategy.LogAndSkip)
    .WriteToCsv("valid_users.csv");
```

## Performance

PipeFlow uses streaming to process data efficiently. Memory usage remains constant regardless of file size.

Benchmark results (1M records):
- CSV read/write: ~3 seconds
- JSON processing: ~5 seconds
- SQL operations: ~4 seconds
- Parallel processing: ~1.5 seconds (8 cores)

## Configuration

```csharp
PipeFlowConfig.Configure(config => {
    config.DefaultCsvDelimiter = ',';
    config.BufferSize = 8192;
    config.ThrowOnMissingColumns = false;
});
```

## Requirements

- .NET 6.0 or later
- SQL Server 2012+ (for SQL features)
- MongoDB 4.0+ (for MongoDB features)

## Benchmarks

PipeFlow is optimized for performance:

| Operation | Records | Time | Memory |
|-----------|---------|------|--------|
| CSV Read | 1M | ~3s | <50MB |
| Parallel Processing | 1M | ~1.5s | <100MB |
| Streaming | 10M | ~30s | <50MB |

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history and release notes.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

**Berkant** - [GitHub](https://github.com/Nonanti)
