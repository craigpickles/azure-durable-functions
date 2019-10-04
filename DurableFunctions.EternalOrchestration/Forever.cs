using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DurableFunctions.Forever
{
    public class Forever
    {
        [FunctionName("Forever")]
        public async Task RunOrchestrator([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            await context.CallActivityAsync("Forever_DoSomething", null);

            var nextTick = context.CurrentUtcDateTime.AddSeconds(30);
            await context.CreateTimer(nextTick, CancellationToken.None);

            context.ContinueAsNew(null); // trunactes functions history so will restart and go into CreateTimer
        }

        [FunctionName("Forever_DoSomething")]
        public static void DoSomething([ActivityTrigger] DurableActivityContextBase context, ILogger log)
        {
            log.LogInformation("Doing something");
        }

        [FunctionName("Forever_HttpStart")]
        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClientBase starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("Forever", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}