using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PipeFlow.Core.Api;
using PipeFlow.Core.Excel;
using PipeFlow.Core.Json;
using PipeFlow.Core.MongoDB;
using PipeFlow.Core.PostgreSQL;
using PipeFlow.Core.Sql;
using Microsoft.EntityFrameworkCore;

namespace PipeFlow.Core.Builder;

public class PipeFlowBuilder
{
  public static ISourceBuilder From => new SourceBuilder();
  
  public static IPipelineBuilder<DataRow> FromCsv(string filePath, Action<CsvReaderOptions> configure = null)
  {
    return new SourceBuilder().Csv(filePath, configure);
  }
  
  public static IPipelineBuilder<DataRow> FromJson(string filePath, Action<JsonReaderOptions> configure = null)
  {
    return new SourceBuilder().Json(filePath, configure);
  }
  
  public static IPipelineBuilder<DataRow> FromExcel(string filePath, Action<ExcelReaderOptions> configure = null)
  {
    return new SourceBuilder().Excel(filePath, configure);
  }
  
  public static IPipelineBuilder<DataRow> FromSql(string connectionString, string query, object parameters = null)
  {
    return new SourceBuilder().Sql(connectionString, query, parameters);
  }
  
  public static IPipelineBuilder<DataRow> FromApi(string url, Action<ApiReaderOptions> configure = null)
  {
    return new SourceBuilder().Api(url, configure);
  }
  
  public static IPipelineBuilder<DataRow> FromMongoDB(string connectionString, string database, string collection)
  {
    return new SourceBuilder().MongoDB(connectionString, database, collection);
  }
  
  public static IPipelineBuilder<DataRow> FromPostgreSql(string connectionString, string query, object parameters = null)
  {
    return new SourceBuilder().PostgreSql(connectionString, query, parameters);
  }
  
  public static IPipelineBuilder<T> FromQueryable<T>(IQueryable<T> queryable) where T : class
  {
    return new QueryablePipelineBuilder<T>(queryable);
  }
  
  public static IPipelineBuilder<T> FromCollection<T>(IEnumerable<T> collection)
  {
    return new PipelineBuilder<T>(collection);
  }
}

public interface ISourceBuilder
{
  IPipelineBuilder<DataRow> Csv(string filePath, Action<CsvReaderOptions> configure = null);
  IPipelineBuilder<DataRow> Json(string filePath, Action<JsonReaderOptions> configure = null);
  IPipelineBuilder<DataRow> Excel(string filePath, Action<ExcelReaderOptions> configure = null);
  IPipelineBuilder<DataRow> Sql(string connectionString, string query, object parameters = null);
  IPipelineBuilder<DataRow> Api(string url, Action<ApiReaderOptions> configure = null);
  IPipelineBuilder<DataRow> MongoDB(string connectionString, string database, string collection);
  IPipelineBuilder<DataRow> PostgreSql(string connectionString, string query, object parameters = null);
}

public interface ISourceBuilder<T> where T : class
{
  IPipelineBuilder<T> Queryable(IQueryable<T> queryable);
  IPipelineBuilder<T> Collection(IEnumerable<T> collection);
}

public class SourceBuilder : ISourceBuilder
{
  public IPipelineBuilder<DataRow> Csv(string filePath, Action<CsvReaderOptions> configure = null)
  {
    var options = new CsvReaderOptions();
    configure?.Invoke(options);
    
    var reader = new CsvReader(filePath);
    return new PipelineBuilder<DataRow>(reader.Read());
  }
  
  public IPipelineBuilder<DataRow> Json(string filePath, Action<JsonReaderOptions> configure = null)
  {
    var options = new JsonReaderOptions();
    configure?.Invoke(options);
    
    var reader = new JsonReader(filePath);
    return new PipelineBuilder<DataRow>(reader.Read());
  }
  
  public IPipelineBuilder<DataRow> Excel(string filePath, Action<ExcelReaderOptions> configure = null)
  {
    var options = new ExcelReaderOptions();
    configure?.Invoke(options);
    
    var reader = new ExcelReader(filePath);
    return new PipelineBuilder<DataRow>(reader.Read());
  }
  
  public IPipelineBuilder<DataRow> Sql(string connectionString, string query, object parameters = null)
  {
    var reader = new SqlReader(connectionString);
    reader.Query(query);
    return new PipelineBuilder<DataRow>(reader.Read());
  }
  
  public IPipelineBuilder<DataRow> Api(string url, Action<ApiReaderOptions> configure = null)
  {
    var options = new ApiReaderOptions();
    configure?.Invoke(options);
    
    var reader = new ApiReader(url);
    return new PipelineBuilder<DataRow>(reader.Read());
  }
  
  public IPipelineBuilder<DataRow> MongoDB(string connectionString, string database, string collection)
  {
    var reader = new MongoReader(connectionString, database, collection);
    return new PipelineBuilder<DataRow>(reader.Read());
  }
  
  public IPipelineBuilder<DataRow> PostgreSql(string connectionString, string query, object parameters = null)
  {
    var reader = new PostgreSqlReader(connectionString);
    
    if (parameters != null)
    {
      reader.Query(query, parameters);
    }
    else
    {
      reader.Query(query);
    }
    
    return new PipelineBuilder<DataRow>(reader.Read());
  }
}

public class SourceBuilder<T> : ISourceBuilder<T> where T : class
{
  public IPipelineBuilder<T> Queryable(IQueryable<T> queryable)
  {
    if (queryable == null)
      throw new ArgumentNullException(nameof(queryable));
    
    return new QueryablePipelineBuilder<T>(queryable);
  }
  
  public IPipelineBuilder<T> Collection(IEnumerable<T> collection)
  {
    if (collection == null)
      throw new ArgumentNullException(nameof(collection));
    
    return new PipelineBuilder<T>(collection);
  }
}

public class CsvReaderOptions
{
  public char Delimiter { get; set; } = ',';
  public bool HasHeaders { get; set; } = true;
  public string Encoding { get; set; } = "UTF-8";
  public int BufferSize { get; set; } = 8192;
}

public class JsonReaderOptions
{
  public string RootPath { get; set; }
  public bool CamelCase { get; set; } = false;
}

public class ExcelReaderOptions
{
  public string SheetName { get; set; }
  public int SheetIndex { get; set; } = 0;
  public bool HasHeaders { get; set; } = true;
}

public class ApiReaderOptions
{
  public string AuthToken { get; set; }
  public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
  public int Timeout { get; set; } = 30000;
  public int RetryCount { get; set; } = 3;
}