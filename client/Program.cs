using System.Collections.Immutable;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LibData;

// SendTo();
// Locally ran port and IP for testing
class Program
{
    static void Main(string[] args)
    {
        ClientUDP.start();
    }
}

public class Setting
{
    public int ServerPortNumber { get; set; }
    public string? ServerIPAddress { get; set; }
    public int ClientPortNumber { get; set; }
    public string? ClientIPAddress { get; set; }
}

class ClientUDP
{

    //TODO: [Deserialize Setting.json]
    static string configFile = @"../../../../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);


    private static Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

    private static IPEndPoint ServerEndpoint = new IPEndPoint(IPAddress.Loopback, 49153);
    private static IPEndPoint ClientEndpoint = new IPEndPoint(IPAddress.Any, 49152);

    public static void start()
    {

        //TODO: [Create endpoints and socket]
        SocketCreation(socket, ClientEndpoint);
        EndPoint convertedEndpoint = (EndPoint)ServerEndpoint;
       
        try
        {
            //TODO: [Create and send HELLO]

            byte[] hellomaxsize = Encoding.ASCII.GetBytes("HELLO");
            int hellobytes = socket.SendTo(hellomaxsize,ServerEndpoint);
            Console.WriteLine($"Sent {hellobytes} bytes to {ServerEndpoint }");

            //

            //TODO: [Receive and print Welcome from server]

            byte[] welcomemaxsize = Encoding.ASCII.GetBytes("WELCOME");
            int recbytes = socket.ReceiveFrom(welcomemaxsize,ref convertedEndpoint);
            string convertedmessage =  Encoding.ASCII.GetString(welcomemaxsize,0,recbytes);
            Console.WriteLine("received message: " + convertedmessage);

            //

            // TODO: [Create and send DNSLookup Message]

            Message Message1 = new Message ();
            Message1.MsgId = 1;
            Message1.MsgType = MessageType.DNSLookup;
            Message1.Content = "www.outlook.com";
            
            string  serializeDNS = JsonSerializer.Serialize(Message1);
            byte[] DNSMessageSize = Encoding.ASCII.GetBytes(serializeDNS);
            int DNSBytesSent = socket.SendTo(DNSMessageSize, ServerEndpoint);
            Console.WriteLine($"Sent {DNSBytesSent} to {ServerEndpoint }");

            //
        }
        catch (Exception ex)
        {
            Console.WriteLine("exception message:" + ex.Message);
        }
       
        //TODO: [Receive and print DNSLookupReply from server]

        //TODO: [Send Acknowledgment to Server]

        // TODO: [Send next DNSLookup to server]

        // repeat the process until all DNSLoopkups (correct and incorrect onces) are sent to server and the replies with DNSLookupReply

        //TODO: [Receive and print End from server]


    }
    
    public static void SocketCreation(Socket socket, IPEndPoint endpoint)
    {
        socket.Bind(endpoint);
        Console.WriteLine("Connection started.");
    }
    
    
}