using System.Net.Http.Json;

namespace PipeFlow.Core.Api;

public class ApiReader<TResult> : IDisposable
{
    protected readonly string BaseUrl;
    protected readonly HttpClient HttpClient;
    protected string AuthToken;
    protected readonly Dictionary<string, string> Headers;
    protected int MaxRetries = 3;
    protected TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    public ApiReader(string baseUrl)
    {
        BaseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        HttpClient = new HttpClient();
        Headers = new Dictionary<string, string>();
    }

    public virtual ApiReader<TResult> WithAuth(string token, string scheme = "Bearer")
    {
        AuthToken = $"{scheme} {token}";
        return this;
    }

    public virtual ApiReader<TResult> WithHeader(string name, string value)
    {
        Headers[name] = value;
        return this;
    }

    public virtual ApiReader<TResult> WithRetry(int maxRetries, TimeSpan? delay = null)
    {
        MaxRetries = maxRetries;
        if (delay != null)
            RetryDelay = delay.Value;
        return this;
    }

    public virtual TResult Read()
    {
        var task = Task.Run(async () => await ReadAsync());
        task.Wait();

        return task.Result;
    }

    public virtual async Task<TResult> ReadAsync()
    {
        return await FetchDataWithRetry(BaseUrl);
    }

    protected virtual async Task<TResult> FetchDataWithRetry(string url)
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
                    return await response.Content.ReadFromJsonAsync<TResult>();
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

        return default;
    }

    public void Dispose()
    {
        HttpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}