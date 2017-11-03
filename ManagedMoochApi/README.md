#Readme
##Managed Mooch API is a simple to use interface with the FindMooch.com service.  
Below is an example:
`
            var moochServerState = new MoochServer(new MoochSettings()
            {
                ApiKey = "YOUR_API_KEY",
                LocationFilter = "100 3rd ave, Seattle, WA", //An exact or approximate address or even just zipcode
                BatchSize = 10 // How many events to return (and cache) at a time
            });
            currentEventIndex = 0;
            
            // Getting events from the server
            IEnumerable<MoochEvent> events = (await moochServerState.Events()).Skip(currentEventIndex).Take(EventsPerInteraction);
            ...
 `         

