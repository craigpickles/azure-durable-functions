using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DurableFunctions.ExternalInteractions
{
    public class ExternalInteractions
    {
        [FunctionName("ExternalInteractions")]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            await context.CallActivityAsync("ExternalInteractions_RequestApproval", null);

            using (var timeoutCts = new CancellationTokenSource())
            {
                var dueTime = context.CurrentUtcDateTime.AddHours(72);
                var durableTimeout = context.CreateTimer(dueTime, timeoutCts.Token);

                Task approvalEvent = context.WaitForExternalEvent("Approve");
                Task rejectEvent = context.WaitForExternalEvent("Reject");

                var completedTask = await Task.WhenAny(approvalEvent, rejectEvent, durableTimeout);

                if (completedTask == approvalEvent)
                {
                    timeoutCts.Cancel();
                    await context.CallActivityAsync("ExternalInteractions_ProcessApproval", null);
                }
                else if (completedTask == rejectEvent)
                {
                    timeoutCts.Cancel();
                    await context.CallActivityAsync("ExternalInteractions_ProcessRejection", null);
                }
                else
                {
                    await context.CallActivityAsync("ExternalInteractions_Escalate", null);
                }
            }
        }

        [FunctionName("ExternalInteractions_RequestApproval")]
        public void RequestApproval([ActivityTrigger] DurableActivityContextBase context, ILogger log)
        {
            log.LogInformation("Request Approval");
        }

        [FunctionName("ExternalInteractions_ProcessApproval")]
        public void ProcessApproval([ActivityTrigger] DurableActivityContextBase context, ILogger log)
        {
            log.LogInformation("Process Approval");
        }

        [FunctionName("ExternalInteractions_ProcessRejection")]
        public void ProcessRejection([ActivityTrigger] DurableActivityContextBase context, ILogger log)
        {
            log.LogInformation("Process Rejection");
        }

        [FunctionName("ExternalInteractions_Escalate")]
        public void Escalate([ActivityTrigger] DurableActivityContextBase context, ILogger log)
        {
            log.LogInformation("Escalate");
        }

        [FunctionName("ExternalInteractions_HttpStart")]
        public async Task<IActionResult> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClientBase starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("ExternalInteractions", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return new OkObjectResult($"http://localhost:7071/api/ExternalInteractions_RaiseEvent?instanceId={instanceId}&event={{event}}");
        }

        [FunctionName("ExternalInteractions_RaiseEvent")]
        public async Task<IActionResult> RaiseEvent(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClientBase client,
            ILogger log)
        {
            var query = req.RequestUri.ParseQueryString();
            var instanceId = query["instanceId"];
            var @event = query["event"];

            var status = await client.GetStatusAsync(instanceId);
            if (status.RuntimeStatus == OrchestrationRuntimeStatus.Running)
            {
                await client.RaiseEventAsync(instanceId, @event);
                return new OkObjectResult("Event sent");
            }

            return new OkObjectResult("To late...");
        }
    }
}