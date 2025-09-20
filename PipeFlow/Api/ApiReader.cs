using System.Text.Json;

namespace PipeFlow.Core.Api;

public sealed class ApiReader : ApiReader<IEnumerable<DataRow>>
{
    private int? _pageSize;
    private string _pageParameter = "page";
    private string _pageSizeParameter = "pageSize";

    public ApiReader(string baseUrl)
        : base(baseUrl)
    {
        
    }

    public override ApiReader WithAuth(string token, string scheme = "Bearer")
    {
        base.WithAuth(token, scheme);
        return this;
    }

    public override ApiReader WithHeader(string name, string value)
    {
        base.WithHeader(name, value);
        return this;
    }

    public override ApiReader WithRetry(int maxRetries, TimeSpan? delay = null)
    {
        base.WithRetry(maxRetries, delay);
        return this;
    }

    public ApiReader WithPagination(int pageSize, string pageParam = "page", string sizeParam = "pageSize")
    {
        _pageSize = pageSize;
        _pageParameter = pageParam;
        _pageSizeParameter = sizeParam;
        return this;
    }

    public override IEnumerable<DataRow> Read()
    {
        var task = Task.Run(async () => await ReadAsync());
        task.Wait();
        
        foreach (var row in task.Result)
        {
            yield return row;
        }
    }

    public override async Task<IEnumerable<DataRow>> ReadAsync()
    {
        var results = new List<DataRow>();

        if (_pageSize != null)
        {
            var page = 1;
            var hasMoreData = true;

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
            var data = await FetchDataWithRetry(BaseUrl);
            if (data != null)
                results.AddRange(data);
        }

        return results;
    }

    private string BuildPaginatedUrl(int page)
    {
        var separator = BaseUrl.Contains('?') ? "&" : "?";
        return $"{BaseUrl}{separator}{_pageParameter}={page}&{_pageSizeParameter}={_pageSize}";
    }

    protected override async Task<IEnumerable<DataRow>> FetchDataWithRetry(string url)
    {
        var attempt = 0;
        
        while (attempt < MaxRetries)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                
                if (AuthToken != null)
                {
                    request.Headers.Add("Authorization", AuthToken);
                }

                foreach (var header in Headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await HttpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return ParseJson(json);
                }

                attempt++;
                if (attempt < MaxRetries)
                {
                    await Task.Delay(RetryDelay * attempt);
                }
            }
            catch (Exception ex)
            {
                attempt++;
                if (attempt >= MaxRetries)
                {
                    throw new Exception($"Failed to fetch data from {url} after {MaxRetries} attempts", ex);
                }
                await Task.Delay(RetryDelay * attempt);
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
}