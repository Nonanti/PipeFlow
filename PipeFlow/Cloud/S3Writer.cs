using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace PipeFlow.Core.Cloud;

public class S3Writer
{
    private readonly string _bucketName;
    private readonly string _key;
    private RegionEndpoint _region = RegionEndpoint.USEast1;
    private string _accessKey;
    private string _secretKey;
    private S3StorageClass _storageClass = S3StorageClass.Standard;
    private ServerSideEncryptionMethod _encryption = ServerSideEncryptionMethod.None;

    public S3Writer(string bucketName, string key)
    {
        if (bucketName == null)
            throw new ArgumentNullException("bucketName");
        if (key == null)
            throw new ArgumentNullException("key");
            
        _bucketName = bucketName;
        _key = key;
    }

    public S3Writer WithRegion(RegionEndpoint region)
    {
        _region = region;
        return this;
    }

    public S3Writer WithCredentials(string accessKey, string secretKey)
    {
        _accessKey = accessKey;
        _secretKey = secretKey;
        return this;
    }

    public S3Writer WithStorageClass(S3StorageClass storageClass)
    {
        _storageClass = storageClass;
        return this;
    }

    public S3Writer WithEncryption(ServerSideEncryptionMethod encryption)
    {
        _encryption = encryption;
        return this;
    }

    private AmazonS3Client GetClient()
    {
        if (_accessKey != null && _secretKey != null)
            return new AmazonS3Client(_accessKey, _secretKey, _region);
        
        return new AmazonS3Client(_region);
    }

    public async Task WriteStreamAsync(Stream stream)
    {
        using var client = GetClient();
        
        var request = new PutObjectRequest();
        request.BucketName = _bucketName;
        request.Key = _key;
        request.InputStream = stream;
        request.StorageClass = _storageClass;
        request.ServerSideEncryptionMethod = _encryption;

        await client.PutObjectAsync(request);
    }

    public async Task WriteTextAsync(string content)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await WriteStreamAsync(stream);
    }

    public async Task UploadFileAsync(string localPath)
    {
        using var client = GetClient();
        
        var request = new PutObjectRequest();
        request.BucketName = _bucketName;
        request.Key = _key;
        request.FilePath = localPath;
        request.StorageClass = _storageClass;
        request.ServerSideEncryptionMethod = _encryption;

        await client.PutObjectAsync(request);
    }

    public async Task DeleteAsync()
    {
        using var client = GetClient();
        
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = _key
        };

        await client.DeleteObjectAsync(request);
    }

    public async Task<bool> ExistsAsync()
    {
        using var client = GetClient();
        
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = _key
            };
            
            await client.GetObjectMetadataAsync(request);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}