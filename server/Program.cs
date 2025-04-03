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

// TODO: [Read the JSON file and return the list of DNSRecords] DONE
// TODO: [Create a socket and endpoints and bind it to the server IP address and port number] (TODO: CHANGE IP ADD AND PORT TO SETTING.JSON)
// TODO:[Receive and print a received Message from the client] DONE
// TODO:[Receive and print Hello] DONE
// TODO:[Send Welcome to the client] DONE
// TODO:[Receive and print DNSLookup] DONE
// TODO:[Query the DNSRecord in Json file] DONE
// TODO:[If found Send DNSLookupReply containing the DNSRecord] DONE
// TODO:[If not found Send Error] DONE
// TODO:[Receive Ack about correct DNSLookupReply from the client]
// TODO:[If no further requests receieved send End to the client]

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
        ServerBinding(socket, ServerEndpoint); 
        
        try   
        {
            ReceiveMessage();
            
            Message welcomeMsg = new();
            welcomeMsg.MsgId = 1;
            welcomeMsg.MsgType = MessageType.Welcome;
            welcomeMsg.Content = "Welcome";
            SendMessage(welcomeMsg);
            
            // Test searching an A-type record
            Message DNSLookup = ReceiveMessage();
            Message DNSlookupReply = CreateDNSLookupReply(DNSLookup);
            SendMessage(DNSlookupReply);

            // Searches MX and A-type records and returns record with most priority
            // (MX-type with higher priority > MX-type with lower priority > A-type)
            Message DNSLookup2 = ReceiveMessage();
            Message DNSlookupReply2 = CreateDNSLookupReply(DNSLookup2);
            SendMessage(DNSlookupReply2);
            
            // Test searching a non-existent record
            Message DNSLookup3 = ReceiveMessage();
            Message DNSlookupReply3 = CreateDNSLookupReply(DNSLookup3);
            SendMessage(DNSlookupReply3);
        }
        
        catch (Exception ex)
        {
            Console.WriteLine("exception: " + ex.Message);
        }
    }

    public static void ServerBinding(Socket socket, IPEndPoint endpoint)
    {
        socket.Bind(endpoint);
        Console.WriteLine("connection binded");
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
    
    public static Message CreateDNSLookupReply(Message DNSMessage)
    {
        DNSRecord? record = FindCorrectDNSRecord(DNSMessage);
        Message DNSLookupReply = new();

        if (record is null)
        {
            DNSLookupReply.MsgId = 4;
            DNSLookupReply.MsgType = MessageType.Error;
            DNSLookupReply.Content = "Domain not found";

            return DNSLookupReply;
        }
        
        DNSLookupReply.MsgId = 5;
        DNSLookupReply.MsgType = MessageType.DNSLookupReply;
        DNSLookupReply.Content = record;

        return DNSLookupReply;
    }

    public static DNSRecord? FindCorrectDNSRecord(Message DNSmessage)
    {
        bool MXflag = false;
        List<DNSRecord>? matchingRecords = DNSMatchCheck(DNSmessage);

        if (matchingRecords is null || matchingRecords.Count == 0) return null;
        
        foreach (DNSRecord match in matchingRecords)
        {
            // If there's an MX-type match, removes all A-type matches
            if (match.Type == "MX")
            {
                MXflag = true;
                foreach (DNSRecord record in matchingRecords)
                {
                    if (record.Type == "A") matchingRecords.Remove(record);
                }
            }
        }

        // Finds the MX-type record with the highest priority
        if (MXflag)
        {
            DNSRecord highestPriorityMatch = matchingRecords[0];
            
            foreach (DNSRecord match in matchingRecords)
            {
                if (match.Priority < highestPriorityMatch.Priority) highestPriorityMatch = match;
            }

            return highestPriorityMatch;
        }

        // If there's only A-type record matches, returns the first record in list
        if (!(MXflag))
        {
            return matchingRecords[0];
        }

        return null;
    }
    
    public static List<DNSRecord> DNSMatchCheck(Message DNSmessage)
    {
        List<DNSRecord> matchingRecords = new() { };
        List<DNSRecord> records = ParsedDNS();
        
        foreach (DNSRecord record in records)
        {
            if (DNSmessage.Content.ToString().Contains(record.Name))
            {
                matchingRecords.Add(record);
            }
        }

        return matchingRecords;
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
        return $" {{\"MsgId\":\"{msg.MsgId}\",\"MsgType\":\"{msg.MsgType}\",\"Content\":\"{msg.Content}\"}}";
    }
    
    // Might need this method for working with IDs
    public static Dictionary<string, object> ConvertMsgToDict(Message msg)
    {
        string serializedMsg = JsonSerializer.Serialize(msg);
        Dictionary<string, object> msgDict = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedMsg);

        return msgDict;
    }
}