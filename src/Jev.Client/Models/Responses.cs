using System.Text.Json.Serialization;

namespace Jev.Client.Models;

/// <summary>The result of a System One evaluation.</summary>
public sealed record SystemOneResponse
{
    /// <summary>The model version that handled the request.</summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>Answers keyed by the question identifiers in the request.</summary>
    [JsonPropertyName("answers")]
    public required Dictionary<string, JevAnswer> Answers { get; init; }

    /// <summary>Token usage for the request.</summary>
    [JsonPropertyName("usage")]
    public required JevUsage Usage { get; init; }
}

/// <summary>Base type for an answer returned by Jev.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(NoulAnswer), "noul")]
[JsonDerivedType(typeof(ChoiceAnswer), "choice")]
[JsonDerivedType(typeof(ScoreAnswer), "score")]
public abstract record JevAnswer;

/// <summary>A Noul probability where 0 means no and 1 means yes.</summary>
public sealed record NoulAnswer : JevAnswer
{
    /// <summary>The probability that the answer is yes.</summary>
    [JsonPropertyName("noul")]
    public required double Noul { get; init; }
}

/// <summary>The selected Choice option and its probability distribution.</summary>
public sealed record ChoiceAnswer : JevAnswer
{
    /// <summary>The highest-probability option.</summary>
    [JsonPropertyName("choice")]
    public required string Choice { get; init; }

    /// <summary>The probability assigned to every option.</summary>
    [JsonPropertyName("probabilities")]
    public required Dictionary<string, double> Probabilities { get; init; }

    /// <summary>The confidence derived from the probability distribution.</summary>
    [JsonPropertyName("confidence")]
    public required double Confidence { get; init; }
}

/// <summary>The probability-weighted Score and its level distribution.</summary>
public sealed record ScoreAnswer : JevAnswer
{
    /// <summary>The probability-weighted score, which can fall between levels.</summary>
    [JsonPropertyName("score")]
    public required double Score { get; init; }

    /// <summary>Maps each numeric level back to its description.</summary>
    [JsonPropertyName("legend")]
    public required Dictionary<string, string> Legend { get; init; }

    /// <summary>The probability assigned to every level.</summary>
    [JsonPropertyName("probabilities")]
    public required Dictionary<string, double> Probabilities { get; init; }

    /// <summary>The confidence derived from the probability distribution.</summary>
    [JsonPropertyName("confidence")]
    public required double Confidence { get; init; }
}

/// <summary>Token usage reported by the API.</summary>
public sealed record JevUsage
{
    /// <summary>The number of input tokens.</summary>
    [JsonPropertyName("input_tokens")]
    public required int InputTokens { get; init; }

    /// <summary>The number of output tokens.</summary>
    [JsonPropertyName("output_tokens")]
    public required int OutputTokens { get; init; }
}
