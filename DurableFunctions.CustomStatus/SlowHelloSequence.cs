using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DurableFunctions.FunctionChaining
{
    public class SlowHelloSequence
    {
        [FunctionName("SlowHelloSequence")]
        public async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var outputs = new List<string>();

            context.SetCustomStatus("John");
            outputs.Add(await context.CallActivityAsync<string>("SlowHelloSequence_Hello", "John"));
            context.SetCustomStatus("Peter");
            outputs.Add(await context.CallActivityAsync<string>("SlowHelloSequence_Hello", "Peter"));
            context.SetCustomStatus("Chris");
            outputs.Add(await context.CallActivityAsync<string>("SlowHelloSequence_Hello", "Chris"));

            return outputs;
        }

        [FunctionName("SlowHelloSequence_Hello")]
        public string SayHello([ActivityTrigger] DurableActivityContextBase context, ILogger log)
        {
            Thread.Sleep(5000);

            var name = context.GetInput<string>();
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("SlowHelloSequence_HttpStart")]
        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClientBase starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("SlowHelloSequence", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            bool.TryParse(req.RequestUri.ParseQueryString()["wait"], out var wait);

            // wait for response
            if (wait)
                return await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(req, instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}