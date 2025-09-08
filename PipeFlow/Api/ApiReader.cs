using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using PipeFlow.Core;

namespace PipeFlow.Core.Api;

public class ApiReader
{
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    private string _authToken;
    private Dictionary<string, string> _headers;
    private int _maxRetries = 3;
    private TimeSpan _retryDelay = TimeSpan.FromSeconds(1);
    private int? _pageSize;
    private string _pageParameter = "page";
    private string _pageSizeParameter = "pageSize";

    public ApiReader(string baseUrl)
    {
        if (baseUrl == null)
            throw new ArgumentNullException("baseUrl");
            
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
        _headers = new Dictionary<string, string>();
    }

    public ApiReader WithAuth(string token, string scheme = "Bearer")
    {
        _authToken = $"{scheme} {token}";
        return this;
    }

    public ApiReader WithHeader(string name, string value)
    {
        _headers[name] = value;
        return this;
    }

    public ApiReader WithRetry(int maxRetries, TimeSpan? delay = null)
    {
        _maxRetries = maxRetries;
        if (delay != null)
            _retryDelay = delay.Value;
        return this;
    }

    public ApiReader WithPagination(int pageSize, string pageParam = "page", string sizeParam = "pageSize")
    {
        _pageSize = pageSize;
        _pageParameter = pageParam;
        _pageSizeParameter = sizeParam;
        return this;
    }

    public IEnumerable<DataRow> Read()
    {
        var task = Task.Run(async () => await ReadAsync());
        task.Wait();
        
        foreach (var row in task.Result)
        {
            yield return row;
        }
    }

    private async Task<IEnumerable<DataRow>> ReadAsync()
    {
        var results = new List<DataRow>();

        if (_pageSize != null)
        {
            int page = 1;
            bool hasMoreData = true;

            while (hasMoreData)
            {
                var url = BuildPaginatedUrl(page);
                var pageData = await FetchDataWithRetry(url);
                
                if (pageData == null || !pageData.Any())
                {
                    hasMoreData = false;
                }
                else
                {
                    results.AddRange(pageData);
                    page++;
                }
            }
        }
        else
        {
            var data = await FetchDataWithRetry(_baseUrl);
            if (data != null)
                results.AddRange(data);
        }

        return results;
    }

    private string BuildPaginatedUrl(int page)
    {
        string separator;
        if (_baseUrl.Contains("?"))
            separator = "&";
        else
            separator = "?";
        return $"{_baseUrl}{separator}{_pageParameter}={page}&{_pageSizeParameter}={_pageSize}";
    }

    private async Task<IEnumerable<DataRow>> FetchDataWithRetry(string url)
    {
        int attempt = 0;
        
        while (attempt < _maxRetries)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                
                if (_authToken != null)
                {
                    request.Headers.Add("Authorization", _authToken);
                }

                foreach (var header in _headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return ParseJson(json);
                }

                attempt++;
                if (attempt < _maxRetries)
                {
                    await Task.Delay(_retryDelay * attempt);
                }
            }
            catch (Exception ex)
            {
                attempt++;
                if (attempt >= _maxRetries)
                {
                    throw new Exception($"Failed to fetch data from {url} after {_maxRetries} attempts", ex);
                }
                await Task.Delay(_retryDelay * attempt);
            }
        }

        return Enumerable.Empty<DataRow>();
    }

    private IEnumerable<DataRow> ParseJson(string json)
    {
        var results = new List<DataRow>();
        
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in root.EnumerateArray())
                {
                    results.Add(ParseJsonObject(element));
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Try to find data array in common patterns
                if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in dataElement.EnumerateArray())
                    {
                        results.Add(ParseJsonObject(element));
                    }
                }
                else if (root.TryGetProperty("results", out var resultsElement) && resultsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in resultsElement.EnumerateArray())
                    {
                        results.Add(ParseJsonObject(element));
                    }
                }
                else if (root.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in itemsElement.EnumerateArray())
                    {
                        results.Add(ParseJsonObject(element));
                    }
                }
                else
                {
                    // Single object response
                    results.Add(ParseJsonObject(root));
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to parse JSON response", ex);
        }
        
        return results;
    }

    private DataRow ParseJsonObject(JsonElement element)
    {
        var row = new DataRow();
        
        foreach (var property in element.EnumerateObject())
        {
            row[property.Name] = GetJsonValue(property.Value);
        }
        
        return row;
    }

    private object GetJsonValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var longValue))
                    return longValue;
                else
                    return element.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Array:
                return element.ToString();
            case JsonValueKind.Object:
                return element.ToString();
            default:
                return element.ToString();
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}