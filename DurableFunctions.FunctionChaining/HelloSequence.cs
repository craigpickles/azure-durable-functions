using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DurableFunctions.FunctionChaining
{
    public class HelloSequence
    {
        [FunctionName("HelloSequence")]
        public async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var outputs = new List<string>();

            outputs.Add(await context.CallActivityAsync<string>("HelloSequence_Hello", "John"));
            outputs.Add(await context.CallActivityAsync<string>("HelloSequence_Hello", "Peter"));
            outputs.Add(await context.CallActivityAsync<string>("HelloSequence_Hello", "Chris"));

            return outputs;
        }

        [FunctionName("HelloSequence_Hello")]
        public string SayHello([ActivityTrigger] DurableActivityContextBase context, ILogger log)
        {
            var name = context.GetInput<string>();
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("HelloSequence_HttpStart")]
        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClientBase starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("HelloSequence", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            bool.TryParse(req.RequestUri.ParseQueryString()["wait"], out var wait);

            // wait for response
            if (wait)
                return await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(req, instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}