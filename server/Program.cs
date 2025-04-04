using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using LibData;
using Microsoft.VisualBasic;

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
    private static IPEndPoint ServerEndpoint = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);
    private static IPEndPoint ClientEndpoint = new IPEndPoint(IPAddress.Parse(setting.ClientIPAddress), setting.ClientPortNumber);

    // Converts IPEndpoint to Endpoint so that we can use it to receive messages
    private static EndPoint convertedEndpoint = (EndPoint)ClientEndpoint;
    
    private static List<DNSRecord> ParsedDNS()
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
        while (true)
        {
        try   
        {
            ReceiveMessage();
            
            Message welcomeMsg = new();
            welcomeMsg.MsgId = 1;
            welcomeMsg.MsgType = MessageType.Welcome;
            welcomeMsg.Content = "Welcome";
            SendMessage(welcomeMsg);
            

            SendDNSMesageSystem();
            SendDNSMesageSystem();
            SendDNSMesageSystem();
            SendDNSMesageSystem();
            SendDNSMesageSystem();
            SendDNSMesageSystem();
            SendDNSMesageSystem();
            
            ReceiveMessage();
            
        }
        catch (SocketException)
        {
            Console.WriteLine("Client disconnected");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception message:" + ex.Message);
        }
        }
    }
    
    //sends and receives dns messages, checks if received message is an acknowledgement or End message
    private static void SendDNSMesageSystem()
    {
        Message DNSLookup = ReceiveMessage();
        MessageType checkAck = AcknowledgementOrEndMessage(DNSLookup);
        Message DNSlookupReply = CreateDNSLookupReply(DNSLookup);

        if (checkAck == MessageType.DNSLookup)
        {
            SendMessage(DNSlookupReply);
        }
    }
    private static MessageType AcknowledgementOrEndMessage(Message lookupOrAck)
    {
        if (lookupOrAck.MsgType == MessageType.Ack)
        {
            //For conmfirmation
            //Console.WriteLine("Acknowledgement received: " + ConvertMsgToString(lookupOrAck));
            return MessageType.Ack;
        }
        if (lookupOrAck.MsgType == MessageType.End)
        {
            return MessageType.End;
        }

        return MessageType.DNSLookup;
    }

    private static void ServerBinding(Socket socket, IPEndPoint endpoint)
    {
        socket.Bind(endpoint);
        Console.WriteLine("connection binded");
    }
    
    private static void SendMessage(Message msg)
    {
        string msgString = JsonSerializer.Serialize(msg);
        byte[] messageSize = Encoding.ASCII.GetBytes(msgString);
        int bytesSent = socket.SendTo(messageSize, convertedEndpoint);
        Console.WriteLine($"Server sent: {msgString}");
    } 

    private static Message ReceiveMessage()
    {
        Console.WriteLine("\nTrying to receive message...");
        byte[] messageSize = new byte[1000];
        int receivedMessage = socket.ReceiveFrom(messageSize, ref convertedEndpoint);
        
        string jsonString = Encoding.UTF8.GetString(messageSize, 0, receivedMessage);
        Dictionary<string, object> dictMessage = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
        
        Message message = ConvertDictToMsg(dictMessage);
        string stringMessage = ConvertMsgToString(message);
        
        Console.WriteLine("received message: " + stringMessage);
        return message;
    }
    
    private static bool recordNotFound = false; 
    private static Message CreateDNSLookupReply(Message DNSMessage)
    {
        DNSRecord record = FindCorrectDNSRecord(DNSMessage);
        Message DNSLookupReply = new();

        if (record is null)
        {
            DNSLookupReply.MsgId = 7534445;
            DNSLookupReply.MsgType = MessageType.Error;
            DNSLookupReply.Content = "Domain not found";
            recordNotFound = true;
            return DNSLookupReply;
        }
        recordNotFound = false;
        DNSLookupReply.MsgId = DNSMessage.MsgId;
        DNSLookupReply.MsgType = MessageType.DNSLookupReply;
        DNSLookupReply.Content = record;

        return DNSLookupReply;
    }

    private static DNSRecord? FindCorrectDNSRecord(Message DNSmessage)
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
       
            var matchingrecord = matchingRecords[0];
            return matchingRecords[0];
        }

        return null;
    }


    private static List<DNSRecord> DNSMatchCheck(Message DNSmessage)
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

    private static Message ConvertDictToMsg(Dictionary<string, object> dict)
    {
        Message msg = new();
        msg.MsgId = ((JsonElement)dict["MsgId"]).GetInt32();
        msg.MsgType = (MessageType)((JsonElement)dict["MsgType"]).GetInt32();
        msg.Content = (JsonElement)dict["Content"];

        return msg;
    }

    private static string ConvertMsgToString(Message msg)
    {
        return $" {{\"MsgId\":\"{msg.MsgId}\",\"MsgType\":\"{msg.MsgType}\",\"Content\":\"{msg.Content}\"}}";
    }
}