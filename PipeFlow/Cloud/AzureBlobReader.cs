using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace PipeFlow.Core.Cloud;

public class AzureBlobReader
{
    private readonly string _connectionString;
    private readonly string _containerName;
    private readonly string _blobName;
    private BlobServiceClient _serviceClient;
    private BlobContainerClient _containerClient;

    public AzureBlobReader(string connectionString, string containerName, string blobName)
    {
        if (connectionString == null)
            throw new ArgumentNullException("connectionString");
        if (containerName == null)
            throw new ArgumentNullException("containerName");
        if (blobName == null)
            throw new ArgumentNullException("blobName");
            
        _connectionString = connectionString;
        _containerName = containerName;
        _blobName = blobName;
    }

    public AzureBlobReader(string containerUrl, string blobName)
    {
        if (containerUrl == null)
            throw new ArgumentNullException("containerUrl");
        if (blobName == null)
            throw new ArgumentNullException("blobName");
            
        _containerName = containerUrl;
        _blobName = blobName;
    }

    private BlobContainerClient GetContainerClient()
    {
        if (_containerClient != null)
            return _containerClient;
            
        if (_connectionString != null)
        {
            _serviceClient = new BlobServiceClient(_connectionString);
            _containerClient = _serviceClient.GetBlobContainerClient(_containerName);
        }
        else
        {
            _containerClient = new BlobContainerClient(new Uri(_containerName));
        }
        
        return _containerClient;
    }

    public async Task<Stream> ReadStreamAsync()
    {
        var containerClient = GetContainerClient();
        var blobClient = containerClient.GetBlobClient(_blobName);
        
        var response = await blobClient.DownloadStreamingAsync();
        return response.Value.Content;
    }

    public async Task<string> ReadTextAsync()
    {
        using var stream = await ReadStreamAsync();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task DownloadToFileAsync(string localPath)
    {
        var containerClient = GetContainerClient();
        var blobClient = containerClient.GetBlobClient(_blobName);
        
        await blobClient.DownloadToAsync(localPath);
    }

    public async Task<List<string>> ListBlobsAsync(string prefix = null)
    {
        var containerClient = GetContainerClient();
        var blobs = new List<string>();
        
        await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
        {
            blobs.Add(blobItem.Name);
        }
        
        return blobs;
    }

    public async Task<BlobProperties> GetPropertiesAsync()
    {
        var containerClient = GetContainerClient();
        var blobClient = containerClient.GetBlobClient(_blobName);
        
        var response = await blobClient.GetPropertiesAsync();
        return response.Value;
    }

    public async Task<bool> ExistsAsync()
    {
        var containerClient = GetContainerClient();
        var blobClient = containerClient.GetBlobClient(_blobName);
        
        var response = await blobClient.ExistsAsync();
        return response.Value;
    }
}