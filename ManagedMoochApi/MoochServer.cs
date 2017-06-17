using ManagedMoochApi.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ManagedMoochApi
{
    [Serializable]
    public class MoochServer
    {
        private MoochSettings settings;
        private int startItem = 0;        
        private IList<MoochEvent> cachedEvents;

        public MoochServer(MoochSettings settings)
        {
            this.settings = settings;
        }

        internal MoochServer(StateObject cachedState)
        {
            this.settings = cachedState.Settings;
            this.startItem = cachedState.StartItem;
            this.cachedEvents = cachedState.Events;
        }

        public async Task LoadEventsFromServer()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://findmooch.com");
                String path;
                path = String.Format("/api/Events?token={0}&hasLocation=1&startItem={1}&pageSize={2}&address={3}", this.settings.ApiKey, this.startItem, this.settings.BatchSize, this.settings.LocationFilter);

                HttpResponseMessage moochResponse = await client.GetAsync(path);
                moochResponse.EnsureSuccessStatusCode();

                var eventsAsString = await moochResponse.Content.ReadAsStringAsync();
                this.cachedEvents = JsonConvert.DeserializeObject<IList<MoochEvent>>(eventsAsString);
                
                this.startItem += this.settings.BatchSize;
            }
        }

        public async Task<IList<MoochEvent>> Events()
        {
            if (this.cachedEvents == null)
            {
                await this.LoadEventsFromServer();
            }

            return this.cachedEvents;
        }

        public string Serialize
        {
            get
            {
                var state = new StateObject()
                {
                    Events = this.cachedEvents,
                    Settings = this.settings,
                    StartItem = this.startItem
                };
                return JsonConvert.SerializeObject(state);
            }
        }

        public static MoochServer Deserialize(string state)
        {
            var stateObject = JsonConvert.DeserializeObject<StateObject>(state);
            return new MoochServer(stateObject);
        }
        
    }
}
