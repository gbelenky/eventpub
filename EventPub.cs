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
            context.ContinueAsNew(eventTarget);           
            
            return;
        }

        [FunctionName("EventScheduler")]
        public static async Task EventScheduler(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            List<EventCalendar> eventCalendars = context.GetInput<List<EventCalendar>>();

            // Run multiple eventPublication flows in parallel
            var eventPubTasks = new List<Task>();

            foreach(EventCalendar cal in eventCalendars)
            {
                Task calTask = context.CallSubOrchestratorAsync("EventPublisher", cal);
                eventPubTasks.Add(calTask);
            }
 
            await Task.WhenAll(eventPubTasks);

            /*
            // schedule next event series
            foreach(EventCalendar cal in eventCalendars)
            {
                cal.SetNextSeries();
            }
            

            context.ContinueAsNew(eventCalendars);
            */
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
            List<EventCalendar> eventCalendars = createCalendars();
            string instanceId = await starter.StartNewAsync("EventScheduler", eventCalendars);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        private static List<EventCalendar> createCalendars()
        {
            List<EventCalendar> allCalendars = new List<EventCalendar>();

            EventCalendar arizonaCalendar = new EventCalendar
            {
                Name = "Arizona",
                EventList = new List<DateTime>
                {
                    new DateTime(2022, 02, 25, 16, 41, 0),
                    new DateTime(2022, 02, 25, 16, 41, 5),
                    new DateTime(2022, 02, 25, 16, 41, 10)
                },
                NextDay = 0,
                NextHour = 0,
                NextMinute = 3
            };

            EventCalendar californiaCalendar = new EventCalendar
            {
                Name = "California",
                EventList = new List<DateTime>
                {
                    new DateTime(2022, 02, 25, 16, 41, 0),
                    new DateTime(2022, 02, 25, 16, 41, 10),
                    new DateTime(2022, 02, 25, 16, 41, 20)
                },
                NextDay = 0,
                NextHour = 0,
                NextMinute = 3
            };


            EventCalendar washingtonCalendar = new EventCalendar
            {
                Name = "Washington",
                EventList = new List<DateTime>
                {
                    new DateTime(2022, 02, 25, 16, 41, 0),
                    new DateTime(2022, 02, 25, 16, 41, 20),
                    new DateTime(2022, 02, 25, 16, 41, 40)
                },
                NextDay = 0,
                NextHour = 0,
                NextMinute = 3
            };

            allCalendars.Add(arizonaCalendar);
            allCalendars.Add(californiaCalendar);
            allCalendars.Add(washingtonCalendar);

            return allCalendars;
        }
    }
}