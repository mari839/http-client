
//we want to send a request to the next handler in line and check response
//if it indicates faulure we want to send again but not endlessly, like if errormessage is 404 sending again will still fail

namespace Movies.Client.Handlers
{
    public class RetryPolicyDelegatingHandler : DelegatingHandler //DelegatingHandler derives from HttpMessageHandler so it's a custom HttpMessageHandler
    {
        private readonly int _maximumAmountOfRetries = 3; //limiting retries

        public RetryPolicyDelegatingHandler(int maximumAmountOfRetries) : base()
        {
            _maximumAmountOfRetries = maximumAmountOfRetries;
        }

        public RetryPolicyDelegatingHandler(HttpMessageHandler innerHandler, int maximumAmountOfRetries) : base(innerHandler)
        {
            _maximumAmountOfRetries = maximumAmountOfRetries;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            for(int i=0; i<_maximumAmountOfRetries; i++)
            {
                var response = await base.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
