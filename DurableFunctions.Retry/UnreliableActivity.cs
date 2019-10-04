using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DurableFunctions.FunctionChaining
{
    public static class UnreliableActivity
    {
        [FunctionName("UnreliableActivity")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContextBase context, ILogger log)
        {
            var retryOptions = new RetryOptions(firstRetryInterval: TimeSpan.FromSeconds(5), maxNumberOfAttempts: 3);

            retryOptions.Handle = (ex) =>
            {
                // The orchestrator re-executes everything on wake, excluding any activity functions already executed, so only log if this is a new call to handle
                if (!context.IsReplaying)
                {
                    log.LogWarning("Handling Error", ex);
                }

                return true;
            };

            try
            {
                await context.CallActivityWithRetryAsync<string>("UnreliableActivity_Hello", retryOptions, "John");
            }
            catch (FunctionFailedException ex)
            {
                log.LogError("UnreliableActivity failed", ex);
            }
        }

        [FunctionName("UnreliableActivity_Hello")]
        public static string SayHello([ActivityTrigger] DurableActivityContextBase context, ILogger log)
        {
            throw new Exception("I don't work");
        }

        [FunctionName("UnreliableActivity_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClientBase starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("UnreliableActivity", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            bool.TryParse(req.RequestUri.ParseQueryString()["wait"], out var wait);

            // wait for response
            if (wait)
                return await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(req, instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}