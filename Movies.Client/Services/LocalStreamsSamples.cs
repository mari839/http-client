using Movies.Client.Helpers;
using Movies.Client.Models;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Movies.Client.Services;

public class LocalStreamsSamples : IIntegrationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptionsWrapper _jsonSerializerOptionsWrapper;
    public LocalStreamsSamples(IHttpClientFactory httpClientFactory, JsonSerializerOptionsWrapper jsonSerializerOptionsWrapper)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _jsonSerializerOptionsWrapper = jsonSerializerOptionsWrapper ?? throw new ArgumentNullException(nameof(jsonSerializerOptionsWrapper));
    }
    public async Task RunAsync()
    {
        //await GetPosterWithSteamAsync();
        //await GetPosterWithSteamAndCompletionModeAsync();
        //await TestMethodAsync(() => GetPosterWithoutSteamAsync());
        //await TestMethodAsync(() => GetPosterWithSteamAsync());
        //await TestMethodAsync(() => GetPosterWithStreamAndCompletionModeAsync());
        //await PostPosterWithStreamAsync();
        //await PostAndReadPosterWithStreamAsync();

        await TestMethodAsync(() => PostPosterWithoutStreamsAsync());
        await TestMethodAsync(() => PostPosterWithStreamAsync());
        await TestMethodAsync(() => PostAndReadPosterWithStreamAsync());
    }

    private async Task GetPosterWithSteamAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("MoviesAPIClient");

        var request = new HttpRequestMessage(HttpMethod.Get, $"api/movies/3d2880ae-5ba6-417c-845d-f4ebfd4bcac7/posters/{Guid.NewGuid()}");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        using (var response = await httpClient.SendAsync(request))// when working with streams we need to dispose steam content object, response is disposed
        {
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync(); //creates stream
            var poster = await JsonSerializer.DeserializeAsync<Poster>(stream, _jsonSerializerOptionsWrapper.Options); //System.Text.Json supports reading from streams
        }
    }

    private async Task GetPosterWithStreamAndCompletionModeAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("MoviesAPIClient");

        var request = new HttpRequestMessage(HttpMethod.Get, $"api/movies/3d2880ae-5ba6-417c-845d-f4ebfd4bcac7/posters/{Guid.NewGuid()}");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)) //HttpCompletionMode can be used to specify when an async operation is considered complete, when all of the response is read or when re header is read
        {
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            var poster = await JsonSerializer.DeserializeAsync<Poster>(stream, _jsonSerializerOptionsWrapper.Options);
        }
    }

    private async Task GetPosterWithoutSteamAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("MoviesAPIClient");

        var request = new HttpRequestMessage(HttpMethod.Get, $"api/movies/3d2880ae-5ba6-417c-845d-f4ebfd4bcac7/posters/{Guid.NewGuid()}");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await httpClient.SendAsync(request); //HttpCompletionMode can be used to specify when an async operation is considered complete, when all of the response is read or when re header is read
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var poster = JsonSerializer.Deserialize<Poster>(content, _jsonSerializerOptionsWrapper.Options);
    }

    public async Task TestMethodAsync(Func<Task> functionToTest)
    {
        //warmup
        await functionToTest();

        //start stopwatch
        var stopWatch = Stopwatch.StartNew();

        //run requests
        for (int i = 0; i < 10; i++)
        {
            await functionToTest();
        }

        //stop stopwatch
        stopWatch.Stop();
        Console.WriteLine($"Elapsed milliseconds without stream: {stopWatch.ElapsedMilliseconds}, " + $"averaging {stopWatch.ElapsedMilliseconds / 200} milliseconds/request");
    }

    private async Task PostPosterWithStreamAsync()
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

                using (var streamContent = new StreamContent(memoryContentStream))
                {
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    request.Content = streamContent;

                    var response = await httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    var poster = JsonSerializer.Deserialize<Poster>(content, _jsonSerializerOptionsWrapper.Options);
                }
            }
        }
    }

    private async Task PostAndReadPosterWithStreamAsync()
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

                using (var streamContent = new StreamContent(memoryContentStream))
                {
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    request.Content = streamContent;

                    var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    var stream = await response.Content.ReadAsStreamAsync();
                    var poster = await JsonSerializer.DeserializeAsync<Poster>(stream, _jsonSerializerOptionsWrapper.Options);
                }
            }
        }
    }

    public async Task PostPosterWithoutStreamsAsync()
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

        var serializedPosterToCreate = JsonSerializer.Serialize(posterForCreation, _jsonSerializerOptionsWrapper.Options);

        var request = new HttpRequestMessage(HttpMethod.Post, "api/movies/d8663e5e-7494-4f81-8739-6e0de1bea7ee/posters");

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        request.Content = new StringContent(serializedPosterToCreate);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");


        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var poster = JsonSerializer.Deserialize<Poster>(content, _jsonSerializerOptionsWrapper.Options);
    }
}
