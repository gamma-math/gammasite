using System;

namespace GamMaSite.Services
{
    public sealed class ContentDeletionException : Exception
    {
        public ContentDeletionException(string message)
            : base(message)
        {
        }

        public ContentDeletionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
