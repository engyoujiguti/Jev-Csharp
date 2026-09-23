namespace Jev.Client;

/// <summary>Configures requests made by <see cref="JevClient"/>.</summary>
public sealed class JevClientOptions
{
    /// <summary>The TypeSafe System One endpoint.</summary>
    public Uri Endpoint { get; init; } = new("https://api.typesafe.ai/v1/systemone");

    /// <summary>The number of retries after the initial request for HTTP 429 or 529 responses.</summary>
    public int MaxRetries { get; init; } = 2;

    /// <summary>The initial delay used by exponential backoff.</summary>
    public TimeSpan RetryBaseDelay { get; init; } = TimeSpan.FromMilliseconds(250);

    /// <summary>The maximum delay between retry attempts.</summary>
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>The time provider used for retry delays.</summary>
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;
}
