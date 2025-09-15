using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PipeFlow.Core.Builder;

namespace PipeFlow.Examples;

public class ModernApiUsage
{
  public static async Task BasicUsageAsync()
  {
    var cts = new CancellationTokenSource();
    
    var pipeline = PipeFlowBuilder
      .FromCsv("input.csv", options =>
      {
        options.Delimiter = ',';
        options.HasHeaders = true;
      })
      .Filter(row => row.GetValue<decimal>("Amount") > 1000)
      .Map(row => new
      {
        Product = row["ProductName"],
        Revenue = row.GetValue<decimal>("Amount") * row.GetValue<int>("Quantity")
      })
      .Build();
    
    var result = await pipeline.ExecuteAsync(cts.Token);
    
    if (result.Success)
    {
      Console.WriteLine($"Processed {result.ProcessedCount} records in {result.ExecutionTime}");
    }
    else
    {
      Console.WriteLine($"Pipeline failed: {string.Join(", ", result.Errors)}");
    }
  }
  
  public static async Task ConsistentNamingAsync()
  {
    var cts = new CancellationTokenSource();
    
    await PipeFlowBuilder
      .FromJson("data.json")
      .Filter(row => row["active"] == "true")
      .ToExcelAsync("output.xlsx", options =>
      {
        options.SheetName = "Active Records";
        options.AutoFitColumns = true;
      }, cts.Token);
    
    await PipeFlowBuilder
      .FromSql("connection_string", "SELECT * FROM Orders WHERE Status = @status", new { status = "Pending" })
      .ToCsvAsync("pending_orders.csv", cancellationToken: cts.Token);
  }
  
  public static async Task EntityFrameworkUsageAsync(MyDbContext context)
  {
    var cts = new CancellationTokenSource();
    
    await PipeFlowBuilder
      .FromQueryable(context.Customers.Where(c => c.IsActive))
      .WithPaging(pageSize: 500)
      .Map(c => new Supplier
      {
        Name = c.CompanyName,
        ContactEmail = c.Email,
        CreatedDate = DateTime.UtcNow
      })
      .ToEntityFrameworkAsync(context, options =>
      {
        options.UpsertPredicate = supplier => s => s.ContactEmail == supplier.ContactEmail;
        options.BatchSize = 100;
        options.UseTransaction = true;
      }, cts.Token);
    
    var exportPipeline = PipeFlowBuilder
      .FromQueryable(context.Orders.Include(o => o.OrderItems))
      .Filter(o => o.OrderDate >= DateTime.Today.AddDays(-30))
      .Map(o => new
      {
        OrderId = o.Id,
        CustomerName = o.Customer.Name,
        TotalAmount = o.OrderItems.Sum(i => i.Price * i.Quantity),
        ItemCount = o.OrderItems.Count()
      })
      .Build();
    
    await exportPipeline.StreamAsync(cts.Token)
      .ToListAsync(cancellationToken: cts.Token);
  }
  
  public static async Task StreamingUsageAsync()
  {
    var cts = new CancellationTokenSource();
    
    var pipeline = PipeFlowBuilder
      .FromCsv("large_file.csv")
      .AsParallel(maxDegreeOfParallelism: 8)
      .Filter(row => !string.IsNullOrEmpty(row["Email"]?.ToString()))
      .Build();
    
    var processedCount = 0;
    await foreach (var item in pipeline.StreamAsync(cts.Token))
    {
      Console.WriteLine($"Processing: {item["Email"]}");
      processedCount++;
      
      if (processedCount % 1000 == 0)
      {
        Console.WriteLine($"Processed {processedCount} records...");
      }
    }
  }
  
  public static async Task ComplexPipelineAsync()
  {
    var cts = new CancellationTokenSource();
    
    var analysisResult = await PipeFlowBuilder
      .FromApi("https://api.example.com/data", options =>
      {
        options.AuthToken = "bearer-token";
        options.RetryCount = 3;
      })
      .Filter(item => item["status"] == "completed")
      .Map(item => new
      {
        Id = item["id"],
        Amount = Convert.ToDecimal(item["amount"]),
        Date = DateTime.Parse(item["date"].ToString())
      })
      .OrderByDescending(x => x.Amount)
      .Take(100)
      .Build()
      .ExecuteAsync(cts.Token);
    
    if (analysisResult.Success)
    {
      await PipeFlowBuilder
        .FromCollection(analysisResult.Data)
        .ToJsonAsync("top_100_transactions.json", options =>
        {
          options.Indented = true;
          options.CamelCase = true;
        }, cts.Token);
    }
  }
  
  public static async Task BatchProcessingAsync()
  {
    var cts = new CancellationTokenSource();
    
    await PipeFlowBuilder
      .FromMongoDB("mongodb://localhost", "mydb", "products")
      .WithBatchSize(500)
      .Filter(row => row.GetValue<bool>("inStock"))
      .ToApiAsync("https://api.example.com/import", options =>
      {
        options.BatchSize = 50;
        options.AuthToken = "api-key";
        options.Headers["X-Custom-Header"] = "value";
      }, cts.Token);
  }
}

public class MyDbContext : DbContext
{
  public DbSet<Customer> Customers { get; set; }
  public DbSet<Order> Orders { get; set; }
  public DbSet<Supplier> Suppliers { get; set; }
}

public class Customer
{
  public int Id { get; set; }
  public string CompanyName { get; set; }
  public string Email { get; set; }
  public bool IsActive { get; set; }
}

public class Order
{
  public int Id { get; set; }
  public DateTime OrderDate { get; set; }
  public Customer Customer { get; set; }
  public List<OrderItem> OrderItems { get; set; }
}

public class OrderItem
{
  public int Id { get; set; }
  public decimal Price { get; set; }
  public int Quantity { get; set; }
}

public class Supplier
{
  public int Id { get; set; }
  public string Name { get; set; }
  public string ContactEmail { get; set; }
  public DateTime CreatedDate { get; set; }
}