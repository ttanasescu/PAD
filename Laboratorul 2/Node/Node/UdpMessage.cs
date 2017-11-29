namespace Node
{
    internal class UdpMessage
    {
        public RequestType RequestType { get; set; }

        public UdpMessage()
        {
        }

        public UdpMessage(RequestType requestType)
        {
            RequestType = requestType;
        }
    }
}