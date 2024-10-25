using Movies.Client.Helpers;
using Movies.Client.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Movies.Client.Services;

public class HttpClientFactorySamples : IIntegrationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptionsWrapper _jsonSerializerOptionsWrapper;
    private readonly MoviesAPIClient _movieAPIClient;
    public HttpClientFactorySamples(IHttpClientFactory httpClientFactory, JsonSerializerOptionsWrapper jsonSerializerOptionsWrapper, MoviesAPIClient moviesAPIClient) 
    { 
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _jsonSerializerOptionsWrapper = jsonSerializerOptionsWrapper ?? throw new ArgumentNullException(nameof(jsonSerializerOptionsWrapper));
        _movieAPIClient = moviesAPIClient ?? throw new ArgumentNullException(nameof(_movieAPIClient));
    }

    public async Task RunAsync()
    {
        //await TestDisposeHttpClientAsync();
        //await TestReuseHttpClientAsync();
        //await GetFilmsAsync();
        //await GetMoviesWithTypedHttpClientAsync();
        await GetMoviesViaMoviesAPICLientAsync();
    }

    private async Task TestDisposeHttpClientAsync() //we can see open sockets by netstat -abn, they are in TIME_WAIT status instead of ESTABILISHED
    {
        for(var i =0; i <10; i++)
        {
            using(var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    "https://www.google.com");

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Request completed with status code " + $"{response.StatusCode}");
            }
        }
    }

    private async Task TestReuseHttpClientAsync() //the HttpCLient and underlying handler is not disposed while application is running, so socket is in estabilished status
    {
        var httpClient = new HttpClient();

        for(var i =0; i <10;i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                    "https://www.google.com");

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Request completed with status code " + $"{response.StatusCode}");
        }
    }

    private async Task GetFilmsAsync() //using named clients
    {
        var httpClient = _httpClientFactory.CreateClient("MoviesAPIClient");

        var request = new HttpRequestMessage(HttpMethod.Get, "api/films");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); //post actions often return the newly created object in the response body. we are accepting json in this case

        var response = await httpClient.SendAsync(request); //?
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        var movies = JsonSerializer.Deserialize<IEnumerable<Movie>>(content, _jsonSerializerOptionsWrapper.Options);
    }

    //private async Task GetMoviesWithTypedHttpClientAsync()
    //{
    //    var request = new HttpRequestMessage(HttpMethod.Get, "api/movies");
    //    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    //    var response = await _movieAPIClient.Client.SendAsync(request);
    //    response.EnsureSuccessStatusCode();

    //    var content = await response.Content.ReadAsStringAsync();
    //    var movies = JsonSerializer.Deserialize<IEnumerable<Movie>>(content, _jsonSerializerOptionsWrapper.Options);
    //}

    private async Task GetMoviesViaMoviesAPICLientAsync()
    {
        var movies = await _movieAPIClient.GetMoviesAsync();
    }
}
