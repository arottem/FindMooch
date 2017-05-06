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
        public static HttpResponseMessage Run(HttpRequestMessage req, TraceWriter log)
        {
            log.Info("FindMooch - Alex-Response function called (v2.0)");

            var speechlet = new MoochSpeechlet(log);
            return speechlet.GetResponse(req);
        }
    }
}