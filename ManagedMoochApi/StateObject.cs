using ManagedMoochApi.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagedMoochApi
{    
    class StateObject
    {
        public IList<MoochEvent> Events { get; set; }
        public MoochSettings Settings { get; set; }
        public int StartItem { get; set; }
    }
}
