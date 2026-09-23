# Jev C# client

.NET 10 / C# 14 から [TypeSafe AI](https://typesafe.ai/) の Jev System One API を呼び出すためのクライアントライブラリとサンプルです。

## 必要なもの

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- TypeSafe AI の API キー

## 実行

```bash
export TYPESAFE_API_KEY="your-api-key"
dotnet run --project samples/Jev.Sample
```

API キーは [TypeSafe Console](https://console.typesafe.ai/) で取得できます。

## ライブラリの利用例

```csharp
using Jev.Client;
using Jev.Client.Models;

using var client = new JevClient(
    Environment.GetEnvironmentVariable("TYPESAFE_API_KEY")!);

var result = await client.EvaluateAsync(new SystemOneRequest(
    state: "配送が1週間遅れています。すぐに確認してください。",
    questions: new Dictionary<string, JevQuestion>
    {
        ["department"] = new ChoiceQuestion(
            "どのチームが対応すべきですか？",
            new Dictionary<string, string?>
            {
                ["shipping"] = "配送状況、遅延、紛失",
                ["billing"] = "請求、支払い、返金"
            }),
        ["urgency"] = new NoulQuestion("緊急性がありますか？")
    }));

var department = (ChoiceAnswer)result.Answers["department"];
var urgency = (NoulAnswer)result.Answers["urgency"];

Console.WriteLine($"{department.Choice}: {department.Confidence:P0}");
Console.WriteLine($"Urgency: {urgency.Noul:P0}");
```

`ChoiceQuestion`、`ScoreQuestion`、`NoulQuestion` を1回のリクエストで組み合わせられます。`state` と `instructions` には文字列だけでなく匿名型や配列などの構造化データも渡せます。

`JevClient` は HTTP 429（rate limit）と 529（overloaded）を指数バックオフで自動的に再試行します。ASP.NET Core などで `HttpClient` を管理する場合は、次のコンストラクターを利用できます。

```csharp
var client = new JevClient(httpClient, apiKey);
```

この場合、`JevClient.Dispose()` は渡された `HttpClient` を破棄しません。

## ビルドとテスト

```bash
dotnet restore Jev.slnx
dotnet build Jev.slnx --configuration Release --no-restore
dotnet test Jev.slnx --configuration Release --no-build
```

## 参考資料

- [TypeSafe AI Quick start](https://docs.typesafe.ai/introduction/quickstart)
- [TypeSafe AI API reference](https://docs.typesafe.ai/api)
