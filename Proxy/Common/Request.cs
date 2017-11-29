using System;

namespace Common
{
    public class Request : ICloneable
    {
        public string EntityType { get; set; }
        public OrderBy OrderBy { get; set; }
        public FilterBy FilterBy { get; set; }
        //public GroupBy GroupBy { get; set; }
        public bool ResultAsJson { get; set; }

        public int TimeToLive { get; set; }


        public Request(Request request)
        {
            EntityType = request.EntityType;
            FilterBy = request.FilterBy;
            OrderBy = request.OrderBy;
            ResultAsJson = request.ResultAsJson;
            TimeToLive = request.TimeToLive;
        }

        public Request()
        {
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}