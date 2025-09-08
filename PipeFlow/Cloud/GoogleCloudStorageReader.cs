using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using Google.Apis.Storage.v1.Data;

namespace PipeFlow.Core.Cloud;

public class GoogleCloudStorageReader
{
    private readonly string _bucketName;
    private readonly string _objectName;
    private StorageClient _storageClient;
    private string _projectId;

    public GoogleCloudStorageReader(string bucketName, string objectName)
    {
        if (bucketName == null)
            throw new ArgumentNullException("bucketName");
        if (objectName == null)
            throw new ArgumentNullException("objectName");
            
        _bucketName = bucketName;
        _objectName = objectName;
    }

    public GoogleCloudStorageReader WithProjectId(string projectId)
    {
        _projectId = projectId;
        return this;
    }

    public GoogleCloudStorageReader WithCredentials(string jsonPath)
    {
        _storageClient = StorageClient.Create(Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(jsonPath));
        return this;
    }

    private StorageClient GetClient()
    {
        if (_storageClient != null)
            return _storageClient;
            
        _storageClient = StorageClient.Create();
        return _storageClient;
    }

    public async Task<Stream> ReadStreamAsync()
    {
        var client = GetClient();
        var stream = new MemoryStream();
        await client.DownloadObjectAsync(_bucketName, _objectName, stream);
        stream.Position = 0;
        return stream;
    }

    public async Task<string> ReadTextAsync()
    {
        using var stream = await ReadStreamAsync();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task DownloadToFileAsync(string localPath)
    {
        var client = GetClient();
        using var fileStream = File.Create(localPath);
        await client.DownloadObjectAsync(_bucketName, _objectName, fileStream);
    }

    public async Task<List<string>> ListObjectsAsync(string prefix = null)
    {
        var client = GetClient();
        var objects = new List<string>();
        
        await foreach (var obj in client.ListObjectsAsync(_bucketName, prefix))
        {
            objects.Add(obj.Name);
        }
        
        return objects;
    }

    public async Task<Google.Apis.Storage.v1.Data.Object> GetMetadataAsync()
    {
        var client = GetClient();
        return await client.GetObjectAsync(_bucketName, _objectName);
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
}