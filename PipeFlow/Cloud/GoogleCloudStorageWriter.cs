using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Object = Google.Apis.Storage.v1.Data.Object;

namespace PipeFlow.Core.Cloud;

public class GoogleCloudStorageWriter
{
    private readonly string _bucketName;
    private readonly string _objectName;
    private StorageClient _storageClient;
    private string _projectId;
    private string _storageClass = "STANDARD";
    private Dictionary<string, string> _metadata;

    public GoogleCloudStorageWriter(string bucketName, string objectName)
    {
        if (bucketName == null)
            throw new ArgumentNullException("bucketName");
        if (objectName == null)
            throw new ArgumentNullException("objectName");
            
        _bucketName = bucketName;
        _objectName = objectName;
        _metadata = new Dictionary<string, string>();
    }

    public GoogleCloudStorageWriter WithProjectId(string projectId)
    {
        _projectId = projectId;
        return this;
    }

    public GoogleCloudStorageWriter WithCredentials(string jsonPath)
    {
        _storageClient = StorageClient.Create(Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(jsonPath));
        return this;
    }

    public GoogleCloudStorageWriter WithStorageClass(string storageClass)
    {
        _storageClass = storageClass;
        return this;
    }

    public GoogleCloudStorageWriter WithMetadata(string key, string value)
    {
        _metadata[key] = value;
        return this;
    }

    private StorageClient GetClient()
    {
        if (_storageClient != null)
            return _storageClient;
            
        _storageClient = StorageClient.Create();
        return _storageClient;
    }

    public async Task WriteStreamAsync(Stream stream)
    {
        var client = GetClient();
        
        var objectToUpload = new Object();
        objectToUpload.Bucket = _bucketName;
        objectToUpload.Name = _objectName;
        objectToUpload.StorageClass = _storageClass;
        if (_metadata.Count > 0)
            objectToUpload.Metadata = _metadata;
        
        await client.UploadObjectAsync(objectToUpload, stream);
    }

    public async Task WriteTextAsync(string content)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await WriteStreamAsync(stream);
    }

    public async Task UploadFileAsync(string localPath)
    {
        var client = GetClient();
        
        using var fileStream = File.OpenRead(localPath);
        
        var objectToUpload = new Object();
        objectToUpload.Bucket = _bucketName;
        objectToUpload.Name = _objectName;
        objectToUpload.StorageClass = _storageClass;
        if (_metadata.Count > 0)
            objectToUpload.Metadata = _metadata;
        
        await client.UploadObjectAsync(objectToUpload, fileStream);
    }

    public async Task DeleteAsync()
    {
        var client = GetClient();
        await client.DeleteObjectAsync(_bucketName, _objectName);
    }

    public async Task<bool> ExistsAsync()
    {
        var client = GetClient();
        
        try
        {
            await client.GetObjectAsync(_bucketName, _objectName);
            return true;
        }
        catch (Google.GoogleApiException ex)
        {
            if (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                return false;
            throw;
        }
    }

    public async Task CreateBucketIfNotExistsAsync()
    {
        var client = GetClient();
        
        try
        {
            await client.GetBucketAsync(_bucketName);
        }
        catch (Google.GoogleApiException ex)
        {
            if (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                if (_projectId == null)
                    throw new InvalidOperationException("ProjectId is required to create bucket");
                await client.CreateBucketAsync(_projectId, _bucketName);
            }
            else
            {
                throw;
            }
        }
    }
}