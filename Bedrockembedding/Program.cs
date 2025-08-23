using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string bucketName = "sri-test-vector";
        string indexName = "testindex1";
        string inputText = System.IO.File.ReadAllText(@"C:\DEVNOTES\sample_corpus.txt");

        var embedding = await EmbeddingService.GenerateEmbeddingAsync(inputText);
        await S3VectorService.InsertEmbeddingAsync(bucketName, indexName, "sample-key", inputText, embedding);
        await S3VectorService.QueryEmbeddingAsync(bucketName, indexName, embedding);
    }

}
