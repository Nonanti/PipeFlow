using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DataFlow.Core.MongoDB;

public class MongoWriter
{
    private readonly string _connectionString;
    private readonly string _databaseName;
    private readonly string _collectionName;
    private bool _upsert = false;
    private string _upsertKeyField = "_id";
    private int _batchSize = 1000;
    private bool _dropCollection = false;
    private bool _createIndexes = false;
    private List<string> _indexFields = new List<string>();
    private InsertManyOptions _insertOptions;
    private ReplaceOptions _replaceOptions;

    public MongoWriter(string connectionString, string databaseName, string collectionName)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        if (databaseName == null)
            throw new ArgumentNullException(nameof(databaseName));
        if (collectionName == null)
            throw new ArgumentNullException(nameof(collectionName));
        
        this._connectionString = connectionString;
        this._databaseName = databaseName;
        this._collectionName = collectionName;
        
        _insertOptions = new InsertManyOptions();
        _insertOptions.IsOrdered = false;
        _replaceOptions = new ReplaceOptions();
        _replaceOptions.IsUpsert = true;
    }

    public MongoWriter WithBatchSize(int batchSize)
    {
        _batchSize = batchSize;
        return this;
    }

    public MongoWriter WithUpsert(string keyField = "_id")
    {
        _upsert = true;
        _upsertKeyField = keyField;
        return this;
    }

    public MongoWriter DropCollectionFirst()
    {
        _dropCollection = true;
        return this;
    }

    public MongoWriter CreateIndex(params string[] fields)
    {
        _createIndexes = true;
        _indexFields.AddRange(fields);
        return this;
    }

    public void Write(IEnumerable<DataRow> rows)
    {
        var client = new MongoClient(_connectionString);
        var database = client.GetDatabase(_databaseName);
        var collection = database.GetCollection<BsonDocument>(_collectionName);

        if (_dropCollection)
        {
            database.DropCollection(_collectionName);
            collection = database.GetCollection<BsonDocument>(_collectionName);
        }

        if (_createIndexes && _indexFields.Any())
        {
            CreateIndexes(collection);
        }

        var batch = new List<BsonDocument>();
        var upsertBatch = new List<(FilterDefinition<BsonDocument>, BsonDocument)>();

        foreach (var row in rows)
        {
            var document = ConvertToDocument(row);

            if (_upsert)
            {
                object filterValue = null;
                if (row.ContainsColumn(_upsertKeyField))
                    filterValue = row[_upsertKeyField];
                
                if (filterValue != null)
                {
                    var filter = Builders<BsonDocument>.Filter.Eq(_upsertKeyField, BsonValue.Create(filterValue));
                    upsertBatch.Add((filter, document));
                }
                else
                {
                    batch.Add(document);
                }

                if (upsertBatch.Count >= _batchSize)
                {
                    ProcessUpsertBatch(collection, upsertBatch);
                    upsertBatch.Clear();
                }
            }
            else
            {
                batch.Add(document);
            }

            if (batch.Count >= _batchSize)
            {
                collection.InsertMany(batch, _insertOptions);
                batch.Clear();
            }
        }

        if (batch.Any())
        {
            collection.InsertMany(batch, _insertOptions);
        }

        if (upsertBatch.Any())
        {
            ProcessUpsertBatch(collection, upsertBatch);
        }
    }

    private void ProcessUpsertBatch(IMongoCollection<BsonDocument> collection, 
        List<(FilterDefinition<BsonDocument>, BsonDocument)> batch)
    {
        var bulkOps = new List<WriteModel<BsonDocument>>();
        
        foreach (var (filter, document) in batch)
        {
            bulkOps.Add(new ReplaceOneModel<BsonDocument>(filter, document) { IsUpsert = true });
        }

        if (bulkOps.Any())
        {
            collection.BulkWrite(bulkOps, new BulkWriteOptions { IsOrdered = false });
        }
    }

    private void CreateIndexes(IMongoCollection<BsonDocument> collection)
    {
        var indexKeysDefinitions = new List<CreateIndexModel<BsonDocument>>();

        foreach (var field in _indexFields)
        {
            var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending(field);
            indexKeysDefinitions.Add(new CreateIndexModel<BsonDocument>(indexKeys));
        }

        if (indexKeysDefinitions.Any())
        {
            collection.Indexes.CreateMany(indexKeysDefinitions);
        }
    }

    private BsonDocument ConvertToDocument(DataRow row)
    {
        var document = new BsonDocument();
        
        foreach (var columnName in row.GetColumnNames())
        {
            var value = row[columnName];
            document[columnName] = ConvertToBsonValue(value);
        }
        
        return document;
    }

    private BsonValue ConvertToBsonValue(object value)
    {
        if (value == null)
            return BsonNull.Value;

        switch (value)
        {
            case string s:
                return new BsonString(s);
            case int i:
                return new BsonInt32(i);
            case long l:
                return new BsonInt64(l);
            case double d:
                return new BsonDouble(d);
            case decimal dec:
                return new BsonDecimal128(dec);
            case float f:
                return new BsonDouble(f);
            case bool b:
                return new BsonBoolean(b);
            case DateTime dt:
                return new BsonDateTime(dt);
            case Guid guid:
                return new BsonString(guid.ToString());
            case byte[] bytes:
                return new BsonBinaryData(bytes);
            case IEnumerable<object> list:
                return ConvertListToBsonArray(list);
            case Dictionary<string, object> dict:
                return ConvertDictionaryToBsonDocument(dict);
            default:
                return BsonValue.Create(value);
        }
    }

    private BsonArray ConvertListToBsonArray(IEnumerable<object> list)
    {
        var array = new BsonArray();
        foreach (var item in list)
        {
            array.Add(ConvertToBsonValue(item));
        }
        return array;
    }

    private BsonDocument ConvertDictionaryToBsonDocument(Dictionary<string, object> dict)
    {
        var document = new BsonDocument();
        foreach (var kvp in dict)
        {
            document[kvp.Key] = ConvertToBsonValue(kvp.Value);
        }
        return document;
    }
}