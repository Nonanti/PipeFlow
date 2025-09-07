using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using DataFlow.Core;

namespace DataFlow.Core.Cloud;

public class S3Reader
{
    private readonly string _bucketName;
    private readonly string _key;
    private RegionEndpoint _region = RegionEndpoint.USEast1;
    private string _accessKey;
    private string _secretKey;

    public S3Reader(string bucketName, string key)
    {
        if (bucketName == null)
            throw new ArgumentNullException("bucketName");
        if (key == null)
            throw new ArgumentNullException("key");
            
        _bucketName = bucketName;
        _key = key;
    }

    public S3Reader WithRegion(RegionEndpoint region)
    {
        _region = region;
        return this;
    }

    public S3Reader WithCredentials(string accessKey, string secretKey)
    {
        _accessKey = accessKey;
        _secretKey = secretKey;
        return this;
    }

    private AmazonS3Client GetClient()
    {
        if (_accessKey != null && _secretKey != null)
            return new AmazonS3Client(_accessKey, _secretKey, _region);
        
        return new AmazonS3Client(_region);
    }

    public async Task<Stream> ReadStreamAsync()
    {
        using var client = GetClient();
        
        var request = new GetObjectRequest();
        request.BucketName = _bucketName;
        request.Key = _key;

        var response = await client.GetObjectAsync(request);
        return response.ResponseStream;
    }

    public async Task<string> ReadTextAsync()
    {
        using var stream = await ReadStreamAsync();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task DownloadToFileAsync(string localPath)
    {
        using var client = GetClient();
        
        var request = new GetObjectRequest();
        request.BucketName = _bucketName;
        request.Key = _key;

        using var response = await client.GetObjectAsync(request);
        using var fileStream = File.Create(localPath);
        await response.ResponseStream.CopyToAsync(fileStream);
    }

    public async Task<List<string>> ListObjectsAsync(string prefix = null)
    {
        using var client = GetClient();
        
        var request = new ListObjectsV2Request();
        request.BucketName = _bucketName;
        if (prefix != null)
            request.Prefix = prefix;
        else
            request.Prefix = _key;

        var objects = new List<string>();
        ListObjectsV2Response response;
        
        do
        {
            response = await client.ListObjectsV2Async(request);
            foreach (var obj in response.S3Objects)
            {
                objects.Add(obj.Key);
            }
            request.ContinuationToken = response.NextContinuationToken;
        } while (response.IsTruncated == true);

        return objects;
    }
}