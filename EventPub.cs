using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Threading;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace gbelenky.EventPub
{
    public static class EventPub
    {

        [FunctionName("EventScheduler")]
        public static async Task EventScheduler(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            EventCalendar eventCalendar = context.GetInput<EventCalendar>();

            // DateTime startTime = context.CurrentUtcDateTime;
            DateTime startTime = eventCalendar.StartTime.UtcDateTime;

            await context.CreateTimer(startTime, CancellationToken.None);
            await context.CallActivityAsync<string>("EventWorker", eventCalendar.Name);

            // or make a direct call context.CallHttpAsync without the Activity "EventWorker" 
            eventCalendar.SetNextStartTime();
            // eternal execution
            context.ContinueAsNew(eventCalendar);
            return;
        }

        [FunctionName("EventWorker")]
        public static string PublishEvent([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"___***Publishing Events for {name}.***___");
            return $"Events for {name} were published";
        }

        [FunctionName("StartScheduler")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string eventCalendarPayload = await req.Content.ReadAsStringAsync();

            EventCalendar eventCalendar = JsonSerializer.Deserialize<EventCalendar>(eventCalendarPayload);

            string instanceId = await starter.StartNewAsync("EventScheduler", eventCalendar);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

    }
}