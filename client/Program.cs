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
    static string configFile = @"../../../../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);
    private static Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    private static IPEndPoint ServerEndpoint = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);
    private static IPEndPoint ClientEndpoint = new IPEndPoint(IPAddress.Parse(setting.ClientIPAddress), setting.ClientPortNumber);
    private static EndPoint convertedEndpoint = (EndPoint)ServerEndpoint;

    private static int acknowledgementIDCounter = 4112;

    public static void start()
    {
        SocketCreation(socket, ClientEndpoint);
        
        try
        {
            Message msg = new();
            msg.MsgId = 2;
            msg.MsgType = MessageType.Hello;
            msg.Content = "Hello";
        
            SendMessage(msg);
            ReceiveMessage();
            
            //Sends 2 valid DNS lookups
            Message Message1 = new Message();
            Message1.MsgId = 3;
            Message1.MsgType = MessageType.DNSLookup;
            Message1.Content = "www.outlook.com";

            SendMessage(Message1);
            Message ack = sendAcknowledgmentMessage(ReceiveMessage());

            if (!(ack is null)) SendMessage(ack);
            
            Message Message2 = new Message();
            Message2.MsgId = 4;
            Message2.MsgType = MessageType.DNSLookup;
            Message2.Content = "example.com";

            SendMessage(Message2);
            Message ack2 = sendAcknowledgmentMessage(ReceiveMessage());

            if (!(ack2 is null)) SendMessage(ack2);


            //Sends 2 invalid DNS lookups
            Message Message3 = new Message();
            Message3.MsgId = 7;
            Message3.MsgType = MessageType.DNSLookup;
            Message3.Content = "skibidi@gmail.com";

            SendMessage(Message3);
            Message ack3 = sendAcknowledgmentMessage(ReceiveMessage());

            if (!(ack3 is null)) SendMessage(ack3);
 
            Message Message4 = new Message();
            Message4.MsgId = 8;
            Message4.MsgType = MessageType.DNSLookup;
            Message4.Content = "sudeenbadr.com";

            SendMessage(Message4);
            Message ack4 = sendAcknowledgmentMessage(ReceiveMessage());

            if (!(ack4 is null)) SendMessage(ack4);

            //End message
            Message endMessage = new Message();
            endMessage.MsgId = 999;
            endMessage.MsgType = MessageType.End;
            endMessage.Content = "No Lookups anymore";
            SendMessage(endMessage);
            socket.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("exception message:" + ex.Message);
        }
    }
    
    private static Message sendAcknowledgmentMessage(Message msg)
    {
        if (msg.MsgType == MessageType.DNSLookupReply)
        {
            Message ack = new();
            ack.MsgId = acknowledgementIDCounter++;
            ack.MsgType = MessageType.Ack;
            ack.Content = msg.MsgId;

            return ack;
        }

        return null;
    }
    
    private static void SocketCreation(Socket socket, IPEndPoint endpoint)
    {
        socket.Bind(endpoint);
        Console.WriteLine("Connection started.\n");
    }
    
    private static void SendMessage(Message msg)
    {
        string msgString = JsonSerializer.Serialize(msg);
        byte[] messageSize = Encoding.ASCII.GetBytes(msgString);
        int bytesSent = socket.SendTo(messageSize, convertedEndpoint);
        Console.WriteLine($"Client sent: {msgString}\n");
    }
    
    
    private static Message ReceiveMessage()
    {
        Console.WriteLine("\nTrying to receive message...");

        byte[] messageSize = new byte[1000];
        int receivedMessage = socket.ReceiveFrom(messageSize, ref convertedEndpoint);
        
        //converts the received message to string 
        string jsonString = Encoding.UTF8.GetString(messageSize, 0, receivedMessage);
        Dictionary<string, object> dictMessage = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
        Message message = ConvertDictToMsg(dictMessage);

        //Converts index to messagetype for display Messagetype name
        int index = ((JsonElement)dictMessage["MsgType"]).GetInt32();
        MessageType msgType = (MessageType)index;
        message.MsgType = msgType;

        string stringMessage = ConvertMsgToString(message);
        
        Console.WriteLine("Received message: " + stringMessage + "\n");
        return message;
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
        JsonSerializerOptions options = new() {Converters = {new JsonStringEnumConverter()}};
        string msgString = JsonSerializer.Serialize(msg, options);
        return msgString;
    }
}