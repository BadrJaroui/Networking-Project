﻿using System.Collections.Immutable;
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
    private static EndPoint convertedEndpoint = (EndPoint)ServerEndpoint;

    public static void start()
    {

        //TODO: [Create endpoints and socket]
        SocketCreation(socket, ClientEndpoint);
       
        try
        {
            //TODO: [Create and send HELLO]
            Message msg = new();
            msg.MsgId = 2;
            msg.MsgType = MessageType.Hello;
            msg.Content = "Hello";

            SendMessage(msg);

            //TODO: [Receive and print Welcome from server]
            ReceiveMessage();

            // TODO: [Create and send DNSLookup Message]
            Message Message1 = new Message ();
            Message1.MsgId = 3;
            Message1.MsgType = MessageType.DNSLookup;
            Message1.Content = "www.outlook.com";

            SendMessage(Message1);
        }
        catch (Exception ex)
        {
            Console.WriteLine("exception message:" + ex.Message);
        }
       
        //TODO: [Receive and print DNSLookupReply from server]
        ReceiveMessage();

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
        
        Console.WriteLine("received message: " + stringMessage);
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

    public static Dictionary<string, object> ConvertMsgToDict(Message msg)
    {
        string serializedMsg = JsonSerializer.Serialize(msg);
        Dictionary<string, object> msgDict = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedMsg);

        return msgDict;
    }
    
    public static string ConvertMsgToString(Message msg)
    {
        string msgString = JsonSerializer.Serialize(msg);
        return msgString;
    }
}