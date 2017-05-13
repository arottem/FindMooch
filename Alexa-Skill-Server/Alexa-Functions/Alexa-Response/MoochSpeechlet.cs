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
        string moochApiToken = ConfigurationManager.AppSettings["MoochApiToken"];
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
            SpeechletResponse speech = null;

            switch (request.Intent.Name.ToLower())
            {
                case "amazon.helpintent":
                    speech = new SpeechletResponse()
                    {
                        ShouldEndSession = false,
                        OutputSpeech = new SsmlOutputSpeech()
                        {
                            Ssml = "<speak><p>Ask me to find you events with free food or drinks</p><p>For example</p> Alexa, open Find Mooch, find an event in Seattle </speak>"
                        }

                    };
                    break;
                case "amazon.cancelintent":
                case "amazon.stopintent":
                    speech = new SpeechletResponse()
                    {
                        ShouldEndSession = true
                    };
                    break;
                case "givedetails":
                    break;
                case "moreevents":
                    var start = session.Attributes["startIndex"];
                    int startIndex;
                    if (!session.IsNew && int.TryParse(start, out startIndex))
                    {
                        speech = GetAndWaitForEvents(request, startIndex);
                    }
                    else
                    {
                        speech = new SpeechletResponse()
                        {
                            ShouldEndSession = false,
                            OutputSpeech = new SsmlOutputSpeech()
                            {
                                Ssml = "<speak><p>Sorry, I couldn't understand</p><p>Please ask me to find you events</p><p>For example</p> Alexa, open Find Mooch, find an event in Seattle </speak>"
                            }
                        };
                    }
                    break;
                case "findmooch":
                default:
                    speech = GetAndWaitForEvents(request);                    
                    break;
            }

            this.log.Info(String.Format("EndOnIntent requestId={0}, sessionId={1}, intent={2}", request.RequestId, session.SessionId, request.Intent));
            return speech;
        }

        private SpeechletResponse GetAndWaitForEvents(IntentRequest request, int startIndex = 0)
        {
            Slot citySlot = request.Intent.Slots["city"];
            string cityName = citySlot.Value;

            if (String.IsNullOrWhiteSpace(cityName))
            {
                // If no city given, default to Seattle
                cityName = "Seattle, WA";
            }

            var task = GetEvents(cityName);
            task.Wait();
            return task.Result;        
        }

        private async Task<SpeechletResponse> GetEvents(string cityName)
        {
            string outputSpeech = String.Format("Here are events near {0}.", cityName);
            string outputCard = String.Empty;
            int pageSize = 3; // Get up to 3 events per interaction (speak 2, know that more are available)
            int startItem = 0;

            // Call Mooch API for data
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://findmooch.com");
                String path;
                path = String.Format("/api/Events?token={0}&hasLocation=1&startItem={1}&pageSize={2}&address={3}", moochApiToken, startItem, pageSize, cityName);
                
                HttpResponseMessage moochResponse = await client.GetAsync(path);
                
                if (moochResponse.IsSuccessStatusCode)
                {
                    var events = await moochResponse.Content.ReadAsAsync<ICollection<MoochEvent>>();
                    if (events.Count > 0)
                    {
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
                        outputSpeech = String.Format(@"<emphasis level=""strong"">Sorry.</emphasis><p>Find Mooch is currently not available near {0}</p><p>It is only available in the Puget Sound area.</p>",
                            cityName);
                    }
                }
                else
                {
                    outputSpeech = @"<emphasis level=""strong"">Sorry.</emphasis> An error occurred with Find Mooch.";
                }
            }


            outputSpeech = String.Format("<speak>{0}</speak>", outputSpeech);
            var response = new SpeechletResponse();
            response.ShouldEndSession = true;
            response.OutputSpeech = new SsmlOutputSpeech() { Ssml = outputSpeech };

            if (!String.IsNullOrWhiteSpace(outputCard))
            {
                var card = new SimpleCard();
                card.Title = String.Format("Here are events near {0}", cityName);
                card.Content = outputCard;
                response.Card = card;
            }

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