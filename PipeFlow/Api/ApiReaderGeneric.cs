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
        Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public virtual ApiReader<TResult> WithAuth(string token, string scheme = "Bearer")
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Authentication token cannot be null or whitespace.", nameof(token));

        if (string.IsNullOrWhiteSpace(scheme))
            throw new ArgumentException("Authentication scheme cannot be null or whitespace.", nameof(scheme));

        AuthToken = $"{scheme} {token}";
        return this;
    }

    public virtual ApiReader<TResult> WithHeader(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Header name cannot be null or whitespace.", nameof(name));

        if (value is null)
            throw new ArgumentNullException(nameof(value));

        Headers[name] = value;
        return this;
    }

    public virtual ApiReader<TResult> WithRetry(int maxRetries, TimeSpan? delay = null)
    {
        if (maxRetries <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetries), maxRetries, "Retry count must be greater than zero.");

        if (delay is { } retryDelay && retryDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "Retry delay must be non-negative.");

        MaxRetries = maxRetries;
        if (delay != null)
            RetryDelay = delay.Value;
        return this;
    }

    public virtual TResult Read()
    {
        var result = ReadAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        return result;
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
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                ApplyRequestHeaders(request);

                using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<TResult>();
                    return result;
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
                continue;
            }

            attempt++;
            if (attempt < MaxRetries)
            {
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

    private void ApplyRequestHeaders(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(AuthToken))
        {
            request.Headers.Add("Authorization", AuthToken);
        }

        foreach (var header in Headers)
        {
            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
            {
                throw new InvalidOperationException($"Failed to add header '{header.Key}' to the request.");
            }
        }
    }
}
