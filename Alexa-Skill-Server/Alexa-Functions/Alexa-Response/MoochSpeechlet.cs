using AlexaSkillsKit.Slu;
using AlexaSkillsKit.Speechlet;
using AlexaSkillsKit.UI;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
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
            response.OutputSpeech = new SsmlOutputSpeech() {  Ssml=@"<speak>Welcome to Find Mooch. Ask me <break strength=""medium""/> <emphasis level=""strong""> find free events </emphasis></speak>"};

            return response;
        }


        public override SpeechletResponse OnIntent(IntentRequest request, Session session)
        {
            this.log.Info(String.Format("BeginOnIntent requestId={0}, sessionId={1}, intent={2}", request.RequestId, session.SessionId, request.Intent.Name));

            var task = GetEvents();
            task.Wait();

            this.log.Info(String.Format("EndOnIntent requestId={0}, sessionId={1}, intent={2}", request.RequestId, session.SessionId, request.Intent));
            return task.Result;
        }


        private async Task<SpeechletResponse> GetEvents()
        {
            string outputSpeech = "Here are events near you.";
            string outputCard = String.Empty;
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
                        outputSpeech += String.Format(
                            @"<p><emphasis level=""strong"">Event titled</emphasis> <break strength=""strong""/> {0} <break strength=""strong""/><emphasis level=""moderate""> Event occurs on </emphasis> {1} </p> ",
                            moochEvent.Title, localTime);
                        string location = StringifyLocation(moochEvent.Location);
                        outputCard += String.Format("Event: {0}\nDate: {1}\nLocation:{2}\n", moochEvent.Title, localTime, location);
                    }
                }
                else
                {
                    outputSpeech = @"<emphasis level=""strong"">Sorry.</emphasis> An error occurred with Find Mooch.";
                }
            }

            outputSpeech = String.Format("<speak>{0}</speak>", outputSpeech);

            var card = new SimpleCard();
            card.Title = "Here are events near you";
            card.Content = outputCard;
            
            var response = new SpeechletResponse();
            response.ShouldEndSession = true;
            response.OutputSpeech = new SsmlOutputSpeech() { Ssml = outputSpeech };
            response.Card = card;
            return response;
        }

        private string StringifyLocation(MoochLocation location)
        {
            var sb = new StringBuilder();
            if (!String.IsNullOrWhiteSpace(location.Name))
            {
                sb.AppendLine(location.Name);
            }
            if (!String.IsNullOrWhiteSpace(location.Address_1))
            {
                sb.Append(location.Address_1 + ",");
            }
            if (!String.IsNullOrWhiteSpace(location.Address_2))
            {
                sb.Append(location.Address_2 + ",");
            }
            if (!String.IsNullOrWhiteSpace(location.Address_3))
            {
                sb.Append(location.Address_3 + ",");
            }
            if (!String.IsNullOrWhiteSpace(location.City))
            {
                sb.Append(location.City);
            }
            sb.AppendLine();
            return sb.ToString();
        }

        public override void OnSessionEnded(SessionEndedRequest request, Session session)
        {
            this.log.Info(String.Format("OnSessionEnded requestId={0}, sessionId={1}", request.RequestId, session.SessionId));
        }
    }
}