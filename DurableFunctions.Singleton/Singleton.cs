using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DurableFunctions.FunctionChaining
{
    public class Singleton
    {
        [FunctionName("Singleton")]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContextBase context, ILogger log)
        {
            var endTime = context.CurrentUtcDateTime.AddHours(1);
            while (context.CurrentUtcDateTime < endTime)
            {
                log.LogInformation("Tick");

                var nextCheck = context.CurrentUtcDateTime.AddMinutes(5);

                await context.CreateTimer(nextCheck, CancellationToken.None);
            }

            log.LogInformation("Boom");
        }


        [FunctionName("Singleton_HttpStart")]
        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orchestrators/Singleton/{instanceId}")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClientBase starter,
            string instanceId,
            ILogger log)
        {
            var existingInstance = await starter.GetStatusAsync(instanceId);
            if (existingInstance == null)
            {
                await starter.StartNewAsync("Singleton", instanceId, null);

                log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

                return starter.CreateCheckStatusResponse(req, instanceId);
            }
            else
            {
                return req.CreateErrorResponse(HttpStatusCode.Conflict, $"An instance with ID '{instanceId}' already exists.");
            }
        }
    }
}