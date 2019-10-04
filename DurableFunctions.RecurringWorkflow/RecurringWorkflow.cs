using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DurableFunctions.Recurring
{
    /// <summary>
    /// Orchestrator function periodically processes something and returns it's status.
    /// The workflow's next occurance is defined by the other function and can also be terminated by it.
    /// </summary>
    public class RecurringWorkflow
    {
        [FunctionName("RecurringWorkflow")]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var taskId = context.GetInput<int>();

            var endTime = context.CurrentUtcDateTime.AddHours(1);
            while (context.CurrentUtcDateTime < endTime)
            {
                var res = await context.CallActivityAsync<ProcessResponse>("RecurringWorkflow_Process", taskId);

                var nextCheck = context.CurrentUtcDateTime.AddSeconds(res.NextCheckInterval);

                if (res.Status == "Completed")
                {
                    // Do something else on completion then finish
                    await context.CallActivityAsync("SendAlert", res.Message);
                    break;
                }


                // sleep until next check
                await context.CreateTimer(nextCheck, CancellationToken.None);
            }
        }

        [FunctionName("RecurringWorkflow_Process")]
        public ProcessResponse Process([ActivityTrigger] DurableActivityContextBase context, ILogger log)
        {
            log.LogInformation("Process Started");

            var rand = new Random();
            var prob = rand.Next(100);
            if (prob <= 20) // randomly mark the processing as completed
            {
                return new ProcessResponse("Completed", message: "Everything has been completed");
            }

            var nextCheck = rand.Next(5, 20); // randomly set the next check interval
            return new ProcessResponse("In progress", nextCheckInterval: nextCheck);
        }

        [FunctionName("RecurringWorkflow_SendAlert")]
        public void SendAlert([ActivityTrigger] DurableActivityContextBase context, ILogger log)
        {
            var message = context.GetInput<string>();
            log.LogInformation($"SendAlert {message}");
        }


        [FunctionName("RecurringWorkflow_HttpStart")]
        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClientBase starter,
            ILogger log)
        {
            bool.TryParse(req.RequestUri.ParseQueryString()["wait"], out var wait);
            int.TryParse(req.RequestUri.ParseQueryString()["task"], out var task);

            string instanceId = await starter.StartNewAsync("RecurringWorkflow", task);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");


            // wait for response
            if (wait)
                return await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(req, instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}