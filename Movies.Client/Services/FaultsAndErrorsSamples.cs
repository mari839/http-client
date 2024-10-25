using Microsoft.AspNetCore.Mvc;
using Movies.Client.Helpers;
using Movies.Client.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;

namespace Movies.Client.Services;

public class FaultsAndErrorsSamples : IIntegrationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptionsWrapper _jsonSerializerOptionsWrapper;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    public FaultsAndErrorsSamples(IHttpClientFactory httpClientFactory, JsonSerializerOptionsWrapper jsonSerializerOptionsWrapper)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _jsonSerializerOptionsWrapper = jsonSerializerOptionsWrapper ?? throw new ArgumentNullException(nameof(jsonSerializerOptionsWrapper));
    }
    public async Task RunAsync()
    {
        await GetMovieAndDealWithInvalidReposnsesAsync(CancellationToken.None);
        //await PostMovieAndHandleErrorsAsync(CancellationToken.None);
    }

    private async Task PostMovieAndHandleErrorsAsync(CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("MoviesAPIClient"); //1.create client

        var movieToCreate = new MovieForCreation()
        {

        };

        var serializedMovieToCreate = JsonSerializer.Serialize(movieToCreate, _jsonSerializerOptionsWrapper.Options);

        using (var request = new HttpRequestMessage(HttpMethod.Post, "api/movies"))
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            request.Content = new StringContent(serializedMovieToCreate);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                if(!response.IsSuccessStatusCode)
                {
                    //inspect status code
                    if(response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        //read out the rersponse body and log it to the console windows
                        var errorStream = await response.Content.ReadAsStreamAsync();

                        //var errorAsProblemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(errorStream, _jsonSerializerOptionsWrapper.Options); //this doesnt include ErrorDetails so we use more specific class

                        //ValidationProblemDetails doesnt' have setters so we create new class with Dictionary and setters to get errors and derives ProblemDetails class
                        //var errorAsProblemDetails = await JsonSerializer.DeserializeAsync<ValidationProblemDetails>(errorStream, _jsonSerializerOptionsWrapper.Options);
                        var errorAsProblemDetails = await JsonSerializer.DeserializeAsync<ExtendedProblemDetailsWithErrors>(errorStream, _jsonSerializerOptionsWrapper.Options);

                        var errors = errorAsProblemDetails?.Errors;
                        Console.WriteLine(errorAsProblemDetails?.Title);
                        return;
                    }
                }
                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync();
                var poster = await JsonSerializer.DeserializeAsync<Poster>(stream, _jsonSerializerOptionsWrapper.Options);
            }

        }
    }
    private async Task GetMovieAndDealWithInvalidReposnsesAsync(CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("MoviesAPIClient");

        var request = new HttpRequestMessage(HttpMethod.Get, $"api/movie/5B1C2B4D-48C7-402A-80C3-CC796AD49C6B");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip")); //can be used to ask for specific encoding if the API supports it, accepts responses that are compressed with gzip format

        using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine("The requested movie can not be found");
                    return;
                }
                response.EnsureSuccessStatusCode();
            }
            var stream = await response.Content.ReadAsStreamAsync();

            //we need to deserialize the compressed file which we configured in Program.cs
            var movie = await JsonSerializer.DeserializeAsync<Movie>(stream, _jsonSerializerOptionsWrapper.Options);
        }
    }
}