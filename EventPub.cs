using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Threading;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace gbelenky.EventPub
{
    public static class EventPub
    {
        [FunctionName("EventPublisher")]
        public static async Task EventPublisher(
    [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            EventCalendar eventTarget = context.GetInput<EventCalendar>();

            var eventList = new List<Task>();

            foreach (DateTime ev in eventTarget.EventList)
            {
                await context.CreateTimer(ev, CancellationToken.None);
                Task eventTask = context.CallActivityAsync<string>("EventWorker", eventTarget.Name);
                eventList.Add(eventTask);
            }

            await Task.WhenAll(eventList);
            return;
        }

        [FunctionName("EventScheduler")]
        public static async Task EventScheduler(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            // Run multiple eventPu flows in parallel
            var eventPubTasks = new List<Task>();

            EventCalendar arizonaCalendar = new EventCalendar
            {
                Name = "Arizona",
                EventList = new List<DateTime> 
                {
                    new DateTime(2022, 02, 24, 15, 00, 0),
                    new DateTime(2022, 02, 24, 15, 00, 30),
                    new DateTime(2022, 02, 24, 15, 01, 0)
                }
            };

            EventCalendar californiaCalendar = new EventCalendar
            {
                Name = "California",
                EventList = new List<DateTime> 
                {
                    new DateTime(2022, 02, 24, 15, 00, 0),
                    new DateTime(2022, 02, 24, 15, 00, 45),
                    new DateTime(2022, 02, 24, 15, 01, 5)
                }
            };


            EventCalendar washingtonCalendar = new EventCalendar
            {
                Name = "Washington",
                EventList = new List<DateTime> 
                {
                    new DateTime(2022, 02, 24, 15, 00, 10),
                    new DateTime(2022, 02, 24, 15, 00, 20),
                    new DateTime(2022, 02, 24, 15, 00, 30)
                }
            };


            Task arizonaTask = context.CallSubOrchestratorAsync("EventPublisher", arizonaCalendar);
            eventPubTasks.Add(arizonaTask);

            Task washingtonTask = context.CallSubOrchestratorAsync("EventPublisher", washingtonCalendar);
            eventPubTasks.Add(washingtonTask);

            Task californiaTask = context.CallSubOrchestratorAsync("EventPublisher", californiaCalendar);
            eventPubTasks.Add(californiaTask);

            await Task.WhenAll(eventPubTasks);
        }

        [FunctionName("EventWorker")]
        public static string PublishEvent([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Publishing Events for {name}.");
            return $"Events for {name} were published";
        }

        [FunctionName("EventPub_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("EventScheduler", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}