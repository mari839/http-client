
using Movies.Client.Helpers;
using Movies.Client.Models;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;

namespace Movies.Client
{
    public class TestableClassWithApiAccess
    {
        public readonly HttpClient _httpClient;
        private readonly JsonSerializerOptionsWrapper _jsonSerializerOptionsWrapper;
        public TestableClassWithApiAccess(HttpClient httpClient, JsonSerializerOptionsWrapper jsonSerializerOptionsWrapper)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonSerializerOptionsWrapper = jsonSerializerOptionsWrapper ?? throw new ArgumentNullException(nameof(_jsonSerializerOptionsWrapper));
        }

        public async Task<Movie?> GetMovieAsync(CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/movies/5B1C2B4D-48C7-402A-80C3-CC796AD49C64");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip")); //can be used to ask for specific encoding if the API supports it, accepts responses that are compressed with gzip format

            using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Console.WriteLine("The requested movie can not be found");
                        return null;
                    }
                    else if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedApiAccessException();
                    }
                }
                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync();

                //we need to deserialize the compressed file which we configured in Program.cs
                var movie = await JsonSerializer.DeserializeAsync<Movie>(stream, _jsonSerializerOptionsWrapper.Options);
            }
            return null;
        }
    }
}
