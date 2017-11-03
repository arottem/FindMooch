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

        /// <summary>
        /// Load the next batch of events from the server to the cache
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Returns the cached events, or if none cached, gets the next set from the server
        /// </summary>
        /// <returns></returns>
        public async Task<IList<MoochEvent>> Events()
        {
            if (this.cachedEvents == null)
            {
                await this.LoadEventsFromServer();
            }

            return this.cachedEvents;
        }

        /// <summary>
        /// Returns a string representating this object for later deserialization
        /// </summary>
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

        /// <summary>
        /// Returns a MoochServer object with state consistent with the one given by the serialized string
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static MoochServer Deserialize(string state)
        {
            var stateObject = JsonConvert.DeserializeObject<StateObject>(state);
            return new MoochServer(stateObject);
        }
        
    }
}
