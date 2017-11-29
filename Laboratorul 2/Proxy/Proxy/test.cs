//// This sender example must be used in conjunction with the listener program.
//// You must run this program as follows:
//// Open a console window and run the listener from the command line. 
//// In another console window run the sender. In both cases you must specify 
//// the local IPAddress to use. To obtain this address,  run the ipconfig command 
//// from the command line. 
////  
//internal static class TestMulticastOption
//{
//    static IPAddress mcastAddress;
//    static int mcastPort;
//    static Socket mcastSocket;

//    private static void JoinMulticastGroup()
//    {
//        // Create a multicast socket.
//        mcastSocket = new Socket(AddressFamily.InterNetwork,
//            SocketType.Dgram,
//            ProtocolType.Udp);

//        // Get the local IP address used by the listener and the sender to
//        // exchange multicast messages. 
//        Console.Write("\nEnter local IPAddress for sending multicast packets: ");
//        var localIPAddr = IPAddress.Any;
//        Console.WriteLine(localIPAddr);

//        // Create an IPEndPoint object. 
//        var IPlocal = new IPEndPoint(localIPAddr, 0);

//        // Bind this endpoint to the multicast socket.
//        mcastSocket.Bind(IPlocal);

//        // Define a MulticastOption object specifying the multicast group 
//        // address and the local IP address.
//        // The multicast group address is the same as the address used by the listener.
//        var mcastOption = new MulticastOption(mcastAddress, localIPAddr);

//        mcastSocket.SetSocketOption(SocketOptionLevel.IP,
//            SocketOptionName.AddMembership,
//            mcastOption);

//    }

//    static void BroadcastMessage(string message)
//    {
//        //Send multicast packets to the listener.
//        var endPoint = new IPEndPoint(mcastAddress, mcastPort);
//        mcastSocket.SendTo(Encoding.ASCII.GetBytes(message), endPoint);
//        Console.WriteLine("Multicast data sent.....");

//        //mcastSocket.Close();
//    }

//    static void Main(string[] args)
//    {
//        // Initialize the multicast address group and multicast port.
//        // Both address and port are selected from the allowed sets as
//        // defined in the related RFC documents. These are the same 
//        // as the values used by the sender.
//        mcastAddress = IPAddress.Parse("224.168.100.2");
//        mcastPort = 11000;

//        // Join the listener multicast group.
//        JoinMulticastGroup();

//        // Broadcast the message to the listener.
//        BroadcastMessage("Hello multicast listener.");
//        Console.ReadKey();
//    }
//}