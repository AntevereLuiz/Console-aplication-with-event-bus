using System;

namespace SquidLogTransferDataServer.Objects
{
    [Serializable]
    public class EventBusMessage
    {
        public string Message { get; set; }
        public Times TimesServer { get; set; }
    }
}
