using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using PipeFlow.Core;

namespace PipeFlow.Core.Api;

public class ApiWriter
{
    private readonly string _endpoint;
    private readonly HttpClient _httpClient;
    private string _authToken;
    private Dictionary<string, string> _headers;
    private HttpMethod _method = HttpMethod.Post;
    private int _batchSize = 100;
    private bool _useBulkEndpoint = false;

    public ApiWriter(string endpoint)
    {
        if (endpoint == null)
            throw new ArgumentNullException("endpoint");
            
        _endpoint = endpoint;
        _httpClient = new HttpClient();
        _headers = new Dictionary<string, string>();
    }

    public ApiWriter WithAuth(string token, string scheme = "Bearer")
    {
        _authToken = $"{scheme} {token}";
        return this;
    }

    public ApiWriter WithHeader(string name, string value)
    {
        _headers[name] = value;
        return this;
    }

    public ApiWriter WithMethod(HttpMethod method)
    {
        _method = method;
        return this;
    }

    public ApiWriter WithBatchSize(int size)
    {
        _batchSize = size;
        return this;
    }

    public ApiWriter UseBulkEndpoint(bool useBulk = true)
    {
        _useBulkEndpoint = useBulk;
        return this;
    }

    public void Write(IEnumerable<DataRow> rows)
    {
        var task = Task.Run(async () => await WriteAsync(rows));
        task.Wait();
    }

    private async Task WriteAsync(IEnumerable<DataRow> rows)
    {
        var rowsList = rows.ToList();
        
        if (!rowsList.Any())
            return;

        if (_useBulkEndpoint)
        {
            await SendBatch(rowsList);
        }
        else
        {
            // Send in batches
            for (int i = 0; i < rowsList.Count; i += _batchSize)
            {
                var batch = rowsList.Skip(i).Take(_batchSize).ToList();
                
                if (_batchSize == 1)
                {
                    // Send individually
                    foreach (var row in batch)
                    {
                        await SendSingle(row);
                    }
                }
                else
                {
                    await SendBatch(batch);
                }
            }
        }
    }

    private async Task SendSingle(DataRow row)
    {
        var json = JsonSerializer.Serialize(row.ToDictionary());
        await SendRequest(json);
    }

    private async Task SendBatch(List<DataRow> batch)
    {
        var data = batch.Select(r => r.ToDictionary()).ToList();
        var json = JsonSerializer.Serialize(data);
        await SendRequest(json);
    }

    private async Task SendRequest(string json)
    {
        var request = new HttpRequestMessage(_method, _endpoint);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        if (_authToken != null)
        {
            request.Headers.Add("Authorization", _authToken);
        }

        foreach (var header in _headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }

        try
        {
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"API request failed with status {response.StatusCode}: {error}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to send data to {_endpoint}", ex);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}