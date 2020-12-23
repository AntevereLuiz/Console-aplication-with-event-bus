using System;

namespace SquidLogTransferDataServer.Objects
{
    [Serializable]
    public class Times
    {
        public int ParseTimeServer { get; set; }
        public int ReadTimeServer { get; set; }
        public int DbTime { get; set; }
        public int ParseTimeClient { get; set; }
        public int TrasferTime { get; set; }
        public int ReadTimeClient { get; set; }
    }
}
