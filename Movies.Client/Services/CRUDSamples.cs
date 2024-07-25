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
        //await CreateResourceAsync();
        //await UpdateResourceAsync();
        //await DeleteReseourceAsync();
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
            DirectorId = Guid.Parse("d28888e9-2ba9-473a-a40f-e38cb54f9b35"),
            ReleaseDate = new DateTimeOffset(new DateTime(1992, 9, 2)),
            Genre = "Crime, Drama"
        };

        var serializedMovieToCreate = JsonSerializer.Serialize(movieToCreate, _jsonSerializerOptionsWrapper.Options);

        var request = new HttpRequestMessage(HttpMethod.Post, "api/movies");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); //post actions often return the newly created object in the response body. we are accepting json in this case

        request.Content = new StringContent(serializedMovieToCreate);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await httpClient.SendAsync(request); //?
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        var createdMovie = JsonSerializer.Deserialize<Movie>(content, _jsonSerializerOptionsWrapper.Options);

        #region
        //shortcut
        //we pass resource Uri, new StringContent instance with serialize, encoding we want to use and media type
        //var response = await httpClient.PostAsync( 
        //    "api/movies", 
        //    new StringContent(JsonSerializer.Serialize(movieToCreate, _jsonSerializerOptionsWrapper.Options), Encoding.UTF8, "application/json"));
        //response.EnsureSuccessStatusCode();

        //var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        //var createdMovie = JsonSerializer.Deserialize<Movie>(content, _jsonSerializerOptionsWrapper.Options);
        #endregion
    }

    public async Task UpdateResourceAsync() //with put method, it doesn't allow partial aupdates, so we need to update description but we can't only enter description field
    {
        var httpClient = _httpClientFactory.CreateClient("MoviesAPIClient"); //1.create client

        var movieToUpdate = new MovieForUpdate() //2.seed object with data
        {
            Title = "Pulp Fiction",
            Description = "The movie with Zed",
            DirectorId = Guid.Parse("D28888E9-2BA9-473A-A40F-E38CB54F9B35"),
            ReleaseDate = new DateTimeOffset(new DateTime(1992, 9, 2)),
            Genre = "Crime, Drama"
        };

        var serializedMovieToUpdate = JsonSerializer.Serialize(movieToUpdate, _jsonSerializerOptionsWrapper.Options); //3.serialize object

        var request = new HttpRequestMessage(HttpMethod.Put, "api/movies/5b1c2b4d-48c7-402a-80c3-cc796ad49c6b"); //url is id of movie 
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); //4.set that we accept json as reponse

        request.Content = new StringContent(serializedMovieToUpdate); //5. set content we pass
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await httpClient.SendAsync(request); //6.send request
        response.EnsureSuccessStatusCode(); //7.ensure it is success

        var content = await response.Content.ReadAsStringAsync(); //read out content
        var updatedMovie = JsonSerializer.Deserialize<Movie>(content, _jsonSerializerOptionsWrapper.Options);
    }

    public async Task DeleteReseourceAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("MoviesAPIClient");

        var request = new HttpRequestMessage(HttpMethod.Delete, "api/movies/5b1c2b4d-48c7-402a-80c3-cc796ad49c6b"); //succesful delete request return 204 response with an empty body,
                                                                                                                    //but we still want to add accept header, that's beacause some
                                                                                                                    //APIs return content in case something goes wrong and that has to be serialized
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(); //if delete goes well this should be empty
    }
}
