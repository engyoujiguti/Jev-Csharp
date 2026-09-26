using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jev.Client.Models;

namespace Jev.Client;

/// <summary>A .NET client for the TypeSafe AI Jev System One API.</summary>
public sealed class JevClient : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        AllowOutOfOrderMetadataProperties = true
    };

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly JevClientOptions _options;
    private readonly bool _ownsHttpClient;

    /// <summary>Creates a client that owns its underlying <see cref="HttpClient"/>.</summary>
    public JevClient(string apiKey, JevClientOptions? options = null)
        : this(new HttpClient(), apiKey, options, ownsHttpClient: true)
    {
    }

    /// <summary>Creates a client using a caller-owned <see cref="HttpClient"/>.</summary>
    public JevClient(HttpClient httpClient, string apiKey, JevClientOptions? options = null)
        : this(httpClient, apiKey, options, ownsHttpClient: false)
    {
    }

    private JevClient(
        HttpClient httpClient,
        string apiKey,
        JevClientOptions? options,
        bool ownsHttpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        _options = options ?? new JevClientOptions();
        ValidateOptions(_options);

        _httpClient = httpClient;
        _apiKey = apiKey;
        _ownsHttpClient = ownsHttpClient;
    }

    /// <summary>Evaluates state against one or more typed questions.</summary>
    public async Task<SystemOneResponse> EvaluateAsync(
        SystemOneRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        for (var attempt = 0; ; attempt++)
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
            {
                Content = JsonContent.Create(request, options: SerializerOptions)
            };
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            using var response = await _httpClient.SendAsync(
                message,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            if (IsRetryable(response.StatusCode) && attempt < _options.MaxRetries)
            {
                var retryDelay = GetRetryDelay(response, attempt);
                response.Dispose();
                await Task.Delay(retryDelay, _options.TimeProvider, cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);
                throw new JevApiException(response.StatusCode, errorBody);
            }

            var result = await response.Content
                .ReadFromJsonAsync<SystemOneResponse>(SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            return result ?? throw new JsonException("The TypeSafe API returned an empty response.");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private static bool IsRetryable(HttpStatusCode statusCode) =>
        (int)statusCode is 429 or 529;

    private TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta)
        {
            return Min(delta, _options.MaxRetryDelay);
        }

        if (retryAfter?.Date is { } retryDate)
        {
            var serverDelay = retryDate - _options.TimeProvider.GetUtcNow();
            if (serverDelay > TimeSpan.Zero)
            {
                return Min(serverDelay, _options.MaxRetryDelay);
            }
        }

        var exponentialMilliseconds =
            _options.RetryBaseDelay.TotalMilliseconds * Math.Pow(2, attempt);
        var cappedMilliseconds = Math.Min(
            exponentialMilliseconds,
            _options.MaxRetryDelay.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(cappedMilliseconds);
    }

    private static TimeSpan Min(TimeSpan left, TimeSpan right) => left <= right ? left : right;

    private static void ValidateOptions(JevClientOptions options)
    {
        if (!options.Endpoint.IsAbsoluteUri)
        {
            throw new ArgumentException("Endpoint must be an absolute URI.", nameof(options));
        }

        if (options.MaxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxRetries cannot be negative.");
        }

        if (options.RetryBaseDelay < TimeSpan.Zero || options.MaxRetryDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Retry delays cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(options.TimeProvider);
    }
}
