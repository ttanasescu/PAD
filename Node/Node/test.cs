// This is the listener example that shows how to use the MulticastOption class. 
// In particular, it shows how to use the MulticastOption(IPAddress, IPAddress) 
// constructor, which you need to use if you have a host with more than one 
// network card.
// The first parameter specifies the multicast group address, and the second 
// specifies the local address of the network card you want to use for the data
// exchange.
// You must run this program in conjunction with the sender program as 
// follows:
// Open a console window and run the listener from the command line. 
// In another console window run the sender. In both cases you must specify 
// the local IPAddress to use. To obtain this address run the ipconfig comand 
// from the command line. 
//  

//public class TestMulticastOption
//{
//    private static IPAddress mcastAddress;
//    private static int mcastPort;
//    private static UdpClient mcastSocket;
//    //private static Socket mcastSocket;
//    private static MulticastOption mcastOption;

//    private static void StartMulticast()
//    {
//        mcastSocket = new UdpClient();
//        //mcastSocket.Client = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);

//        mcastSocket.ExclusiveAddressUse = false;
//        mcastSocket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

//        mcastSocket.JoinMulticastGroup(mcastAddress);

//        mcastSocket.Client.Bind(new IPEndPoint(IPAddress.Loopback, mcastPort));

//        // Define a MulticastOption object specifying the multicast group 
//        // address and the local IPAddress.
//        // The multicast group address is the same as the address used by the server.
//        //mcastOption = new MulticastOption(mcastAddress, localIPAddr);
//        //mcastSocket.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOption);
//    }

//    private static void ReceiveBroadcastMessages()
//    {
//        var done = false;
//        var bytes = new Byte[100];
//        var groupEP = new IPEndPoint(mcastAddress, mcastPort);
//        EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

//        while (!done)
//        {
//            Console.WriteLine("Waiting for multicast packets.......");
//            Console.WriteLine("Enter ^C to terminate.");

//            mcastSocket.Client.ReceiveFrom(bytes, ref remoteEP);

//            Console.WriteLine($"Received broadcast from {groupEP} :\n {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}\n");
//        }

//        mcastSocket.Close();
//    }

//    public static void Main(String[] args)
//    {
//        // Initialize the multicast address group and multicast port.
//        // Both address and port are selected from the allowed sets as
//        // defined in the related RFC documents. These are the same 
//        // as the values used by the sender.
//        mcastAddress = IPAddress.Parse("224.168.100.2");
//        mcastPort = 11000;

//        // Start a multicast group.
//        StartMulticast();

//        // Display MulticastOption properties.
//        //Console.WriteLine("Current multicast group is: " + mcastOption.Group);
//        //Console.WriteLine("Current multicast local address is: " + mcastOption.LocalAddress);

//        // Receive broadcast messages.
//        ReceiveBroadcastMessages();
//    }
//}