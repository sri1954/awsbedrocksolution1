using Amazon.S3Vectors;
using Amazon.S3Vectors.Model;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

// Replace all usages of 'Vector' with 'PutInputVector' and ensure 'VectorData.Float32' is a List<float>.

public static class S3VectorService
{
    private static readonly AmazonS3VectorsClient client = new AmazonS3VectorsClient();

    public static async Task InsertEmbeddingAsync(string bucket, string index, string key, string text, float[] embedding)
    {
        var request = new PutVectorsRequest
        {
            VectorBucketName = bucket,
            IndexName = index,
            Vectors = new List<PutInputVector>
            {
                new PutInputVector
                {
                    Key = key,
                    Data = new VectorData { Float32 = new List<float>(embedding) },
                    Metadata = Amazon.Runtime.Documents.Document.FromObject(new Dictionary<string, string>
                    {
                        { "source_text", text },
                        { "category", "sample" }
                    })
                }
            }
        };

        await client.PutVectorsAsync(request);
        Console.WriteLine("Embedding inserted successfully.");
    }

    public static async Task QueryEmbeddingAsync(string bucket, string index, float[] queryVector)
    {
        var request = new QueryVectorsRequest
        {
            VectorBucketName = bucket,
            IndexName = index,
            QueryVector = new VectorData { Float32 = new List<float>(queryVector) },
            TopK = 3,
            ReturnDistance = true,
            ReturnMetadata = true
        };

        var response = await client.QueryVectorsAsync(request);
        Console.WriteLine("Top matches:");
        foreach (var vector in response.Vectors)
        {
            Console.WriteLine($"Key: {vector.Key}, Distance: {vector.Distance}");
            if (vector.Metadata.IsDictionary())
            {
                foreach (var meta in vector.Metadata.AsDictionary())
                {
                    Console.WriteLine($"  {meta.Key}: {meta.Value}");
                }
            }
        }
    }
}
