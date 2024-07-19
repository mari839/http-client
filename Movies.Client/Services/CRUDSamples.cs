using Movies.Client.Models;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Xml.Serialization;
using System.Text;
using Movies.Client.Helpers;
namespace Movies.Client.Services;

public class CRUDSamples : IIntegrationService
{
    public readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptionsWrapper _jsonSerializerOptionsWrapper;
    public CRUDSamples(IHttpClientFactory httpClientFactory, JsonSerializerOptionsWrapper jsonSerializerOptionsWrapper)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _jsonSerializerOptionsWrapper = jsonSerializerOptionsWrapper ?? throw new ArgumentNullException(nameof(_jsonSerializerOptionsWrapper));
    }
    public async Task RunAsync()
    {
        await GetResourceAsync();
        await GetResourceThroughHttpRequestMessageAsync();
    }
    public async Task GetResourceAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("MoviesAPIClient");

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); //this tells http client that we accept json format response.
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml", 0.9)); //the second parameter is relative quality parameter, it means it's acceptable but less preferable than JSON, we support XML as a fallback option 
        //DefaultRequestHeaders.Accept is a collection which implies that we can tell the API that we are willing to accept multiple media types.

        var response = await httpClient.GetAsync("api/movies"); //this is combined with the base address we set on httpClient instance
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(); //we can also use ReadAsStringAsync().Result() but it's advised against, because .Result() blocks the current thread, we want thread to be freed up when it's no longer useful. that's why we use async await

        //var movies = JsonSerializer.Deserialize<IEnumerable<Movie>>(content); //we deserialize response into Movie object
        //if we look at movies object, it will return content with default values, which is null, because our JSON file is CamelCased and our properties aren't . System.Text.Json will try to exactly match JSON field names to property names, in our case it failed, because it's case sensitive

        //so we need to write this to ignore case sensitivity
        //we can inspect that header to know how to deserialize
        var movies = new List<Movie>();
        if (response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            movies = JsonSerializer.Deserialize<List<Movie>>(content, _jsonSerializerOptionsWrapper.Options);
        }
        else if (response.Content.Headers.ContentType?.MediaType == "application/xml")
        {
            var serializer = new XmlSerializer(typeof(List<Movie>));
            movies = serializer.Deserialize(new StringReader(content)) as List<Movie>;
        }
    }

    public async Task GetResourceThroughHttpRequestMessageAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("MoviesAPIClient");

        var request = new HttpRequestMessage(HttpMethod.Get, "api/movies"); //we pass get as method and address for resource
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await httpClient.SendAsync(request); //send http request
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var movies = JsonSerializer.Deserialize<IEnumerable<Movie>>(content, _jsonSerializerOptionsWrapper.Options);
    }

    public async Task CreateResourceAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("MoviesAPIClient");
        var movieToCreate = new MovieForCreation()
        {
            Title = "Reservoir Dogs",
            Description = "Six criminals, hired to steal diamonds, do not know each other's true identity." +
            " While attempting the heist, the police ambushes them, leading them to believe that one of " +
            "them is an undercover officer.",
            DirectorId = Guid.NewGuid(),
            ReleaseDate = new DateTimeOffset(new DateTime(1992, 9, 2)),
            Genre = "Crime, Drama"
        };

        var serializedMovieToCreate = JsonSerializer.Serialize(movieToCreate, _jsonSerializerOptionsWrapper.Options);

        var request = new HttpRequestMessage(HttpMethod.Post, "api/movies");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("Application/json")); //post actions often return the newly created object in the response body. we are accepting json in this case

        request.Content = new StringContent(serializedMovieToCreate);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await httpClient.SendAsync(request); //?
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        var createdMovie = JsonSerializer.Deserialize<Movie>(content, _jsonSerializerOptionsWrapper.Options);
    }
}
