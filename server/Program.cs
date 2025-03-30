using System;
using System.Data;
using System.Data.SqlTypes;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using LibData;

// ReceiveFrom();
class Program
{
    static void Main(string[] args)
    {
        ServerUDP.start();
    }
}

public class Setting
{
    public int ServerPortNumber { get; set; }
    public string? ServerIPAddress { get; set; }
    public int ClientPortNumber { get; set; }
    public string? ClientIPAddress { get; set; }
}


class ServerUDP
{
    static string configFile = @"../../../../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);
    
    private static Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

  private static IPEndPoint ServerEndpoint = new IPEndPoint(IPAddress.Loopback, 49153);
    private static IPEndPoint ClientEndpoint = new IPEndPoint(IPAddress.Any, 49152);

    // TODO: [Read the JSON file and return the list of DNSRecords]
    public static List<DNSRecord> ParsedDNS()
    {
        List<DNSRecord> records = new();
        using (StreamReader rdr = new("../../../DNSrecords.json"))
        {
            string json = rdr.ReadToEnd();
            records = JsonSerializer.Deserialize<List<DNSRecord>>(json);
        }

        return records;
    }

    public static void start()
    {
        // TODO: [Create a socket and endpoints and bind it to the server IP address and port number]
       
        ServerBinding(socket, ServerEndpoint); 

        // Converts IPEndpoint to Endpoint so that we can use it to receive messages
        EndPoint convertedEndpoint = (EndPoint)ClientEndpoint;
        // TODO:[Receive and print a received Message from the client]
      
        try   
        { 
            // TODO:[Receive and print Hello]
            byte[] messagesize = new byte[5];
            Console.WriteLine("trying to receive message");
            int receivedbytes = socket.ReceiveFrom(messagesize,ref convertedEndpoint);
            string convertedmessage =  Encoding.ASCII.GetString(messagesize,0,receivedbytes);
            Console.WriteLine(convertedmessage);

            // TODO:[Send Welcome to the client]
            byte[] welcomeMessageSize = Encoding.ASCII.GetBytes("WELCOME");
            Console.WriteLine("Sending data.");
            int bytessent = socket.SendTo(welcomeMessageSize,convertedEndpoint);    
            Console.WriteLine($"Sent welcome message to: " + convertedEndpoint);

            // TODO:[Receive and print DNSLookup]
            byte[] DNSMessageSize = new byte[1000];
            int receivedDNS = socket.ReceiveFrom(DNSMessageSize, ref convertedEndpoint);
            string convertedDNSmessage = Encoding.ASCII.GetString(DNSMessageSize,0,receivedDNS);
            Console.WriteLine(convertedDNSmessage);

            //door de json file heen zoeken dmv de deserializedDNS
            //Message deserializedDNS = JsonSerializer.Deserialize<Message>(convertedDNSmessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine("exception: " + ex.Message);
        }
        
        // TODO:[Query the DNSRecord in Json file]
        
        // TODO:[If found Send DNSLookupReply containing the DNSRecord]

        // TODO:[If not found Send Error]


        // TODO:[Receive Ack about correct DNSLookupReply from the client]


        // TODO:[If no further requests receieved send End to the client]

    }

    public static void ServerBinding(Socket socket, IPEndPoint endpoint)
    {
        socket.Bind(endpoint);
        Console.WriteLine("connection binded");
    }

    public static Message DNSMatchCheck(string convertedDNSmessage)
    {
        Dictionary<string, object> DNSdict = JsonSerializer.Deserialize<Dictionary<string, object>>(convertedDNSmessage);
        List<DNSRecord> records = ParsedDNS();
        
        foreach (DNSRecord record in records)
        {
            if (record.Name == DNSdict["content"])
            {
                Message matchMsg = new();
                matchMsg.MsgId = 2;
                matchMsg.MsgType = MessageType.DNSLookupReply;
                matchMsg.Content = record;

                return matchMsg;
            }
        }

        Message errorMsg = new();
        errorMsg.MsgId = 3;
        errorMsg.MsgType = MessageType.Error;
        errorMsg.Content = "Domain not found";

        return errorMsg;
    }

    public static bool sendDNSLookupReply(Message msg, EndPoint convertedEndpoint)
    {
        byte[] welcomeMessageSize = Encoding.ASCII.GetBytes("WELCOME");
        int bytessent = socket.SendTo(welcomeMessageSize, convertedEndpoint);
    }
}