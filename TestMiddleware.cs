using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreWcfTest
{
    public class MyMemoryStream : MemoryStream, IDisposable
    {
        public override void Close()
        {
            base.Close();
        }

        void IDisposable.Dispose()
        {
        }
    }

    public class RequestResponseLoggingMiddleware
    {
        public const string StopwatchHttpContextItemName = "LoggingStopwatch";

        private readonly RequestDelegate next;

        public RequestResponseLoggingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Only log JSON messages, SOAP logging needs to consider WS-Security
            Console.WriteLine("Processing request");

            var originalBodyStream = context.Response.Body;
            
            using var responseBody = new MyMemoryStream();

            context.Response.Body = responseBody;

            await this.next(context).ConfigureAwait(false);
            
            Console.WriteLine("Processing response: " + Encoding.UTF8.GetString(responseBody.ToArray()));

            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}
