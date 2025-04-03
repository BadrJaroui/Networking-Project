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
    // Converts IPEndpoint to Endpoint so that we can use it to receive messages
    private static EndPoint convertedEndpoint = (EndPoint)ClientEndpoint;

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
        try   
        { 
            // TODO:[Receive and print a received Message from the client]
            // TODO:[Receive and print Hello]
            ReceiveMessage();

            // TODO:[Send Welcome to the client]
            Message welcomeMsg = new();
            welcomeMsg.MsgId = 1;
            welcomeMsg.MsgType = MessageType.Welcome;
            welcomeMsg.Content = "Welcome";
            SendMessage(welcomeMsg);

            // TODO:[Receive and print DNSLookup]
            Message DNSLookup = ReceiveMessage();
            
            // TODO:[Query the DNSRecord in Json file]
            // TODO:[If found Send DNSLookupReply containing the DNSRecord]
            // TODO:[If not found Send Error]
            Message DNSLookupCheck = DNSMatchCheck(DNSLookup);
            SendMessage(DNSLookupCheck);

            //door de json file heen zoeken dmv de deserializedDNS
            //Message deserializedDNS = JsonSerializer.Deserialize<Message>(convertedDNSmessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine("exception: " + ex.Message);
        }

        // TODO:[Receive Ack about correct DNSLookupReply from the client]


        // TODO:[If no further requests receieved send End to the client]

    }

    public static void ServerBinding(Socket socket, IPEndPoint endpoint)
    {
        socket.Bind(endpoint);
        Console.WriteLine("connection binded");
    }

    public static Message DNSMatchCheck(Message DNSmessage)
    {
        List<DNSRecord> records = ParsedDNS();
        
        foreach (DNSRecord record in records)
        {
            if (DNSmessage.Content.ToString().Contains(record.Name))
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

    public static void SendMessage(Message msg)
    {
        string msgString = JsonSerializer.Serialize(msg);
        byte[] messageSize = Encoding.ASCII.GetBytes(msgString);
        int bytesSent = socket.SendTo(messageSize, convertedEndpoint);
        Console.WriteLine($"Sent {bytesSent} bytes.");
    }

    public static Message ReceiveMessage()
    {
        Console.WriteLine("Trying to receive message...");
        byte[] messageSize = new byte[1000];
        int receivedMessage = socket.ReceiveFrom(messageSize, ref convertedEndpoint);
        
        string jsonString = Encoding.UTF8.GetString(messageSize, 0, receivedMessage);
        Dictionary<string, object> dictMessage = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
        
        Message message = ConvertDictToMsg(dictMessage);
        string stringMessage = ConvertMsgToString(message);
        
        Console.WriteLine(stringMessage);
        return message;
    }

    public static Message ConvertDictToMsg(Dictionary<string, object> dict)
    {
        Message msg = new();
        msg.MsgId = ((JsonElement)dict["MsgId"]).GetInt32();
        msg.MsgType = (MessageType)((JsonElement)dict["MsgType"]).GetInt32();
        msg.Content = (JsonElement)dict["Content"];

        return msg;
    }
    
    public static string ConvertMsgToString(Message msg)
    {
        string msgString = JsonSerializer.Serialize(msg);
        return msgString;
    }
}