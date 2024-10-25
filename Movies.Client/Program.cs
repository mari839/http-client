using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Movies.Client;
using Movies.Client.Handlers;
using Movies.Client.Helpers;
using Movies.Client.Services;
using Polly;
using System.Reflection.Metadata.Ecma335;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    { 
        // register services for DI
        services.AddLogging(configure => configure.AddDebug().AddConsole());

        services.AddSingleton<JsonSerializerOptionsWrapper>();

        //register service as transient because it's a lightweight service
        services.AddTransient(fact =>
        {
            return new RetryPolicyDelegatingHandler(2);
        });
        //we add our custom retrying policy RetryPolicyDelegatingHandler
        services.AddHttpClient("MoviesAPIClientWithCustomerHandler", configureClient =>
        {
            configureClient.BaseAddress = new Uri("http://localhost:5001");
            configureClient.Timeout = new TimeSpan(0, 0, 30);
        }).AddHttpMessageHandler<RetryPolicyDelegatingHandler>() //generic AddHttpMessageHandler method, beacuse we already registered service as transient
        //.AddHttpMessageHandler(()=>
        //{
        //    return new RetryPolicyDelegatingHandler(2);
        //}
        //)
        .ConfigurePrimaryHttpMessageHandler(() => //we can chain multiple handlers but PrimatyHttpMessageHandler should always be the last one in the pipeline
        {
            var handler = new SocketsHttpHandler();
            handler.AutomaticDecompression = System.Net.DecompressionMethods.GZip;
            //handler.AllowAutoRedirect = false; // this means that if url changes in API, for example api/films changes to api/movies it redirects to correct url, by default it's true (As an API developers you can make it known by returning redirection based response like 302 redirect)
            return handler;
        });



        services.AddHttpClient("MoviesAPIClient", configureClient =>
        {
            configureClient.BaseAddress = new Uri("http://localhost:5001");
            configureClient.Timeout = new TimeSpan(0, 0, 30);
        }).AddPolicyHandler(Policy.HandleResult<HttpResponseMessage>// polly helps us to send request again if it fails.
        (response => !response.IsSuccessStatusCode).RetryAsync(5))
        
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new SocketsHttpHandler();
            handler.AutomaticDecompression = System.Net.DecompressionMethods.GZip;
            //handler.AllowAutoRedirect = false; // this means that if url changes in API, for example api/films changes to api/movies it redirects to correct url, by default it's true (As an API developers you can make it known by returning redirection based response like 302 redirect)
            return handler;
        });

        services.AddHttpClient<MoviesAPIClient>();

        // For the cancellation samples
        // services.AddScoped<IIntegrationService, CancellationSamples>();

        // For the compression samples
        // services.AddScoped<IIntegrationService, CompressionSamples>();

        // For the CRUD samples
        //services.AddScoped<IIntegrationService, CRUDSamples>();

        // For the compression samples
        // services.AddScoped<IIntegrationService, CompressionSamples>();

        // For the custom message handler samples
         services.AddScoped<IIntegrationService, CustomMessageHandlersSamples>();

        // For the faults and errors samples
        // services.AddScoped<IIntegrationService, FaultsAndErrorsSamples>();

        // For the HttpClientFactory samples
        // services.AddScoped<IIntegrationService, HttpClientFactorySamples>();

        // For the local streams samples
        // services.AddScoped<IIntegrationService, LocalStreamsSamples>();

        // For the partial update samples
        //services.AddScoped<IIntegrationService, PartialUpdateSamples>();

        // For the remote streaming samples
        //services.AddScoped<IIntegrationService, RemoteStreamingSamples>();

    }).Build();



// For demo purposes: overall catch-all to log any exception that might 
// happen to the console & wait for key input afterwards so we can easily 
// inspect the issue.  
try
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Host created.");

    // Run the IntegrationService containing all samples and
    // await this call to ensure the application doesn't 
    // prematurely exit.
    await host.Services.GetRequiredService<IIntegrationService>().RunAsync();
}
catch (Exception generalException)
{
    // log the exception
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(generalException,
        "An exception happened while running the integration service.");
}

Console.ReadKey();

await host.RunAsync();
 
 