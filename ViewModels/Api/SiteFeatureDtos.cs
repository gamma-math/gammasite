using System;
using System.Collections.Generic;

namespace GamMaSite.ViewModels.Api
{
    public class CalendarEventDto
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string StartsAt { get; set; }

        public string Weekday { get; set; }

        public string WeekNumber { get; set; }

        public string Location { get; set; }

        public string MapsUrl { get; set; }

        public string Description { get; set; }
    }

    public class LibraryItemDto
    {
        public string Name { get; set; }

        public string Path { get; set; }

        public string Type { get; set; }

        public string Icon { get; set; }
    }

    public class LibraryListingDto
    {
        public string Root { get; set; }

        public string Parent { get; set; }

        public bool HasParent { get; set; }

        public IReadOnlyList<LibraryItemDto> Items { get; set; }
    }

    public class PaymentProductDto
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public long? UnitAmount { get; set; }

        public string Currency { get; set; }

        public string Additional { get; set; }

        public string Conditions { get; set; }

        public string ConditionsName { get; set; }
    }

    public class PaymentConfigDto
    {
        public string PublicApiKey { get; set; }
    }

    public class MessageCategoriesDto
    {
        public IReadOnlyList<string> Statuses { get; set; }

        public IReadOnlyList<string> Roles { get; set; }
    }

    public class MessageRecipientPreviewRequest
    {
        public string[] Statuses { get; set; }

        public string[] Roles { get; set; }
    }

    public class MessageRecipientPreviewDto
    {
        public int RecipientCount { get; set; }

        public int EmailCount { get; set; }

        public int SmsCount { get; set; }
    }
}
