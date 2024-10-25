using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Movies.Client.Helpers
{
    public class JsonSerializerOptionsWrapper //we register this in Program.cs class as singleton 
    {
        public JsonSerializerOptions Options { get; set; }
        public JsonSerializerOptionsWrapper()
        {
            Options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            //the streaming can be visible and items will be dispalyed one by one
            Options.DefaultBufferSize = 10;
        }
    }
}
