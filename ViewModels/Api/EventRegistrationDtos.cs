using System;

namespace GamMaSite.ViewModels.Api
{
    public class EventRegistrationDto
    {
        public int Id { get; set; }

        public int ContentItemId { get; set; }

        public string UserId { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string RegistrationType { get; set; }

        public bool Registered { get; set; }

        public string ResponseText { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }
    }

    public class SaveEventRegistrationRequest
    {
        public string RegistrationType { get; set; }

        public string ResponseText { get; set; }
    }
}
