using Microsoft.AspNetCore.JsonPatch;
using Movies.Client.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Movies.Client.Services;

public class PartialUpdateSamples : IIntegrationService //we use Newtonsoft.Json for partial update because it's a requirement, so we add it's support in API program.cs
{
    private readonly IHttpClientFactory _httpClientFactory;
    public PartialUpdateSamples(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    public async Task RunAsync()
    {
        //await PatchResourceAsync();
        //await PatchResourceShortcutAsync();
    }

    //public async Task PatchResourceAsync()
    //{
    //    var httpClient = _httpClientFactory.CreateClient("MoviesAPIClient");

    //    var patchDoc = new JsonPatchDocument<MovieForUpdate>();
    //    patchDoc.Replace(m => m.Title, "Updated Title");
    //    patchDoc.Remove(m => m.Description);

    //    var serializedChangeSet = JsonConvert.SerializeObject(patchDoc);
    //    var request = new HttpRequestMessage(HttpMethod.Patch, "api/movies/bb6a100a-053f-4bf8-b271-60ce3aae6eb5");
    //    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    //    request.Content = new StringContent(serializedChangeSet);
    //    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json-patch+json");

    //    var response = await httpClient.SendAsync(request);
    //    response.EnsureSuccessStatusCode();

    //    var content = await response.Content.ReadAsStringAsync();   
    //    var updatedMovie = JsonConvert.DeserializeObject<Movie>(content);
    //}

    public async Task PatchResourceShortcutAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("MoviesAPIClient");

        var patchDoc = new JsonPatchDocument<MovieForUpdate>();
        patchDoc.Replace(m => m.Title, "Updated Title");
        patchDoc.Remove(m => m.Description);

        var response = await httpClient.PatchAsync("api/movies/bb6a100a-053f-4bf8-b271-60ce3aae6eb5",
            new StringContent(JsonConvert.SerializeObject(patchDoc), encoding: UTF8Encoding.UTF8, "application/json-patch+json"));

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var updatedMovie = JsonConvert.DeserializeObject<Movie>(content);
    }
}
