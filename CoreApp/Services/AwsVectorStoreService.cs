using Amazon.S3Vectors;
using Amazon.S3Vectors.Model;
using CoreApp.Models;
using System.Numerics;

namespace CoreApp.Services
{
    public class AwsVectorStoreService
    {
        private readonly AmazonS3VectorsClient client = new AmazonS3VectorsClient();

        public async Task InsertEmbeddingAsync(string bucket, string index, string key, string text, float[] embedding)
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

        public async Task<List<VectorResult>> QueryEmbeddingAsync(string bucket, string index, float[] queryVector)
        {
            var request = new QueryVectorsRequest
            {
                VectorBucketName = bucket,
                IndexName = index,
                QueryVector = new VectorData { Float32 = new List<float>(queryVector) },
                TopK = 5,
                ReturnDistance = true,
                ReturnMetadata = true
            };

            var response = await client.QueryVectorsAsync(request);

            List<VectorResult> results = new List<VectorResult>();

            foreach (var vector in response.Vectors)
            {
                string documentText = "";

                if (vector.Metadata.IsDictionary())
                {
                    // Flatten metadata into a string
                    documentText = string.Join("; ",
                        vector.Metadata.AsDictionary().Select(kv => $"{kv.Key}: {kv.Value}"));
                }

                results.Add(new VectorResult
                {
                    Document = $"Key: {vector.Key} | {documentText}",
                    Score = (float)(1 - vector.Distance)!  // AWS returns *distance*, convert to similarity score
                });
            }

            return results;
        }


        public async Task<List<string>> QueryEmbeddingAsyncOld(string bucket, string index, float[] queryVector)
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
            List<string> results = new List<string>();

            foreach (var vector in response.Vectors)
            {
                Console.WriteLine($"Key: {vector.Key}, Distance: {vector.Distance}");
                if (vector.Metadata.IsDictionary())
                {
                    foreach (var meta in vector.Metadata.AsDictionary())
                    {
                        //Console.WriteLine($"  {meta.Key}: {meta.Value}");
                        results.Add(meta.Key + "-" + meta.Value);
                    }
                }
            }

            return results;
        }

        public async Task<List<string>> ListVectorBucketsAsync()
        {
            var vectorBuckets = new List<string>();
            string nextToken = null!;

            do
            {
                var request = new ListVectorBucketsRequest
                {
                    MaxResults = 100, // Adjust as needed
                    NextToken = nextToken
                };

                var response = await client.ListVectorBucketsAsync(request);
                foreach (var bucket in response.VectorBuckets)
                {
                    vectorBuckets.Add(bucket.VectorBucketName);
                }

                nextToken = response.NextToken;

            } while (nextToken != null);

            return vectorBuckets;
        }

        public async Task<List<string>> ListAllKeysAsync(string bucketName, string indexName)
        {
            var keys = new List<string>();
            string nextToken = null!;

            do
            {
                var request = new ListVectorsRequest
                {
                    VectorBucketName = bucketName,
                    IndexName = indexName,
                    NextToken = nextToken,
                    MaxResults = 100 // adjust as needed
                };

                var response = await client.ListVectorsAsync(request);
                if (response.Vectors != null)
                {
                    foreach (var vec in response.Vectors)
                    {
                        keys.Add(vec.Key);
                    }
                }

                nextToken = response.NextToken;
            } while (!string.IsNullOrEmpty(nextToken));

            return keys;
        }

        public async Task<List<GetOutputVector>> GetVectorsAsync(string bucketName, string indexName, List<string> keys)
        {
            var request = new GetVectorsRequest
            {
                VectorBucketName = bucketName,
                IndexName = indexName,
                Keys = keys,
                ReturnData = true,
                ReturnMetadata = true
            };

            var response = await client.GetVectorsAsync(request);
            return response.Vectors ?? new List<GetOutputVector>();
        }

        public async Task<bool> DeleteVectorAsync(string bucketName, string indexName, string key)
        {
            var request = new DeleteVectorsRequest
            {
                VectorBucketName = bucketName,
                IndexName = indexName,
                Keys = new List<string> { key }
            };

            var response = await client.DeleteVectorsAsync(request);
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }

        public async Task<List<string>> ListIndexesAsync(string bucketName)
        {
            var client = new AmazonS3VectorsClient();

            var response = await client.ListIndexesAsync(new ListIndexesRequest
            {
                VectorBucketName = bucketName
            });

            return response.Indexes.Select(i => i.IndexName).ToList();
        }

        public async Task DeleteIndexAsync(string bucketName, string indexName)
        {
            var request = new DeleteIndexRequest
            {
                VectorBucketName = bucketName,
                IndexName = indexName
            };

            await client.DeleteIndexAsync(request);
        }
    }
}

