using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace PipeFlow.Core.MongoDB;

public class MongoReader
{
    private readonly string _connectionString;
    private readonly string _databaseName;
    private readonly string _collectionName;
    private FilterDefinition<BsonDocument> _filter = FilterDefinition<BsonDocument>.Empty;
    private SortDefinition<BsonDocument> _sort;
    private int? _limit;
    private int? _skip;
    private ProjectionDefinition<BsonDocument> _projection;
    private readonly List<string> _pipeline = new List<string>();

    public MongoReader(string connectionString, string databaseName, string collectionName)
    {
        if (connectionString == null)
            throw new ArgumentNullException("connectionString");
        if (databaseName == null)
            throw new ArgumentNullException("databaseName");
        if (collectionName == null)
            throw new ArgumentNullException("collectionName");
            
        _connectionString = connectionString;
        _databaseName = databaseName;
        _collectionName = collectionName;
    }

    public MongoReader Where(string field, object value)
    {
        _filter = Builders<BsonDocument>.Filter.Eq(field, BsonValue.Create(value));
        return this;
    }

    public MongoReader Where(FilterDefinition<BsonDocument> filter)
    {
        _filter = filter;
        return this;
    }

    public MongoReader WhereJson(string jsonFilter)
    {
        _filter = jsonFilter;
        return this;
    }

    public MongoReader Sort(string field, bool ascending = true)
    {
        if (ascending)
            _sort = Builders<BsonDocument>.Sort.Ascending(field);
        else
            _sort = Builders<BsonDocument>.Sort.Descending(field);
        return this;
    }

    public MongoReader Limit(int limit)
    {
        _limit = limit;
        return this;
    }

    public MongoReader Skip(int skip)
    {
        _skip = skip;
        return this;
    }

    public MongoReader Project(params string[] fields)
    {
        var projectionBuilder = Builders<BsonDocument>.Projection;
        ProjectionDefinition<BsonDocument> projection = null;

        foreach (var field in fields)
        {
            if (projection == null)
                projection = projectionBuilder.Include(field);
            else
                projection = projection.Include(field);
        }

        if (projection != null)
        {
            if (!fields.Contains("_id"))
                projection = projection.Exclude("_id");
        }
        
        _projection = projection;
        return this;
    }

    public MongoReader Aggregate(string pipelineStage)
    {
        _pipeline.Add(pipelineStage);
        return this;
    }

    public IEnumerable<DataRow> Read()
    {
        var client = new MongoClient(_connectionString);
        var database = client.GetDatabase(_databaseName);
        var collection = database.GetCollection<BsonDocument>(_collectionName);

        if (_pipeline.Any())
        {
            var pipelineDefinition = new BsonDocument[0];
            foreach (var stage in _pipeline)
            {
                var stages = pipelineDefinition.ToList();
                stages.Add(BsonDocument.Parse(stage));
                pipelineDefinition = stages.ToArray();
            }
            
            var cursor = collection.Aggregate<BsonDocument>(pipelineDefinition);
            
            foreach (var document in cursor.ToEnumerable())
            {
                yield return ConvertToDataRow(document);
            }
        }
        else
        {
            var findOptions = new FindOptions<BsonDocument, BsonDocument>();
            findOptions.Sort = _sort;
            findOptions.Limit = _limit;
            findOptions.Skip = _skip;
            findOptions.Projection = _projection;

            var cursor = collection.Find(_filter).Sort(_sort).Limit(_limit).Skip(_skip).Project<BsonDocument>(_projection);
            
            foreach (var document in cursor.ToEnumerable())
            {
                yield return ConvertToDataRow(document);
            }
        }
    }

    private DataRow ConvertToDataRow(BsonDocument document)
    {
        var row = new DataRow();
        
        foreach (var element in document.Elements)
        {
            row[element.Name] = ConvertBsonValue(element.Value);
        }
        
        return row;
    }

    private object ConvertBsonValue(BsonValue value)
    {
        switch (value.BsonType)
        {
            case BsonType.Null:
                return null;
            case BsonType.String:
                return value.AsString;
            case BsonType.Int32:
                return value.AsInt32;
            case BsonType.Int64:
                return value.AsInt64;
            case BsonType.Double:
                return value.AsDouble;
            case BsonType.Decimal128:
                return value.AsDecimal128.ToString();
            case BsonType.Boolean:
                return value.AsBoolean;
            case BsonType.DateTime:
                return value.ToUniversalTime();
            case BsonType.ObjectId:
                return value.AsObjectId.ToString();
            case BsonType.Array:
                return ConvertBsonArray(value.AsBsonArray);
            case BsonType.Document:
                return ConvertBsonDocument(value.AsBsonDocument);
            default:
                return value.ToString();
        }
    }

    private object ConvertBsonArray(BsonArray array)
    {
        var list = new List<object>();
        foreach (var item in array)
        {
            list.Add(ConvertBsonValue(item));
        }
        return list;
    }

    private object ConvertBsonDocument(BsonDocument document)
    {
        var dict = new Dictionary<string, object>();
        foreach (var element in document.Elements)
        {
            dict[element.Name] = ConvertBsonValue(element.Value);
        }
        return dict;
    }
}