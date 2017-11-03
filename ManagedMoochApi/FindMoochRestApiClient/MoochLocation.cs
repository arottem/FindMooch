using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ManagedMoochApi.Model
{
    [Serializable]
    public class MoochLocation
    {
        public string Address_1 { get; set; }
        public string Address_2 { get; set; }
        public string Address_3 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public float Lat { get; set; }
        public float Lon { get; set; }
        public string Name { get; set; }
    }
}