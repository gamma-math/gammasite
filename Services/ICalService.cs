using Ical.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Ical.Net.CalendarComponents;
using System;
using Humanizer;
using System.Web;

namespace GamMaSite.Services
{
    public interface IICalService
    {
        public Task<IEnumerable<CalendarEvent>> FetchCalendarEvents();

        public Task<CalendarEvent> FetchCalendarEvent(string uid);

        public Task<EventsWrapper> GetEventsWrapper();
    }

    public class ICalService : IICalService
    {
        private readonly string _icalAddress;

        public ICalService(string icalAddress)
        {
            this._icalAddress = icalAddress;
        }

        public async Task<IEnumerable<CalendarEvent>> FetchCalendarEvents()
        {
            var result = await GetResult(this._icalAddress);
            var calendar = Calendar.Load(result);
            var events = calendar.Events;
            
            return events;
        }

        public async Task<CalendarEvent> FetchCalendarEvent(string uid)
        {
            var calEvents = await FetchCalendarEvents();
            var calEvent = calEvents.First(it => it.Uid == uid);
            return calEvent;
        }

        private async Task<string> GetResult(string query)
        {
            var handler = new HttpClientHandler
            {
                SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12
            };
            using var client = new HttpClient(handler);
            var response = await client.GetAsync(query);

            var responseString = await response.Content.ReadAsStringAsync();
            return responseString;
        }

        public async Task<EventsWrapper> GetEventsWrapper()
        {
            return new EventsWrapper
            {
                Events = await FetchCalendarEvents()
            };
        }
    }

    public class EventsWrapper
    {
        public IEnumerable<CalendarEvent> Events { get; set; }

        public IEnumerable<CalendarEvent> UpcomingEvents
        {
            get { return Events.Where(it => it.Start.AsUtc.CompareTo(DateTime.UtcNow) >= 0); }
        }

        public IEnumerable<CalendarEvent> HistoricEvents
        {
            get { return Events.Where(it => it.Start.AsUtc.CompareTo(DateTime.UtcNow) < 0); }
        }
    }

    public static class ICalExtensions
    {
        public static string ToStartLocalDateTime(this CalendarEvent calendar)
        {
            return calendar.Start.AsSystemLocal.ToLocalTime().ToString("dd-MM-yyyy HH:mm");
        }

        public static string ToStartWeekday(this CalendarEvent calendar)
        {
            return calendar.Start.AsSystemLocal.ToLocalTime().ToString("dddd").Humanize(LetterCasing.Sentence);
        }

        public static string ToWeekOfYear(this CalendarEvent calendar)
        {
            var datetime = calendar.Start.AsSystemLocal.ToLocalTime();
            var dfi = System.Globalization.DateTimeFormatInfo.CurrentInfo;
            var weekNumber = dfi?.Calendar.GetWeekOfYear(datetime, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
            return $"{weekNumber}";
        }
        
        public static string ToGoogleMapsAddress(this CalendarEvent calendar)
        {
            return $"https://google.com/maps?q={HttpUtility.UrlEncode(calendar.Location)}";
        }
    }
}
