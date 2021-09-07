using PersonListener.Infrastructure;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PersonListener.Boundary
{
    public class EventData
    {
        public Dictionary<string, object> OldData { get; set; }

        public Dictionary<string, object> NewData { get; set; }
    }
}
