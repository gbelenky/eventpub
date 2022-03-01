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
using System.Text.Json.Serialization; 

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

            foreach (EventCalendar cal in eventCalendars)
            {
                Task calTask = context.CallSubOrchestratorAsync("EventPublisher", cal);
                eventPubTasks.Add(calTask);
            }

            await Task.WhenAll(eventPubTasks);
            return;
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
                    new DateTime(2022, 02, 26, 15, 01, 0),
                    new DateTime(2022, 02, 26, 15, 01, 5),
                    new DateTime(2022, 02, 26, 15, 01, 10)
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
                    new DateTime(2022, 02, 26, 15, 01, 0),
                    new DateTime(2022, 02, 26, 15, 01, 10),
                    new DateTime(2022, 02, 26, 15, 01, 20)
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
                    new DateTime(2022, 02, 26, 15, 01, 0),
                    new DateTime(2022, 02, 26, 15, 01, 20),
                    new DateTime(2022, 02, 26, 15, 01, 40)
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

        [FunctionName("CancelScheduledEvents")]
        public static async Task CancelScheduledEvents(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
    [DurableClient] IDurableOrchestrationClient client,
    ILogger log)
        {
            var noFilter = new OrchestrationStatusQueryCondition();
            OrchestrationStatusQueryResult result = await client.ListInstancesAsync(
                noFilter,
                CancellationToken.None);
            foreach (DurableOrchestrationStatus instance in result.DurableOrchestrationState)
            {
                //log.LogInformation(JsonSerializer.Serialize(instance));
                if(instance.Name.Equals("EventPublisher") && (instance.RuntimeStatus==OrchestrationRuntimeStatus.ContinuedAsNew))
                {
                    await client.TerminateAsync(instance.InstanceId, "EventPublisher graceful shutdown");
                }
            }

             foreach (DurableOrchestrationStatus instance in result.DurableOrchestrationState)
            {
                //log.LogInformation(JsonSerializer.Serialize(instance));
                if(instance.Name.Equals("EventScheduler") && (instance.RuntimeStatus==OrchestrationRuntimeStatus.Completed))
                {
                    await client.TerminateAsync(instance.InstanceId, "EventScheduler graceful shutdown");
                }
            }

            // Note: ListInstancesAsync only returns the first page of results.
            // To request additional pages provide the result.ContinuationToken
            // to the OrchestrationStatusQueryCondition's ContinuationToken property.
        }
    }
}