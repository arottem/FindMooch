using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

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
            int pageSize = 5; // Get up to 5 events per interaction
            string moochApiToken = ConfigurationManager.AppSettings["MoochApiToken"];

            // Call Mooch API for data
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://findmooch.com");
                var path = String.Format("/api/Events?token={0}&latitude=0&longitude=0&hasLocation=0&pageSize={1}", moochApiToken, pageSize);
                HttpResponseMessage moochResponse = await client.GetAsync(path);
                if (moochResponse.IsSuccessStatusCode)
                {
                    var events = await moochResponse.Content.ReadAsAsync<ICollection<MoochEvent>>();
                    foreach (MoochEvent moochEvent in events)
                    {
                        var localTime = TimeZoneInfo.ConvertTime(moochEvent.EventStart, TimeZoneInfo.Utc, TimeZoneInfo.Local);
                        outputSpeach += String.Format(
                            @"<p><emphasis level=""strong"">Event titled</emphasis> <break strength=""strong""/> {0} <break strength=""strong""/><emphasis level=""moderate""> Event occurs on </emphasis> {1} </p> ", 
                            moochEvent.Title, localTime);
                    }
                }
                else
                {
                    outputSpeach = @"<emphasis level=""strong"">Sorry.</emphasis> An error occurred with Find Mooch.";
                }
            }

            outputSpeach = String.Format("<speak>{0}</speak>", outputSpeach);
            var httpResponse = new
            {
                version = "1.0",
                response = new
                {
                    shouldEndSession = "true",
                    outputSpeech = new
                    {
                        type = "SSML",
                        text = outputSpeach
                    }
                }

            };


            return req.CreateResponse(HttpStatusCode.OK, httpResponse);

        }
    }
}