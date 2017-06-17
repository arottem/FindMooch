using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagedMoochApi
{
    [Serializable]
    public class MoochSettings
    {
        public string ApiKey { get; set; }
        public string LocationFilter { get; set; }
        public int BatchSize { get; set; }

    }
}
