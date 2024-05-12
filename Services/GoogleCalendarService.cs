using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using System;
using System.Threading.Tasks;

namespace GamMaSite.Services
{
    public interface IGoogleCalendarService
    {
        public Task<Events> GetUpcommingEvents();

        public Task<Events> GetHistoricEvents();

        public Task<Events> GetEvents(DateTimeOffset from, DateTimeOffset to);

        public Task<Events> GetEvents();

    }

    public class GoogleCalendarService : IGoogleCalendarService
    {
        private readonly JsonCredentialParameters _googleCredentials;
        private readonly string _calendar;

        public GoogleCalendarService(JsonCredentialParameters googleCredentials, string calendar)
        {
            this._googleCredentials = googleCredentials;
            this._calendar = calendar;
        }

        public async Task<Events> GetUpcommingEvents()
        {
            return await GetEvents(DateTimeOffset.Now, DateTimeOffset.MaxValue);
        }

        public async Task<Events> GetHistoricEvents()
        {
            return await GetEvents(DateTimeOffset.MinValue, DateTimeOffset.Now);
        }

        public async Task<Events> GetEvents()
        {
            return await GetEvents(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
        }

        public async Task<Events> GetEvents(DateTimeOffset from, DateTimeOffset to)
        {
            string[] scopes = [CalendarService.Scope.CalendarReadonly];
            var service = CreateService(scopes);

            EventsResource.ListRequest request = service.Events.List(this._calendar);
            request.TimeMinDateTimeOffset = from;
            request.TimeMaxDateTimeOffset = to;
            request.ShowDeleted = true;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var events = await request.ExecuteAsync();
            return events;
        }

        private CalendarService CreateService(string[] scopes)
        {
            var credential = GoogleCredential.FromJsonParameters(this._googleCredentials).CreateScoped(scopes);

            return new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "application-calendar",
            });
        }
    }
}
