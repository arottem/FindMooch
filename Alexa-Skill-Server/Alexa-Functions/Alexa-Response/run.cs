using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AlexaFunctions
{
    public class AlexaResponseTrigger
    {
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
        {
            log.Info("FindMooch - Alex-Response function called (v2.0)");

            // Get request body
            string data = await req.Content.ReadAsStringAsync();
            log.Info(data);
            var httpResponse = new
            {
                version = "1.0",
                response = new
                {
                    shouldEndSession = "true",
                    outputSpeech = new
                    {
                        type = "PlainText",
                        text = "How are we today?"
                    }
                }

            };


            return req.CreateResponse(HttpStatusCode.OK, httpResponse);

        }
    }
}