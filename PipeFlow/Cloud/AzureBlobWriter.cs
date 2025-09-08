using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace PipeFlow.Core.Cloud;

public class AzureBlobWriter
{
    private readonly string _connectionString;
    private readonly string _containerName;
    private readonly string _blobName;
    private BlobServiceClient _serviceClient;
    private BlobContainerClient _containerClient;
    private AccessTier? _accessTier;
    private bool _overwrite = true;

    public AzureBlobWriter(string connectionString, string containerName, string blobName)
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

    public AzureBlobWriter(string containerUrl, string blobName)
    {
        if (containerUrl == null)
            throw new ArgumentNullException("containerUrl");
        if (blobName == null)
            throw new ArgumentNullException("blobName");
            
        _containerName = containerUrl;
        _blobName = blobName;
    }

    public AzureBlobWriter WithAccessTier(AccessTier accessTier)
    {
        _accessTier = accessTier;
        return this;
    }

    public AzureBlobWriter WithOverwrite(bool overwrite)
    {
        _overwrite = overwrite;
        return this;
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

    public async Task WriteStreamAsync(Stream stream)
    {
        var containerClient = GetContainerClient();
        await containerClient.CreateIfNotExistsAsync();
        
        var blobClient = containerClient.GetBlobClient(_blobName);
        
        await blobClient.UploadAsync(stream, _overwrite);
        
        if (_accessTier != null)
        {
            await blobClient.SetAccessTierAsync(_accessTier.Value);
        }
    }

    public async Task WriteTextAsync(string content)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await WriteStreamAsync(stream);
    }

    public async Task UploadFileAsync(string localPath)
    {
        var containerClient = GetContainerClient();
        await containerClient.CreateIfNotExistsAsync();
        
        var blobClient = containerClient.GetBlobClient(_blobName);
        
        await blobClient.UploadAsync(localPath, _overwrite);
        
        if (_accessTier != null)
        {
            await blobClient.SetAccessTierAsync(_accessTier.Value);
        }
    }

    public async Task DeleteAsync()
    {
        var containerClient = GetContainerClient();
        var blobClient = containerClient.GetBlobClient(_blobName);
        
        await blobClient.DeleteIfExistsAsync();
    }

    public async Task<bool> ExistsAsync()
    {
        var containerClient = GetContainerClient();
        var blobClient = containerClient.GetBlobClient(_blobName);
        
        var response = await blobClient.ExistsAsync();
        return response.Value;
    }

    public async Task SetMetadataAsync(System.Collections.Generic.IDictionary<string, string> metadata)
    {
        var containerClient = GetContainerClient();
        var blobClient = containerClient.GetBlobClient(_blobName);
        
        await blobClient.SetMetadataAsync(metadata);
    }
}