using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using PipeFlow.Core.Api;

namespace PipeFlow.Tests
{
    public class ApiReaderGenericTests
    {
        private sealed class SampleDto
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }

        private sealed class TestableApiReader<T> : ApiReader<T>
        {
            public TestableApiReader(string baseUrl) : base(baseUrl)
            {
            }

            public string? AuthTokenValue => AuthToken;
            public IReadOnlyDictionary<string, string> HeaderValues => Headers;
            public int MaxRetriesValue => MaxRetries;
            public TimeSpan RetryDelayValue => RetryDelay;

            public Func<string, Task<T>>? CustomFetch { get; set; }

            public void UseHttpClient(HttpClient client)
            {
                var field = typeof(ApiReader<T>).GetField("HttpClient", BindingFlags.Instance | BindingFlags.NonPublic);
                field?.SetValue(this, client);
            }

            protected override Task<T> FetchDataWithRetry(string url)
            {
                if (CustomFetch != null)
                {
                    return CustomFetch(url);
                }

                return base.FetchDataWithRetry(url);
            }
        }

        private sealed class TestHttpMessageHandler : HttpMessageHandler
        {
            private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses;

            public TestHttpMessageHandler(IEnumerable<Func<HttpRequestMessage, HttpResponseMessage>> responses)
            {
                _responses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(responses);
            }

            public List<HttpRequestMessage> Requests { get; } = new();

            public void EnqueueResponse(Func<HttpRequestMessage, HttpResponseMessage> response)
            {
                _responses.Enqueue(response);
            }

            public void EnqueueResponses(params Func<HttpRequestMessage, HttpResponseMessage>[] responses)
            {
                foreach (var response in responses)
                {
                    _responses.Enqueue(response);
                }
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(CloneRequest(request));

                var responseFactory = _responses.Count > 0
                    ? _responses.Dequeue()
                    : (_ => new HttpResponseMessage(HttpStatusCode.NotFound));

                var response = responseFactory(request);
                return Task.FromResult(response);
            }

            private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
            {
                var clone = new HttpRequestMessage(request.Method, request.RequestUri);

                foreach (var header in request.Headers)
                {
                    clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                return clone;
            }
        }

        [Fact]
        public void Constructor_NullUrl_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ApiReader<SampleDto>(null!));
        }

        [Fact]
        public async Task ReadAsync_ReturnsDeserializedResult()
        {
            var reader = CreateReader(out var handler);

            handler.EnqueueResponses(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new SampleDto { Id = 42, Name = "Test" })
            });

            var result = await reader.ReadAsync();

            Assert.NotNull(result);
            Assert.Equal(42, result!.Id);
            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public void Read_UsesSynchronousWrapper()
        {
            var reader = new TestableApiReader<SampleDto>("https://api.example.com/data")
            {
                CustomFetch = _ => Task.FromResult(new SampleDto { Id = 7, Name = "Sync" })
            };

            var result = reader.Read();

            Assert.Equal(7, result.Id);
            Assert.Equal("Sync", result.Name);
        }

        [Fact]
        public async Task WithAuth_AddsAuthorizationHeader()
        {
            var reader = CreateReader(out var handler);

            handler.EnqueueResponses(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new SampleDto())
            });

            reader.WithAuth("token-value", "Bearer");

            await reader.ReadAsync();

            var authorization = handler.Requests.Single().Headers.GetValues("Authorization").Single();
            Assert.Equal("Bearer token-value", authorization);
            Assert.Equal("Bearer token-value", reader.AuthTokenValue);
        }

        [Fact]
        public async Task WithHeader_AddsCustomHeader()
        {
            var reader = CreateReader(out var handler);

            handler.EnqueueResponses(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new SampleDto())
            });

            reader.WithHeader("X-Custom", "Value123");

            await reader.ReadAsync();

            var headerValues = handler.Requests.Single().Headers.GetValues("X-Custom").ToArray();
            Assert.Single(headerValues);
            Assert.Equal("Value123", headerValues[0]);
            Assert.Equal("Value123", reader.HeaderValues["X-Custom"]);
        }

        [Fact]
        public void WithRetry_OverridesRetryConfiguration()
        {
            var reader = new TestableApiReader<SampleDto>("https://api.example.com/data");

            reader.WithRetry(5, TimeSpan.FromMilliseconds(250));

            Assert.Equal(5, reader.MaxRetriesValue);
            Assert.Equal(TimeSpan.FromMilliseconds(250), reader.RetryDelayValue);
        }

        [Fact]
        public async Task FetchDataWithRetry_RetriesUntilSuccess()
        {
            var reader = CreateReader(out var handler);

            reader.WithRetry(3, TimeSpan.Zero);

            handler.EnqueueResponses(
                _ => new HttpResponseMessage(HttpStatusCode.InternalServerError),
                _ => new HttpResponseMessage(HttpStatusCode.BadGateway),
                _ => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new SampleDto { Id = 1 })
                });

            var result = await reader.ReadAsync();

            Assert.Equal(3, handler.Requests.Count);
            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
        }

        [Fact]
        public async Task FetchDataWithRetry_ReturnsDefaultAfterUnsuccessfulResponses()
        {
            var reader = CreateReader(out var handler);

            reader.WithRetry(2, TimeSpan.Zero);

            handler.EnqueueResponses(
                _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
                _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var result = await reader.ReadAsync();

            Assert.Equal(2, handler.Requests.Count);
            Assert.Null(result);
        }

        [Fact]
        public async Task FetchDataWithRetry_ThrowsAfterMaxExceptions()
        {
            var reader = CreateReader(out var handler);

            reader.WithRetry(2, TimeSpan.Zero);

            handler.EnqueueResponses(
                _ => throw new HttpRequestException("boom"),
                _ => throw new HttpRequestException("boom"));

            var exception = await Assert.ThrowsAsync<Exception>(() => reader.ReadAsync());

            Assert.Contains("Failed to fetch data", exception.Message);
            Assert.Equal(2, handler.Requests.Count);
        }

        [Fact]
        public async Task FetchDataWithRetry_NoConfiguredResponses_ReturnsDefault()
        {
            var reader = CreateReader(out var handler);

            var result = await reader.ReadAsync();

            Assert.Null(result);
            Assert.Equal(reader.MaxRetriesValue, handler.Requests.Count);
        }

        [Fact]
        public void WithHeader_RewritesExistingValue()
        {
            var reader = CreateReader(out _);

            reader.WithHeader("X-Duplicate", "First")
                  .WithHeader("X-Duplicate", "Second");

            Assert.Equal("Second", reader.HeaderValues["X-Duplicate"]);
        }

        [Fact]
        public async Task Dispose_PreventsFurtherRequests()
        {
            var reader = CreateReader(out var handler);

            handler.EnqueueResponses(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new SampleDto())
            });

            await reader.ReadAsync();

            reader.Dispose();

            var exception = await Assert.ThrowsAsync<Exception>(() => reader.ReadAsync());

            Assert.Contains("Failed to fetch data", exception.Message);
            Assert.All(handler.Requests, request => Assert.Equal(HttpMethod.Get, request.Method));
        }

        private static TestableApiReader<SampleDto> CreateReader(out TestHttpMessageHandler handler)
        {
            handler = new TestHttpMessageHandler(Array.Empty<Func<HttpRequestMessage, HttpResponseMessage>>());
            var reader = new TestableApiReader<SampleDto>("https://api.example.com/data");
            reader.UseHttpClient(new HttpClient(handler));
            return reader;
        }
    }
}
