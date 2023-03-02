using active_directory_wpf_msgraph_v2.Connectors;
using active_directory_wpf_msgraph_v2.Helppers;
using Google.Apis.Calendar.v3.Data;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace active_directory_wpf_msgraph_v2
{
    class MigrateCalendars
    {
        public static Microsoft.Graph.Event convertEvents(Google.Apis.Calendar.v3.Data.Event eventGoogle, Microsoft.Graph.Calendar calendar)
        {
            Microsoft.Graph.Event msEvent = new Microsoft.Graph.Event();
            try{ msEvent.Subject = eventGoogle.Summary; } catch { };
            try{msEvent.Body.Content = eventGoogle.Description;} catch { };
            try{msEvent.Body.ContentType = BodyType.Html; } catch { };
            try{msEvent.CreatedDateTime = eventGoogle.Created;}catch { };
            try {msEvent.Start = new DateTimeTimeZone() {
                DateTime = eventGoogle.Start.DateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                TimeZone = eventGoogle.Start.TimeZone ?? "E. South America Standard Time"
            };}catch { };
            try {msEvent.End = new DateTimeTimeZone()
            {
                DateTime = eventGoogle.End.DateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                TimeZone = eventGoogle.End.TimeZone ?? "E. South America Standard Time"
            };}catch { };
            IList<Attendee> attendees = new List<Attendee>();
            try {foreach (var attendee in eventGoogle.Attendees.ToList())
            {
                Attendee tempo = new Attendee()
                {
                    EmailAddress = new EmailAddress()
                    {
                        Name = attendee.DisplayName,
                        Address = attendee.Email
                    },
                };
                if (attendee.Optional == true)
                {
                    tempo.Type = AttendeeType.Optional;
                }
                else
                {
                    tempo.Type = AttendeeType.Required;
                }
                if (attendee.Resource == true)
                {
                    tempo.Type = AttendeeType.Resource;
                }
                if (attendee.ResponseStatus.Contains("needsAction"))
                {
                    tempo.Status = new ResponseStatus()
                    {
                        Response = ResponseType.NotResponded
                    };
                }
                else if (attendee.ResponseStatus.Contains("declined"))
                {
                    tempo.Status = new ResponseStatus()
                    {
                        Response = ResponseType.Declined
                    };
                }
                else if (attendee.ResponseStatus.Contains("tentative"))
                {
                    tempo.Status = new ResponseStatus()
                    {
                        Response = ResponseType.TentativelyAccepted
                    };
                }
                else 
                {
                    tempo.Status = new ResponseStatus()
                    {
                        Response = ResponseType.Accepted
                    };
                }
                attendees.Add(tempo);
            }
            }
            catch { };
            msEvent.Calendar = calendar;
            msEvent.Attendees = attendees;
            return msEvent;
        }
        public static Microsoft.Graph.Calendar buildCalendar(String calendarName)
        {
            Random random = new Random ();
            Microsoft.Graph.Calendar calendar = new Microsoft.Graph.Calendar();
            calendar.Name = calendarName;
            calendar.Color = (CalendarColor) random.Next(10);
            return calendar;
        }
        public static async Task<Microsoft.Graph.Calendar> CloudUp(Microsoft.Graph.Calendar calendar, MsGraph currentGraph)
        {
            new Output("1120", "Criado calendario: " + calendar.Name);
            return await currentGraph.graphServiceClient.Me.Calendars
                        .Request()
                        .AddAsync(calendar);

        }
        public static async Task CloudUpAsync(List<Microsoft.Graph.Event> Events, Microsoft.Graph.Calendar calendar , MsGraph currentGraph)
        {
            foreach (Microsoft.Graph.Event temp in Events)
            {
                await currentGraph.graphServiceClient.Me.Calendars[calendar.Id].Events
                        .Request()
                        .AddAsync(temp);
            }
        }
    }
    
}
