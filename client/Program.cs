using System.Collections.Immutable;
using System.ComponentModel;
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
        //TODO: [Create and send HELLO]
        try
        {

            //verstuurd hello
            byte[] hellomaxsize = Encoding.ASCII.GetBytes("HELLO");
            int bytessent = socket.SendTo(hellomaxsize,ServerEndpoint);
            Console.WriteLine($"Sent {bytessent} bytes to {ServerEndpoint }");

            //ontvangt welcome
            byte[] welcomemaxsize = Encoding.ASCII.GetBytes("WELCOME");
            int recbytes = socket.ReceiveFrom(welcomemaxsize,ref convertedEndpoint);
          
            string convertedmessage =  Encoding.ASCII.GetString(welcomemaxsize,0,recbytes);
            Console.WriteLine("received message: " + convertedmessage);
       
            
        }
        catch (Exception ex)
        {
             Console.WriteLine("exception message:" + ex.Message);
        }
          


        //TODO: [Receive and print Welcome from server]
       
        try 
        {
            byte[] messagemaxsize = new byte[10];
         
            int recbytes = socket.ReceiveFrom(messagemaxsize,ref convertedEndpoint);
            string encoded = Encoding.ASCII.GetString(messagemaxsize,0,recbytes);
            Console.WriteLine("encoded: " +encoded);

        }
        catch (Exception ex)
        {
            Console.WriteLine("exception message:" + ex.Message);
        }

        socket.Close();
        // TODO: [Create and send DNSLookup Message]

       
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