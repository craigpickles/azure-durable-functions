using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DurableFunctions.FanOutThenIn
{
    public class FanOutThenInSequence
    {
        [FunctionName("FanOutThenInSequence")]
        public async Task<string> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            int[] workload = await context.CallActivityAsync<int[]>("FanOutThenInSequence_In", 10);

            var parallelTasks = new List<Task<int>>();

            for (int i = 0; i < workload.Length; i++)
            {
                var task = context.CallActivityAsync<int>("FanOutThenInSequence_DoStuff", workload[i]);
                parallelTasks.Add(task);
            }

            await Task.WhenAll(parallelTasks);

            int sum = await context.CallActivityAsync<int>("FanOutThenInSequence_Out", parallelTasks.Select(t => t.Result));

            return $"The Sum is {sum}";
        }

        [FunctionName("FanOutThenInSequence_In")]
        public int[] In([ActivityTrigger] DurableActivityContextBase context, ILogger log)
        {
            log.LogInformation("InFunction Started");

            var workloadLength = context.GetInput<int>();
            return Enumerable.Range(0, workloadLength).ToArray();
        }

        [FunctionName("FanOutThenInSequence_DoStuff")]
        public int DoStuff([ActivityTrigger] DurableActivityContextBase context, ILogger log)
        {
            var value = context.GetInput<int>();

            log.LogInformation($"DoStuffFunction Started: {value}");

            Thread.Sleep(new Random().Next(200, 1000));

            log.LogInformation($"DoStuffFunction Ended: {value}");

            return value * 1000;
        }

        [FunctionName("FanOutThenInSequence_Out")]
        public int Out([ActivityTrigger] DurableActivityContextBase context, ILogger log)
        {
            log.LogInformation("OutFunction Started");

            var results = context.GetInput<IEnumerable<int>>();
            return results.Sum();
        }


        [FunctionName("FanOutThenInSequence_HttpStart")]
        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClientBase starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("FanOutThenInSequence", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            bool.TryParse(req.RequestUri.ParseQueryString()["wait"], out var wait);

            // wait for response
            if (wait)
                return await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(req, instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}