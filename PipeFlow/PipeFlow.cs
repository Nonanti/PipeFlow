using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PipeFlow.Core.Json;
using PipeFlow.Core.Sql;
using PipeFlow.Core.Excel;
using PipeFlow.Core.Api;
using PipeFlow.Core.MongoDB;
using PipeFlow.Core.Cloud;
using PipeFlow.Core.Parallel;
using PipeFlow.Core.Validation;
using Amazon;

namespace PipeFlow.Core;

public static class PipeFlow
{
    public static PipeFlowBuilder From => new PipeFlowBuilder();

    public class PipeFlowBuilder
    {
    public IPipeline<DataRow> Csv(string filePath)
    {
        var reader = new CsvReader(filePath);
        return new Pipeline<DataRow>(reader.Read());
    }

    public IPipeline<DataRow> Csv(string filePath, Action<CsvReader> configure)
    {
        var reader = new CsvReader(filePath);
        configure?.Invoke(reader);
        return new Pipeline<DataRow>(reader.Read());
    }

    public IAsyncEnumerable<DataRow> CsvAsync(string filePath)
    {
        var reader = new CsvReaderAsync(filePath);
        return reader.ReadAsync();
    }

    public IAsyncEnumerable<DataRow> CsvAsync(string filePath, Action<CsvReaderAsync> configure)
    {
        var reader = new CsvReaderAsync(filePath);
        configure?.Invoke(reader);
        return reader.ReadAsync();
    }

    public IPipeline<T> Collection<T>(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));
        
        return new Pipeline<T>(items);
    }

    public IPipeline<DataRow> DataRows(IEnumerable<DataRow> rows)
    {
        if (rows == null)
            throw new ArgumentNullException(nameof(rows));
        
        return new Pipeline<DataRow>(rows);
    }

    public IPipeline<DataRow> Json(string filePath)
    {
        var reader = new JsonReader(filePath);
        return new Pipeline<DataRow>(reader.Read());
    }

    public IPipeline<DataRow> Json(string filePath, Action<JsonReader> configure)
    {
        var reader = new JsonReader(filePath);
        configure?.Invoke(reader);
        return new Pipeline<DataRow>(reader.Read());
    }

    public IPipeline<DataRow> Sql(string connectionString, string query)
    {
        var reader = new SqlReader(connectionString).Query(query);
        return new Pipeline<DataRow>(reader.Read());
    }

    public IPipeline<DataRow> Sql(string connectionString, Action<SqlReader> configure)
    {
        var reader = new SqlReader(connectionString);
        configure?.Invoke(reader);
        return new Pipeline<DataRow>(reader.Read());
    }

    public IPipeline<DataRow> Excel(string filePath)
    {
        var reader = new ExcelReader(filePath);
        return new Pipeline<DataRow>(reader.Read());
    }

    public IPipeline<DataRow> Excel(string filePath, Action<ExcelReader> configure)
    {
        var reader = new ExcelReader(filePath);
        configure?.Invoke(reader);
        return new Pipeline<DataRow>(reader.Read());
    }

    public IPipeline<DataRow> Api(string url)
    {
        var reader = new ApiReader(url);
        return new Pipeline<DataRow>(reader.Read());
    }

    public IPipeline<DataRow> Api(string url, Action<ApiReader> configure)
    {
        var reader = new ApiReader(url);
        configure?.Invoke(reader);
        return new Pipeline<DataRow>(reader.Read());
    }

    public IPipeline<DataRow> MongoDB(string connectionString, string database, string collection)
    {
        var reader = new MongoReader(connectionString, database, collection);
        return new Pipeline<DataRow>(reader.Read());
    }

    public IPipeline<DataRow> MongoDB(string connectionString, string database, string collection, Action<MongoReader> configure)
    {
        var reader = new MongoReader(connectionString, database, collection);
        configure?.Invoke(reader);
        return new Pipeline<DataRow>(reader.Read());
    }

    public async Task<IPipeline<DataRow>> S3Csv(string bucketName, string key, string region = "us-east-1")
    {
        var tempFile = Path.GetTempFileName();
        var s3Reader = new S3Reader(bucketName, key)
            .WithRegion(RegionEndpoint.GetBySystemName(region));
        
        await s3Reader.DownloadToFileAsync(tempFile);
        
        var csvReader = new CsvReader(tempFile);
        return new Pipeline<DataRow>(csvReader.Read());
    }

    public async Task<IPipeline<DataRow>> AzureBlobCsv(string connectionString, string containerName, string blobName)
    {
        var tempFile = Path.GetTempFileName();
        var azureReader = new AzureBlobReader(connectionString, containerName, blobName);
        
        await azureReader.DownloadToFileAsync(tempFile);
        
        var csvReader = new CsvReader(tempFile);
        return new Pipeline<DataRow>(csvReader.Read());
    }

    public async Task<IPipeline<DataRow>> GoogleCloudCsv(string bucketName, string objectName)
    {
        var tempFile = Path.GetTempFileName();
        var gcsReader = new GoogleCloudStorageReader(bucketName, objectName);
        
        await gcsReader.DownloadToFileAsync(tempFile);
        
        var csvReader = new CsvReader(tempFile);
        return new Pipeline<DataRow>(csvReader.Read());
    }
    }

}

