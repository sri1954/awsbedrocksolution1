using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;

class Program
{
    private const string ModelId = "amazon.nova-pro-v1:0";

    static async Task Main(string[] args)
    {
        var client = new AmazonBedrockRuntimeClient(RegionEndpoint.APSoutheast2);

        Console.WriteLine("AWS Bedrock Nova-Pro Chat (Sydney Region)");
        Console.WriteLine("Type 'exit' to quit.\n");

        while (true)
        {
            Console.Write("You: ");
            string userInput = Console.ReadLine()!;
            if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            string reply = await ChatWithNova(client, userInput);
            Console.WriteLine($"Bot: {reply}\n");
        }
    }

    private static async Task<string> ChatWithNova(AmazonBedrockRuntimeClient client, string userInput)
    {
        var payload = new
        {
            messages = new object[]
            {
                new {
                    role = "user",
                    content = new object[]
                    {
                        new { text = userInput }
                    }
                }
            },
            inferenceConfig = new
            {
                maxTokens = 300,
                temperature = 0.7,
                topP = 0.9
            }
        };

        var request = new InvokeModelRequest
        {
            ModelId = ModelId,
            ContentType = "application/json",
            Accept = "application/json",
            Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)))
        };

        var response = await client.InvokeModelAsync(request);

        using var reader = new StreamReader(response.Body);
        var body = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(body);

        // Nova puts text inside outputText
        if (doc.RootElement.TryGetProperty("outputText", out var textElement))
            return textElement.GetString()!;

        // Sometimes it's inside "results" array depending on SDK updates
        if (doc.RootElement.TryGetProperty("results", out var results)
            && results.ValueKind == JsonValueKind.Array
            && results[0].TryGetProperty("outputText", out var outputText))
            return outputText.GetString()!;

        return body; // fallback
    }
}
