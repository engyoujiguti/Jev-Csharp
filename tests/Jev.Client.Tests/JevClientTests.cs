using System.Net;
using System.Text;
using System.Text.Json;
using Jev.Client.Models;

namespace Jev.Client.Tests;

public sealed class JevClientTests
{
    [Fact]
    public async Task EvaluateAsync_SerializesQuestionsAndDeserializesTypedAnswers()
    {
        string? capturedJson = null;
        string? capturedAuthorization = null;
        var handler = new StubHttpMessageHandler(async (request, _) =>
        {
            capturedJson = await request.Content!.ReadAsStringAsync();
            capturedAuthorization = request.Headers.Authorization?.ToString();
            return JsonResponse(HttpStatusCode.OK, """
                {
                  "model": "jev-1.13.0",
                  "answers": {
                    "department": {
                      "type": "choice",
                      "choice": "technical",
                      "confidence": 0.78,
                      "probabilities": { "technical": 0.85, "billing": 0.15 }
                    },
                    "frustration": {
                      "type": "score",
                      "score": 1.05,
                      "confidence": 0.92,
                      "legend": { "0": "Calm", "1": "Frustrated" },
                      "probabilities": { "0": 0.05, "1": 0.95 }
                    },
                    "urgent": { "type": "noul", "noul": 0.95 }
                  },
                  "usage": { "input_tokens": 300, "output_tokens": 40 }
                }
                """);
        });

        using var httpClient = new HttpClient(handler);
        using var client = new JevClient(httpClient, "test-key");

        var result = await client.EvaluateAsync(new SystemOneRequest(
            "Help! My payouts have failed for 3 days.",
            new Dictionary<string, JevQuestion>
            {
                ["department"] = new ChoiceQuestion(
                    "Which team?",
                    new Dictionary<string, string?>
                    {
                        ["billing"] = "Payments",
                        ["technical"] = "Bugs"
                    }),
                ["frustration"] = new ScoreQuestion(
                    "How frustrated?",
                    ["Calm", "Frustrated"]),
                ["urgent"] = new NoulQuestion("Is it urgent?")
            }));

        Assert.Equal("Bearer test-key", capturedAuthorization);
        using var requestJson = JsonDocument.Parse(capturedJson!);
        var questions = requestJson.RootElement.GetProperty("questions");
        Assert.Equal("choice", questions.GetProperty("department").GetProperty("type").GetString());
        Assert.Equal("score", questions.GetProperty("frustration").GetProperty("type").GetString());
        Assert.Equal("noul", questions.GetProperty("urgent").GetProperty("type").GetString());

        var choice = Assert.IsType<ChoiceAnswer>(result.Answers["department"]);
        Assert.Equal("technical", choice.Choice);
        Assert.Equal(0.78, choice.Confidence);
        Assert.IsType<ScoreAnswer>(result.Answers["frustration"]);
        Assert.Equal(0.95, Assert.IsType<NoulAnswer>(result.Answers["urgent"]).Noul);
        Assert.Equal(300, result.Usage.InputTokens);
    }

    [Fact]
    public async Task EvaluateAsync_RetriesRateLimitResponse()
    {
        var attempts = 0;
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            attempts++;
            return Task.FromResult(attempts == 1
                ? JsonResponse(HttpStatusCode.TooManyRequests, "{\"error\":\"slow down\"}")
                : JsonResponse(HttpStatusCode.OK, """
                    {
                      "model": "jev-1.13.0",
                      "answers": { "urgent": { "type": "noul", "noul": 0.9 } },
                      "usage": { "input_tokens": 10, "output_tokens": 2 }
                    }
                    """));
        });

        using var httpClient = new HttpClient(handler);
        using var client = new JevClient(httpClient, "test-key", new JevClientOptions
        {
            MaxRetries = 1,
            RetryBaseDelay = TimeSpan.Zero
        });

        await client.EvaluateAsync(NoulRequest());

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task EvaluateAsync_ThrowsApiExceptionWithResponseBody()
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(
            JsonResponse(HttpStatusCode.UnprocessableEntity, "{\"detail\":\"invalid question\"}")));

        using var httpClient = new HttpClient(handler);
        using var client = new JevClient(httpClient, "test-key");

        var exception = await Assert.ThrowsAsync<JevApiException>(
            () => client.EvaluateAsync(NoulRequest()));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, exception.StatusCode);
        Assert.Contains("invalid question", exception.ResponseBody);
    }

    private static SystemOneRequest NoulRequest() => new(
        "State",
        new Dictionary<string, JevQuestion>
        {
            ["urgent"] = new NoulQuestion("Is it urgent?")
        });

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => handler(request, cancellationToken);
    }
}