public static class PipelineExtensions
{
    public static IPipeline<T> Parallel<T>(this IPipeline<T> pipeline, int maxDegreeOfParallelism = -1)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        return new ParallelPipeline<T>(pipeline, maxDegreeOfParallelism);
    }

    public static IPipeline<T> Batch<T>(this IPipeline<T> pipeline, int batchSize)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than zero", nameof(batchSize));
        
        var batched = pipeline.Execute().Chunk(batchSize);
        return new Pipeline<IEnumerable<T>>(batched).SelectMany(batch => batch);
    }
    
    public static IPipeline<DataRow> RemoveDuplicates(this IPipeline<DataRow> pipeline, string keyColumn)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        if (string.IsNullOrWhiteSpace(keyColumn))
            throw new ArgumentNullException(nameof(keyColumn));

        var seen = new HashSet<object>();
        return pipeline.Filter(row =>
        {
            var key = row[keyColumn];
            return seen.Add(key);
        });
    }

    public static IPipeline<DataRow> FillMissing(this IPipeline<DataRow> pipeline, string column, object defaultValue)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentNullException(nameof(column));

        return pipeline.Map(row =>
        {
            if (!row.ContainsColumn(column) || row[column] == null)
            {
                row[column] = defaultValue;
            }
            return row;
        });
    }

    public static IPipeline<DataRow> AddColumn(this IPipeline<DataRow> pipeline, string columnName, Func<DataRow, object> valueSelector)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentNullException(nameof(columnName));
        
        if (valueSelector == null)
            throw new ArgumentNullException(nameof(valueSelector));

        return pipeline.Map(row =>
        {
            row[columnName] = valueSelector(row);
            return row;
        });
    }

    public static IPipeline<DataRow> RemoveColumn(this IPipeline<DataRow> pipeline, string columnName)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentNullException(nameof(columnName));

        return pipeline.Map(row =>
        {
            var newRow = new DataRow();
            foreach (var col in row.GetColumnNames())
            {
                if (!col.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    newRow[col] = row[col];
                }
            }
            return newRow;
        });
    }

    public static IPipeline<DataRow> RenameColumn(this IPipeline<DataRow> pipeline, string oldName, string newName)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        if (string.IsNullOrWhiteSpace(oldName))
            throw new ArgumentNullException(nameof(oldName));
        
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentNullException(nameof(newName));

        return pipeline.Map(row =>
        {
            var newRow = new DataRow();
            foreach (var col in row.GetColumnNames())
            {
                var columnName = col.Equals(oldName, StringComparison.OrdinalIgnoreCase) ? newName : col;
                newRow[columnName] = row[col];
            }
            return newRow;
        });
    }

    public static void ToCsv(this IPipeline<DataRow> pipeline, string filePath)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        var writer = new CsvWriter(filePath);
        writer.Write(pipeline.Execute());
    }

    public static void ToCsv(this IPipeline<DataRow> pipeline, string filePath, Action<CsvWriter> configure)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        var writer = new CsvWriter(filePath);
        configure?.Invoke(writer);
        writer.Write(pipeline.Execute());
    }

    public static void ToJson(this IPipeline<DataRow> pipeline, string filePath)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        var writer = new JsonWriter(filePath);
        writer.Write(pipeline.Execute());
    }

    public static void ToJson(this IPipeline<DataRow> pipeline, string filePath, Action<JsonWriter> configure)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        var writer = new JsonWriter(filePath);
        configure?.Invoke(writer);
        writer.Write(pipeline.Execute());
    }

    public static void ToSql(this IPipeline<DataRow> pipeline, string connectionString, string tableName)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        var writer = new SqlWriter(connectionString, tableName);
        writer.Write(pipeline.Execute());
    }

    public static void ToSql(this IPipeline<DataRow> pipeline, string connectionString, string tableName, Action<SqlWriter> configure)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        var writer = new SqlWriter(connectionString, tableName);
        configure?.Invoke(writer);
        writer.Write(pipeline.Execute());
    }

    public static void ToSqlBulk(this IPipeline<DataRow> pipeline, string connectionString, string tableName)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        var writer = new SqlWriter(connectionString, tableName);
        writer.BulkWrite(pipeline.Execute());
    }

    public static void ToExcel(this IPipeline<DataRow> pipeline, string filePath)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        var writer = new ExcelWriter(filePath);
        writer.Write(pipeline.Execute());
    }

    public static void ToExcel(this IPipeline<DataRow> pipeline, string filePath, Action<ExcelWriter> configure)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        var writer = new ExcelWriter(filePath);
        configure?.Invoke(writer);
        writer.Write(pipeline.Execute());
    }

    public static void ToApi(this IPipeline<DataRow> pipeline, string endpoint)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        var writer = new ApiWriter(endpoint);
        writer.Write(pipeline.Execute());
    }

    public static void ToApi(this IPipeline<DataRow> pipeline, string endpoint, Action<ApiWriter> configure)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        var writer = new ApiWriter(endpoint);
        configure?.Invoke(writer);
        writer.Write(pipeline.Execute());
    }

    public static void ToMongoDB(this IPipeline<DataRow> pipeline, string connectionString, string database, string collection)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        var writer = new MongoWriter(connectionString, database, collection);
        writer.Write(pipeline.Execute());
    }

    public static void ToMongoDB(this IPipeline<DataRow> pipeline, string connectionString, string database, string collection, Action<MongoWriter> configure)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        var writer = new MongoWriter(connectionString, database, collection);
        configure?.Invoke(writer);
        writer.Write(pipeline.Execute());
    }

    public static IPipeline<IGrouping<TKey, DataRow>> GroupBy<TKey>(
        this IPipeline<DataRow> pipeline, 
        Func<DataRow, TKey> keySelector)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));

        var groups = pipeline.Execute().GroupBy(keySelector);
        return new Pipeline<IGrouping<TKey, DataRow>>(groups);
    }

    public static IPipeline<DataRow> GroupBy(
        this IPipeline<DataRow> pipeline,
        string keyColumn,
        params (string columnName, Func<IEnumerable<DataRow>, object> aggregator)[] aggregations)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        if (string.IsNullOrWhiteSpace(keyColumn))
            throw new ArgumentNullException(nameof(keyColumn));

        var groups = pipeline.Execute().GroupBy(row => row[keyColumn]);
        
        var result = groups.Select(group =>
        {
            var newRow = new DataRow();
            newRow[keyColumn] = group.Key;
            
            foreach (var (columnName, aggregator) in aggregations)
            {
                newRow[columnName] = aggregator(group);
            }
            
            return newRow;
        });

        return new Pipeline<DataRow>(result);
    }

    public static IPipeline<DataRow> Validate(
        this IPipeline<DataRow> pipeline,
        Action<DataValidator> configure,
        ValidationErrorHandling errorHandling = ValidationErrorHandling.Skip)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var validator = new DataValidator();
        configure(validator);

        var rows = pipeline.Execute();
        var validatedRows = new List<DataRow>();

        foreach (var row in rows)
        {
            var result = validator.Validate(row);
            
            if (result.IsValid)
            {
                validatedRows.Add(row);
            }
            else
            {
                switch (errorHandling)
                {
                    case ValidationErrorHandling.ThrowException:
                        throw new InvalidOperationException($"Validation failed: {result.GetErrorSummary()}");
                    
                    case ValidationErrorHandling.Skip:
                        continue;
                    
                    case ValidationErrorHandling.Log:
                        Console.WriteLine($"Validation error: {result.GetErrorSummary()}");
                        validatedRows.Add(row);
                        break;
                    
                    case ValidationErrorHandling.Fix:
                        validatedRows.Add(row);
                        break;
                }
            }
        }

        return new Pipeline<DataRow>(validatedRows);
    }

    public static IPipeline<ValidationResult> ValidateWithResults(
        this IPipeline<DataRow> pipeline,
        Action<DataValidator> configure)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var validator = new DataValidator();
        configure(validator);

        var results = validator.ValidateAll(pipeline.Execute());
        return new Pipeline<ValidationResult>(results);
    }

    public static async Task ToS3Csv(this IPipeline<DataRow> pipeline, string bucketName, string key, string region = "us-east-1")
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        var tempFile = Path.GetTempFileName();
        var csvWriter = new CsvWriter(tempFile);
        csvWriter.Write(pipeline.Execute());
        
        var s3Writer = new S3Writer(bucketName, key)
            .WithRegion(RegionEndpoint.GetBySystemName(region));
        
        await s3Writer.UploadFileAsync(tempFile);
        File.Delete(tempFile);
    }

    public static async Task ToAzureBlobCsv(this IPipeline<DataRow> pipeline, string connectionString, string containerName, string blobName)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        var tempFile = Path.GetTempFileName();
        var csvWriter = new CsvWriter(tempFile);
        csvWriter.Write(pipeline.Execute());
        
        var azureWriter = new AzureBlobWriter(connectionString, containerName, blobName);
        await azureWriter.UploadFileAsync(tempFile);
        File.Delete(tempFile);
    }

    public static async Task ToGoogleCloudCsv(this IPipeline<DataRow> pipeline, string bucketName, string objectName)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        var tempFile = Path.GetTempFileName();
        var csvWriter = new CsvWriter(tempFile);
        csvWriter.Write(pipeline.Execute());
        
        var gcsWriter = new GoogleCloudStorageWriter(bucketName, objectName);
        await gcsWriter.UploadFileAsync(tempFile);
        File.Delete(tempFile);
    }
}