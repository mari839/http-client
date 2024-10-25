using Movies.Client.Helpers;
using Movies.Client.Models;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Movies.Client.Services;

public class CompressionSamples : IIntegrationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptionsWrapper _jsonSerializerOptionsWrapper;

    public CompressionSamples(IHttpClientFactory httpClientFactory, JsonSerializerOptionsWrapper jsonSerializerOptionsWrapper)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _jsonSerializerOptionsWrapper = jsonSerializerOptionsWrapper ?? throw new ArgumentNullException(nameof(jsonSerializerOptionsWrapper));
    }

    public async Task RunAsync()
    {
        //await GetPorsterWithGZipCompression();
        await SendAndReceivePosterWithGZipCompressionAsync();
    }

    public async Task GetPorsterWithGZipCompression()
    {
        var httpClient = _httpClientFactory.CreateClient("MoviesAPIClient");

        var request = new HttpRequestMessage(HttpMethod.Get, $"api/movies/5B1C2B4D-48C7-402A-80C3-CC796AD49C6B/posters/{Guid.NewGuid()}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip")); //can be used to ask for specific encoding if the API supports it, accepts responses that are compressed with gzip format

        using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
        {
            var stream = await response.Content.ReadAsStreamAsync();

            response.EnsureSuccessStatusCode();

            //we need to deserialize the compressed file which we configured in Program.cs
            var poster = await JsonSerializer.DeserializeAsync<Poster>(stream, _jsonSerializerOptionsWrapper.Options);
        }
    }

    public async Task SendAndReceivePosterWithGZipCompressionAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("MoviesAPIClient");

        //generate movie poster og 5mb
        var random = new Random();
        var generatedBytes = new byte[5242880];
        random.NextBytes(generatedBytes);

        var posterForCreation = new PosterForCreation()
        {
            Name = "A new poster for the Big Lebowski",
            Bytes = generatedBytes
        };

        using (var memoryContentStream = new MemoryStream())
        {
            await JsonSerializer.SerializeAsync(memoryContentStream, posterForCreation);

            //set memory stream back to position 0, as that is where we want to start streaming it from
            memoryContentStream.Seek(0, SeekOrigin.Begin);

            using (var request = new HttpRequestMessage(HttpMethod.Post, "api/movies/d8663e5e-7494-4f81-8739-6e0de1bea7ee/posters"))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

                using (var compressedMemoryContentStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(compressedMemoryContentStream, CompressionMode.Compress)) //we will use it to compress the incoming memoryContentStream
                    {
                        memoryContentStream.CopyTo(gzipStream); //memoryContentStream is uncompressed and we copy it to gzipStream and flush it
                        gzipStream.Flush();
                        compressedMemoryContentStream.Position = 0; //compressedMemoryContentStream will contain the compressed data coming from the memoryContentStream

                        using (var streamContent = new StreamContent(compressedMemoryContentStream))
                        {
                            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                            streamContent.Headers.ContentEncoding.Add("gzip"); //lets our API know that request body is encoded and how it's encoded

                            request.Content = streamContent;

                            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                            response.EnsureSuccessStatusCode();

                            var stream = await response.Content.ReadAsStreamAsync();
                            var poster = await JsonSerializer.DeserializeAsync<Poster>(stream, _jsonSerializerOptionsWrapper.Options);
                        }
                    }
                }
            }
        }
    }
}

