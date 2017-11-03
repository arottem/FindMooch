using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ManagedMoochApi.Model
{
    [Serializable]
    public class MoochEvent
    {
        /// <summary>
        /// Internal unique ID
        /// </summary>
        public int Id;
        /// <summary>
        /// ID to use when querying for a specific event
        /// </summary>
        public string UrlId;
        /// <summary>
        /// Event description (usually long)
        /// </summary>
        public string Description;
        /// <summary>
        /// Short title of the event
        /// </summary>
        public string Title;
        /// <summary>
        /// URI to find more details and rsvp to the event
        /// </summary>
        public string DetailsUri;
        /// <summary>
        /// Time the event starts (in local time zone)
        /// </summary>
        public DateTime EventStart;
        /// <summary>
        /// Full location of the event
        /// </summary>
        public MoochLocation Location;

    }
}
