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

            eventTarget.SetNextSeries();
            //context.ContinueAsNew(eventTarget);           
            return;
        }

        [FunctionName("EventScheduler")]
        public static async Task EventScheduler(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            // Run multiple eventPu flows in parallel
            var eventPubTasks = new List<Task>();

            // for debugging purposes
            DateTime startTime = context.CurrentUtcDateTime;

            EventCalendar arizonaCalendar = new EventCalendar
            {
                Name = "Arizona",
                EventList = new List<DateTime>
                {
                    /*
                    new DateTime(2022, 02, 25, 10, 25, 0),
                    new DateTime(2022, 02, 25, 10, 25, 30),
                    new DateTime(2022, 02, 25, 10, 26, 0)
                    */
                    startTime.AddMinutes(1).AddSeconds(0),
                    startTime.AddMinutes(1).AddSeconds(30),
                    startTime.AddMinutes(1).AddSeconds(60)
                },
                NextDay = 0,
                NextHour = 0,
                NextMinute = 1
            };

            EventCalendar californiaCalendar = new EventCalendar
            {
                Name = "California",
                EventList = new List<DateTime>
                {
                    /*
                    new DateTime(2022, 02, 25, 10, 25, 0),
                    new DateTime(2022, 02, 25, 10, 25, 45),
                    new DateTime(2022, 02, 25, 10, 26, 5)
                    */
                    startTime.AddMinutes(1).AddSeconds(10),
                    startTime.AddMinutes(1).AddSeconds(20),
                    startTime.AddMinutes(1).AddSeconds(45)

                },
                NextDay = 0,
                NextHour = 0,
                NextMinute = 2
            };


            EventCalendar washingtonCalendar = new EventCalendar
            {
                Name = "Washington",
                EventList = new List<DateTime>
                {
                    /*
                    new DateTime(2022, 02, 25, 10, 25, 10),
                    new DateTime(2022, 02, 25, 10, 25, 20),
                    new DateTime(2022, 02, 25, 10, 25, 30)
                    */
                    startTime.AddMinutes(2).AddSeconds(0),
                    startTime.AddMinutes(2).AddSeconds(30),
                    startTime.AddMinutes(2).AddSeconds(45)
                },
                NextDay = 0,
                NextHour = 0,
                NextMinute = 2
            };


            Task arizonaTask = context.CallSubOrchestratorAsync("EventPublisher", arizonaCalendar);
            eventPubTasks.Add(arizonaTask);

            Task washingtonTask = context.CallSubOrchestratorAsync("EventPublisher", washingtonCalendar);
            eventPubTasks.Add(washingtonTask);

            Task californiaTask = context.CallSubOrchestratorAsync("EventPublisher", californiaCalendar);
            eventPubTasks.Add(californiaTask);

            await Task.WhenAll(eventPubTasks);


            //eventTarget.SetNextSeries();
            context.ContinueAsNew(null);           
        }

        [FunctionName("EventWorker")]
        public static string PublishEvent([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"___***Publishing Events for {name}.***___");
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