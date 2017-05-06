using AlexaSkillsKit.Slu;
using AlexaSkillsKit.Speechlet;
using AlexaSkillsKit.UI;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace AlexaFunctions
{
    public class MoochSpeechlet : Speechlet
    {
        private TraceWriter log;

        public MoochSpeechlet(TraceWriter log)
        {
            this.log = log;
        }

#if DEBUG
        public override bool OnRequestValidation(
    AlexaSkillsKit.Authentication.SpeechletRequestValidationResult result, DateTime referenceTimeUtc, AlexaSkillsKit.Json.SpeechletRequestEnvelope requestEnvelope)
        {
            return true;
        }
#endif

        public override void OnSessionStarted(SessionStartedRequest request, Session session)
        {
            this.log.Info(String.Format("OnSessionStarted requestId={0}, sessionId={1}", request.RequestId, session.SessionId));
        }

        public override SpeechletResponse OnLaunch(LaunchRequest request, Session session)
        {
            this.log.Info(String.Format("OnLaunch requestId={0}, sessionId={1}", request.RequestId, session.SessionId));

            var response = new SpeechletResponse();
            response.ShouldEndSession = false;
            response.OutputSpeech = new SsmlOutputSpeech() {  Ssml=@"Welcome to Find Mooch. Ask me <break strength=""medium""/> <emphasis level=""strong""> find free events </emphasis>"};

            return response;
        }




        public override SpeechletResponse OnIntent(IntentRequest request, Session session)
        {
            this.log.Info(String.Format("BeginOnIntent requestId={0}, sessionId={1}, intent={2}", request.RequestId, session.SessionId, request.Intent));

            var response = new SpeechletResponse();
            response.ShouldEndSession = true;

            var task = GetEvents();
            task.Wait();
            response.OutputSpeech = new SsmlOutputSpeech() { Ssml = task.Result };

            this.log.Info(String.Format("EndOnIntent requestId={0}, sessionId={1}, intent={2}, result={3}", request.RequestId, session.SessionId, request.Intent, task.Result));
            return response;
        }


        private async Task<string> GetEvents()
        {
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
            return outputSpeach;
        }


        public override void OnSessionEnded(SessionEndedRequest request, Session session)
        {
            this.log.Info(String.Format("OnSessionEnded requestId={0}, sessionId={1}", request.RequestId, session.SessionId));
        }
    }
}