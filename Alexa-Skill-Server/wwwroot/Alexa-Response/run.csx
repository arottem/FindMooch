#r "Newtonsoft.Json"
using System.Net;
using Newtonsoft.Json;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");

    // Get request body
    string data = await req.Content.ReadAsStringAsync();
    log.Info(data);
    var httpResponse = new {
        version = "1.0",
         response = new {
             shouldEndSession = "true", 
             outputSpeech = new {
                     type = "PlainText",
                     text = "How are we today?"
                 }
             }

         };
    

return req.CreateResponse(HttpStatusCode.OK, httpResponse);
    
}