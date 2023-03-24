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
    public class MyMemoryStream : MemoryStream
    {
        public override void Close()
        {
            base.Close();
        }
    }

    public class ResponseLoggingMiddleware
    {
        private readonly RequestDelegate next;

        public ResponseLoggingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            Console.WriteLine("Processing request");

            var originalBodyStream = context.Response.Body;
            
            using var responseBody = new MyMemoryStream();

            context.Response.Body = responseBody;

            await this.next(context).ConfigureAwait(false);
            
            Console.WriteLine("Processing response: " + Encoding.UTF8.GetString(responseBody.ToArray()));

            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);

            context.Response.Body = originalBodyStream;
        }
    }
}
