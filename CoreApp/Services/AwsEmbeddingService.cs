using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoreApp.Services
{

    public class AwsEmbeddingService
    {
        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var client = new AmazonBedrockRuntimeClient();
            var request = new InvokeModelRequest
            {
                ModelId = "amazon.titan-embed-text-v2:0",
                ContentType = "application/json",
                Accept = "application/json",
                Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { inputText = text })))
            };

            var response = await client.InvokeModelAsync(request);
            using var reader = new StreamReader(response.Body);
            var json = await reader.ReadToEndAsync();
            var parsed = JsonDocument.Parse(json);
            return parsed.RootElement.GetProperty("embedding").EnumerateArray().Select(x => x.GetSingle()).ToArray();
        }
    }
}
