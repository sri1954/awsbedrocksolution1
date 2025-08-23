using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using CoreApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoreApp.Services
{

    public class AwsChatService
    {
        private readonly AmazonBedrockRuntimeClient _client = new(RegionEndpoint.APSoutheast2);
        private const string ModelId = "amazon.nova-pro-v1:0";

        public async Task<string> GetResponseAsync(List<ChatMessage> messages)
        {
            try
            {
                var payload = new
                {
                    messages = messages.Select(m => new
                    {
                        role = m.Role,  // "user" or "assistant"
                        content = new[]
                        {
                    new { text = m.Content }
                }
                    }).ToArray(),
                    inferenceConfig = new
                    {
                        maxTokens = 300,
                        temperature = 0.7,
                        topP = 0.9
                    }
                };

                var request = new InvokeModelRequest
                {
                    ModelId = ModelId, // e.g. "amazon.nova-pro-v1:0"
                    ContentType = "application/json",
                    Accept = "application/json",
                    Body = new MemoryStream(
                        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)))
                };

                var response = await _client.InvokeModelAsync(request);
                using var reader = new StreamReader(response.Body);
                var body = await reader.ReadToEndAsync();

                using var doc = JsonDocument.Parse(body);

                // Nova responses typically return: { "output": { "message": { "content": [ { "text": "..." } ] } } }
                if (doc.RootElement.TryGetProperty("output", out var output)
                    && output.TryGetProperty("message", out var message)
                    && message.TryGetProperty("content", out var contentArr)
                    && contentArr.ValueKind == JsonValueKind.Array
                    && contentArr.GetArrayLength() > 0
                    && contentArr[0].TryGetProperty("text", out var textElement))
                {
                    return textElement.GetString()!;
                }
                return body;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error invoking model: {ex}");
                return "An error occurred while processing your request.";
            }
        }


        // Recursively scan a JsonElement and return the longest string found (or null)
        private static string FindLongestStringValue(JsonElement el)
        {
            string best = null;

            void Recurse(JsonElement e)
            {
                switch (e.ValueKind)
                {
                    case JsonValueKind.String:
                        var s = e.GetString();
                        if (!string.IsNullOrEmpty(s) && (best == null || s.Length > best.Length))
                            best = s;
                        break;

                    case JsonValueKind.Object:
                        foreach (var p in e.EnumerateObject())
                            Recurse(p.Value);
                        break;

                    case JsonValueKind.Array:
                        foreach (var item in e.EnumerateArray())
                            Recurse(item);
                        break;

                    default:
                        break;
                }
            }

            Recurse(el);
            return best!;
        }

    }
}
