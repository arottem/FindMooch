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
            string outputSpeach = "Here are events near you.";

            // Call Mooch API for data
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://findmooch.com");
                var path = "/api/Events?token=9ML&latitude=0&longitude=0&hasLocation=0";
                HttpResponseMessage moochResponse = await client.GetAsync(path);
                if (moochResponse.IsSuccessStatusCode)
                {
                    //                    var rep = await moochResponse.Content.ReadAsStringAsync();
                    var events = await moochResponse.Content.ReadAsAsync<IEnumerable<MoochEvent>>();
                    foreach (MoochEvent moochEvent in events)
                    {
                        outputSpeach += String.Format("Event titled. {0}. ", moochEvent.Title);
                    }
                }
                else
                {
                    outputSpeach = "Sorry. An error occurred with Find Mooch.";
                }
            }
            var httpResponse = new
            {
                version = "1.0",
                response = new
                {
                    shouldEndSession = "true",
                    outputSpeech = new
                    {
                        type = "PlainText",
                        text = outputSpeach
                    }
                }

            };


            return req.CreateResponse(HttpStatusCode.OK, httpResponse);

        }
    }
}