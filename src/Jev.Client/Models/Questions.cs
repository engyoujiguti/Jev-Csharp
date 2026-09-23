using System.Text.Json.Serialization;

namespace Jev.Client.Models;

/// <summary>Base type for a typed question sent to Jev.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(NoulQuestion), "noul")]
[JsonDerivedType(typeof(ChoiceQuestion), "choice")]
[JsonDerivedType(typeof(ScoreQuestion), "score")]
public abstract record JevQuestion
{
    private protected JevQuestion(object instructions)
    {
        ArgumentNullException.ThrowIfNull(instructions);
        Instructions = instructions;
    }

    /// <summary>The question or structured instructions Jev evaluates.</summary>
    [JsonPropertyName("instructions")]
    public object Instructions { get; }
}

/// <summary>A yes/no question whose answer is a probability from 0 to 1.</summary>
public sealed record NoulQuestion : JevQuestion
{
    /// <summary>Creates a Noul question.</summary>
    public NoulQuestion(object instructions, NoulCriteria? criteria = null)
        : base(instructions)
    {
        Criteria = criteria;
    }

    /// <summary>Optional descriptions of the true and false outcomes.</summary>
    [JsonPropertyName("criteria")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NoulCriteria? Criteria { get; }
}

/// <summary>Describes what true and false mean for a Noul question.</summary>
public sealed record NoulCriteria(
    [property: JsonPropertyName("true")] object? True,
    [property: JsonPropertyName("false")] object? False);

/// <summary>A question that selects one option from a fixed set.</summary>
public sealed record ChoiceQuestion : JevQuestion
{
    /// <summary>Creates a Choice question with structured criteria.</summary>
    public ChoiceQuestion(object instructions, IReadOnlyDictionary<string, object?> criteria)
        : base(instructions)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        if (criteria.Count == 0)
        {
            throw new ArgumentException("At least one choice is required.", nameof(criteria));
        }

        Criteria = criteria;
    }

    /// <summary>Creates a Choice question with string criteria.</summary>
    public ChoiceQuestion(string instructions, IReadOnlyDictionary<string, string?> criteria)
        : this(
            instructions,
            criteria.ToDictionary(
                static pair => pair.Key,
                static pair => (object?)pair.Value))
    {
    }

    /// <summary>The selectable options and their descriptions.</summary>
    [JsonPropertyName("criteria")]
    public IReadOnlyDictionary<string, object?> Criteria { get; }
}

/// <summary>A question that scores the state against an ordered rubric.</summary>
public sealed record ScoreQuestion : JevQuestion
{
    /// <summary>Creates a Score question with structured levels.</summary>
    public ScoreQuestion(object instructions, IReadOnlyList<object> criteria)
        : base(instructions)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        if (criteria.Count is < 2 or > 10)
        {
            throw new ArgumentOutOfRangeException(
                nameof(criteria),
                "A score requires between 2 and 10 levels.");
        }

        Criteria = criteria;
    }

    /// <summary>Creates a Score question with string levels.</summary>
    public ScoreQuestion(string instructions, IReadOnlyList<string> criteria)
        : this(instructions, criteria.Cast<object>().ToArray())
    {
    }

    /// <summary>The ordered score levels.</summary>
    [JsonPropertyName("criteria")]
    public IReadOnlyList<object> Criteria { get; }
}

/// <summary>A complete System One evaluation request.</summary>
public sealed record SystemOneRequest
{
    /// <summary>Creates an evaluation request.</summary>
    public SystemOneRequest(
        object state,
        IReadOnlyDictionary<string, JevQuestion> questions)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(questions);
        if (questions.Count == 0)
        {
            throw new ArgumentException("At least one question is required.", nameof(questions));
        }

        State = state;
        Questions = questions;
    }

    /// <summary>The text or structured state to evaluate.</summary>
    [JsonPropertyName("state")]
    public object State { get; }

    /// <summary>The model alias or version to call.</summary>
    [JsonPropertyName("model")]
    public string Model { get; init; } = "jev-latest";

    /// <summary>The questions, keyed by caller-defined identifiers.</summary>
    [JsonPropertyName("questions")]
    public IReadOnlyDictionary<string, JevQuestion> Questions { get; }
}
