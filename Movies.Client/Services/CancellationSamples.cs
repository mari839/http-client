using Movies.Client.Helpers;
using Movies.Client.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;

namespace Movies.Client.Services;

public class CancellationSamples : IIntegrationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptionsWrapper _jsonSerializerOptionsWrapper;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    public CancellationSamples(IHttpClientFactory httpClientFactory, JsonSerializerOptionsWrapper jsonSerializerOptionsWrapper)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _jsonSerializerOptionsWrapper = jsonSerializerOptionsWrapper ?? throw new ArgumentNullException(nameof(jsonSerializerOptionsWrapper));
    }

    public async Task RunAsync()
    {
        //_cancellationTokenSource.CancelAfter(200);
        //await GetTrailerAndCancelAsync(_cancellationTokenSource.Token);
        await GetTrailerAndHandleTimoutAsync();
    }

    private async Task GetTrailerAndCancelAsync(CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("MoviesAPIClient");

        var request = new HttpRequestMessage(HttpMethod.Get, $"api/movies/5B1C2B4D-48C7-402A-80C3-CC796AD49C6B/trailers/{Guid.NewGuid()}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip")); //can be used to ask for specific encoding if the API supports it, accepts responses that are compressed with gzip format

        try
        {

        using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            var stream = await response.Content.ReadAsStreamAsync();

            response.EnsureSuccessStatusCode();

            //we need to deserialize the compressed file which we configured in Program.cs
            var poster = await JsonSerializer.DeserializeAsync<Trailer>(stream, _jsonSerializerOptionsWrapper.Options);
        }
        }catch (OperationCanceledException ocException) //handles cancellation with message
        {
            Console.WriteLine($"An operation was cancelled with message {ocException.Message}.");
        }
    }

    private async Task GetTrailerAndHandleTimoutAsync() //this handles cancellation when timout happens, we configure timoout span in program cs
    {
        var httpClient = _httpClientFactory.CreateClient("MoviesAPIClient");

        var request = new HttpRequestMessage(HttpMethod.Get, $"api/movies/5B1C2B4D-48C7-402A-80C3-CC796AD49C6B/trailers/{Guid.NewGuid()}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip")); //can be used to ask for specific encoding if the API supports it, accepts responses that are compressed with gzip format

        try
        {

            using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                var stream = await response.Content.ReadAsStreamAsync();

                response.EnsureSuccessStatusCode();

                //we need to deserialize the compressed file which we configured in Program.cs
                var poster = await JsonSerializer.DeserializeAsync<Trailer>(stream, _jsonSerializerOptionsWrapper.Options);
            }
        }
        catch (OperationCanceledException ocException) //handles cancellation with message
        {
            Console.WriteLine($"An operation was cancelled with message {ocException.Message}.");
        }
    }
}
