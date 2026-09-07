using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GamMaSite.Services
{
    /*
     * Defines SMS sending for admin message flows.
     */
    public interface ISmsSender
    {
        public Task<string> SendSmsAsync(string message, params string[] recipients);
    }
}
