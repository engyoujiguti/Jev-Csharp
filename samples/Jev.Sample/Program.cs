using Jev.Client;
using Jev.Client.Models;

var apiKey = Environment.GetEnvironmentVariable("TYPESAFE_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("Set TYPESAFE_API_KEY before running this sample.");
    return 1;
}

using var client = new JevClient(apiKey);

var response = await client.EvaluateAsync(new SystemOneRequest(
    state: "Stripe連携が3日間失敗しています。売上に影響しているので至急助けてください。",
    questions: new Dictionary<string, JevQuestion>
    {
        ["department"] = new ChoiceQuestion(
            "どのチームが対応すべきですか？",
            new Dictionary<string, string?>
            {
                ["billing"] = "支払い、請求、返金に関する問題",
                ["technical"] = "バグ、障害、外部サービス連携の問題",
                ["sales"] = "価格、アップグレード、新規契約に関する相談"
            }),
        ["frustration"] = new ScoreQuestion(
            "顧客の不満の強さを評価してください。",
            ["冷静", "不満がある", "非常に怒っている"]),
        ["is_urgent"] = new NoulQuestion("緊急性がありますか？")
    }));

var department = (ChoiceAnswer)response.Answers["department"];
var frustration = (ScoreAnswer)response.Answers["frustration"];
var urgency = (NoulAnswer)response.Answers["is_urgent"];

Console.WriteLine($"Model: {response.Model}");
Console.WriteLine($"Department: {department.Choice} (confidence: {department.Confidence:P0})");
Console.WriteLine($"Frustration: {frustration.Score:F2} (confidence: {frustration.Confidence:P0})");
Console.WriteLine($"Urgency: {urgency.Noul:P0}");
Console.WriteLine($"Tokens: input={response.Usage.InputTokens}, output={response.Usage.OutputTokens}");

return 0;
