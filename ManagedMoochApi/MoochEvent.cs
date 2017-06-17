using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ManagedMoochApi.Model
{
    [Serializable]
    public class MoochEvent
    {
        public string Id;
        public string Description;
        public string Title;
        public string DetailsUri;
        public DateTime EventStart;
        public MoochLocation Location;

    }
}
