using System;
using System.Collections.Generic;

namespace SquidLogTransferDataClient.Objects
{
    [Serializable]
    public class AccessLogObject
    {
        public string Time { get; set; }
        public string Duration { get; set; }
        public string ClientAddress { get; set; }
        public string ResultCode { get; set; }
        public string Bytes { get; set; }
        public string RequestMethod { get; set; }
        public string Url { get; set; }
        public string User { get; set; }
        public string HierarchyCode { get; set; }
        public string Type { get; set; }
    }

    [Serializable]
    public class Data
    {
        public int ParseTime { get; set; }
        public DateTime StartTrasferTime { get; set; }
        public int ReadTime { get; set; }
        public List<AccessLogObject> Datas { get; set; }
    }
}
