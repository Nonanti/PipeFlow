# PipeFlow

[![NuGet](https://img.shields.io/nuget/v/PipeFlow.Core.svg)](https://www.nuget.org/packages/PipeFlow.Core)
[![Build Status](https://github.com/Nonanti/PipeFlow/actions/workflows/build.yml/badge.svg)](https://github.com/Nonanti/PipeFlow/actions)

A .NET library for ETL operations. Process CSV, JSON, Excel, SQL and MongoDB data with a simple fluent API.

## Installation

```bash
dotnet add package PipeFlowCore
```

## Quick Start

```csharp
// Basic CSV processing
PipeFlow.From.Csv("input.csv")
    .Filter(row => row["Status"] == "Active")
    .WriteToCsv("output.csv");

// SQL to Excel export
PipeFlow.From.Sql(connectionString)
    .Query("SELECT * FROM Orders WHERE Date > @date", new { date = DateTime.Today })
    .WriteToExcel("orders.xlsx");
```

## Supported Data Sources

PipeFlow can read from and write to:
- CSV files
- JSON files
- Excel files (xlsx)
- SQL Server databases
- MongoDB collections
- REST APIs
- In-memory collections

## Examples

### CSV Processing

```csharp
// Read CSV, transform, and save
PipeFlow.From.Csv("sales.csv")
    .Filter(row => row.GetValue<decimal>("Amount") > 1000)
    .Map(row => new {
        Product = row["ProductName"],
        Revenue = row.GetValue<decimal>("Amount") * row.GetValue<int>("Quantity")
    })
    .WriteToCsv("high_value_sales.csv");
```

### Database Operations

```csharp
// Export from SQL Server
PipeFlow.From.Sql(connectionString)
    .Query("SELECT * FROM Products WHERE InStock = 1")
    .WriteToJson("products.json");

// Import to SQL Server
PipeFlow.From.Excel("inventory.xlsx")
    .WriteToSql(connectionString, "Inventory");
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

// Send to API
PipeFlow.From.Csv("upload.csv")
    .ToApi("https://api.example.com/import", api => api
        .WithAuth("api-key")
        .WithBatchSize(100));
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
PipeFlow.From.Csv("large_file.csv")
    .AsParallel(maxDegreeOfParallelism: 8)
    .Map(row => {
        // CPU intensive operation
        row["Hash"] = ComputeHash(row["Data"]);
        return row;
    })
    .WriteToCsv("processed.csv");
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

## Contributing

Pull requests are welcome. Please make sure to update tests as appropriate.

## License

MIT