using AlexaSkillsKit.Slu;
using AlexaSkillsKit.Speechlet;
using AlexaSkillsKit.UI;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
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
        private const string SESSION_ATTRIBUTE_START_INDEX = "startIndex";
        private const string SESSION_ATTRIBUTE_DATA = "sessionData";

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
                case "details":
                    int startIndex;
                    if (!LastEventIndex(session, out startIndex))
                    {
                        return ErrorResponse();
                    }

                    return RetrieveEventToSpeech(session, startIndex, true);
                    
                case "moreevents":
                    int previousStartIndex;
                    if (!LastEventIndex(session, out previousStartIndex))
                    {
                        return ErrorResponse();
                    }

                    return RetrieveEventToSpeech(session, ++previousStartIndex);
                case "findmooch":
                default:
                    var task = ProcessFindMoochIntent(request, session);
                    task.Wait();
                    speech = task.Result;
                    break;
            }
            
            this.log.Info(String.Format("EndOnIntent requestId={0}, sessionId={1}, intent={2}", request.RequestId, session.SessionId, request.Intent));
            return speech;
        }

        private bool LastEventIndex(Session session, out int startIndex)
        {
            startIndex = -1;
            var start = session.Attributes[SESSION_ATTRIBUTE_START_INDEX];
            return (!session.IsNew && int.TryParse(start, out startIndex));
        }

        private async Task<SpeechletResponse> ProcessFindMoochIntent(IntentRequest request, Session session)
        {
            Slot citySlot = request.Intent.Slots["city"];
            string cityName = citySlot.Value;

            if (String.IsNullOrWhiteSpace(cityName))
            {
                // If no city given, default to Seattle
                cityName = "Seattle, WA";
            }

            MoochApiResult result = await GetEvents(cityName, session);
            if (result == MoochApiResult.Success)
            {
                return RetrieveEventToSpeech(session, 0);                
            }
            else if(result == MoochApiResult.NoEvents)
            {
                String outputSpeech = String.Format(@"<emphasis level=""strong"">Sorry.</emphasis><p>Find Mooch is currently not available near {0}</p><p>It is only available in the Puget Sound area.</p>",
                        cityName);

                var response = new SpeechletResponse();
                response.ShouldEndSession = true;
                response.OutputSpeech = new SsmlOutputSpeech() { Ssml = outputSpeech };
                return response;
            }
            else
            {
                return ErrorResponse();
            }
        }
                             
        private SpeechletResponse ErrorResponse()
        {
            string outputSpeech = @"<speak><emphasis level=""strong"">Sorry.</emphasis> An error occurred with Find Mooch.</speak>";

            var response = new SpeechletResponse();
            response.ShouldEndSession = true;
            response.OutputSpeech = new SsmlOutputSpeech() { Ssml = outputSpeech };
            return response;
        }

        private enum MoochApiResult
        {
            Error,
            NoEvents,
            Success
        }

        private async Task<MoochApiResult> GetEvents(string cityName, Session session)
        {
            int pageSize = 10; 
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
                    var events = await moochResponse.Content.ReadAsStringAsync();
                    session.Attributes[SESSION_ATTRIBUTE_DATA] = events;

                    if (GetEventsFromSession(session).Count == 0)
                    {
                        return MoochApiResult.NoEvents;
                    }
                    return MoochApiResult.Success;
                }
                else
                {
                    return MoochApiResult.Error;
                }
            }
        }

        private IList<MoochEvent> GetEventsFromSession(Session session)
        {
            string allEvents = session.Attributes[SESSION_ATTRIBUTE_DATA];
            var events = JsonConvert.DeserializeObject<IList<MoochEvent>>(allEvents);
            return events;
        }

        private SpeechletResponse RetrieveEventToSpeech(Session session, int eventIndex, bool sayDetails = false)
        {
            IList<MoochEvent> events = GetEventsFromSession(session);
            string outputSpeech = String.Empty;
            string outputCard = String.Empty;
            var response = new SpeechletResponse();

            if (eventIndex >= events.Count)
            {
                // Index out of range, we shouldn't have allowed the "more" command, but if user did return return response
                outputSpeech = @"<speak>No more events found</speak>";

                response.ShouldEndSession = true;
                response.OutputSpeech = new SsmlOutputSpeech() { Ssml = outputSpeech };
                return response;
            }

            var moochEvent = events[eventIndex];

            var localTime = TimeZoneInfo.ConvertTime(moochEvent.EventStart, TimeZoneInfo.Local, TimeZoneInfo.Local);
            outputSpeech += String.Format(
                @"<p><emphasis level=""strong"">Event titled</emphasis> <break strength=""strong""/> {0} <break strength=""strong""/><emphasis level=""moderate""> Event occurs on </emphasis> {1} </p> ",
                moochEvent.Title, localTime);
            if (sayDetails)
            {
                outputSpeech += String.Format(
                    @"<p><emphasis level=""strong"">Located at</emphasis> <break strength=""strong""/>{0}</p>",
                    StringifyLocation(moochEvent.Location)
                );
            }
            string location = StringifyLocation(moochEvent.Location);
            outputCard += String.Format("Event: {0}\nDate: {1}\nLocation:{2}\n", moochEvent.Title, localTime, location);

            outputSpeech += @"<p>You can say</p><p><emphasis level=""medium"">repeat</emphasis>, to repeat event with more details</p>";
            if (eventIndex < events.Count - 1)
            {
                outputSpeech += @"<p>or say<emphasis level=""medium"">next event</emphasis>, to hear the next event</p>";
            }

            outputSpeech = String.Format("<speak>{0}</speak>", outputSpeech);
            response.ShouldEndSession = false;
            response.OutputSpeech = new SsmlOutputSpeech() { Ssml = outputSpeech };
            session.Attributes[SESSION_ATTRIBUTE_START_INDEX] = eventIndex.ToString(CultureInfo.InvariantCulture);
            if (!String.IsNullOrWhiteSpace(outputCard))
            {
                var card = new SimpleCard();
                card.Title = "Events near you";
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