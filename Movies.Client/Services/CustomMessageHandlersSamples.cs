using Movies.Client.Helpers;
using Movies.Client.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;

namespace Movies.Client.Services;

public class CustomMessageHandlersSamples : IIntegrationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptionsWrapper _jsonSerializerOptionsWrapper;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    public CustomMessageHandlersSamples(IHttpClientFactory httpClientFactory, JsonSerializerOptionsWrapper jsonSerializerOptionsWrapper)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _jsonSerializerOptionsWrapper = jsonSerializerOptionsWrapper ?? throw new ArgumentNullException(nameof(jsonSerializerOptionsWrapper));
    }
    public async Task RunAsync()
    {
        await GetMovieWithCustomRetryHandlerAsync(CancellationToken.None);
    }

    public async Task GetMovieWithCustomRetryHandlerAsync(CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("MoviesAPIClientWithCustomerHandler");

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
