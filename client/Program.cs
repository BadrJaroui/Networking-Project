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

//TODO: [Create endpoints and socket] DONE
//TODO: [Create and send HELLO] DONE
//TODO: [Receive and print Welcome from server] DONE
//TODO: [Create and send DNSLookup Message] DONE
//TODO: [Receive and print DNSLookupReply from server] DONE
//TODO: [Send Acknowledgment to Server]
// TODO: [Send next DNSLookup to server] DONE
//TODO: [Receive and print End from server]

//TODO: change IP adds and ports

class ClientUDP
{

    //TODO: [Deserialize Setting.json]
    static string configFile = @"../../../../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);


    private static Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

    private static IPEndPoint ServerEndpoint = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);
    private static IPEndPoint ClientEndpoint = new IPEndPoint(IPAddress.Parse(setting.ClientIPAddress), setting.ClientPortNumber);
    private static EndPoint convertedEndpoint = (EndPoint)ServerEndpoint;

    public static int acknowledgementIDCounter = 4112;

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
            
            Message Message1 = new Message();
            Message1.MsgId = 3;
            Message1.MsgType = MessageType.DNSLookup;
            Message1.Content = "www.outlook.com";

            SendMessage(Message1);
            sendAcknowledgmentMessage(ReceiveMessage());

      
                      
            Message Message2 = new Message();
            Message2.MsgId = 6;
            Message2.MsgType = MessageType.DNSLookup;
            Message2.Content = "example.com";
            SendMessage(Message2);
            sendAcknowledgmentMessage(ReceiveMessage());
             
            Message Message3 = new Message();
            Message3.MsgId = 7;
            Message3.MsgType = MessageType.DNSLookup;
            Message3.Content = "skibidi@gmail.com";

            SendMessage(Message3);
            sendAcknowledgmentMessage(ReceiveMessage());
          

            Message Message4 = new Message();
            Message4.MsgId = 8;
            Message4.MsgType = MessageType.DNSLookup;
            Message4.Content = "sudeenbadr.com";
            
            SendMessage(Message4);
            sendAcknowledgmentMessage(ReceiveMessage());
            
           
         

        }
        catch (Exception ex)
        {
            Console.WriteLine("exception message:" + ex.Message);
        }
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
        Console.WriteLine($"Client sent: {msgString}\n");
    }
    
    public static Message ReceiveMessage()
    {
        Console.WriteLine("\nTrying to receive message...");
        byte[] messageSize = new byte[1000];
        int receivedMessage = socket.ReceiveFrom(messageSize, ref convertedEndpoint);
        
        string jsonString = Encoding.UTF8.GetString(messageSize, 0, receivedMessage);
        Dictionary<string, object> dictMessage = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
        Message message = ConvertDictToMsg(dictMessage);

        int index = ((JsonElement)dictMessage["MsgType"]).GetInt32();
        MessageType msgType = (MessageType)index;
        message.MsgType = msgType;
        //Console.WriteLine("test: " + msgType);
        string stringMessage = ConvertMsgToString(message);
        
        Console.WriteLine("received message: " + stringMessage + "\n");
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
        JsonSerializerOptions options = new() {Converters = {new JsonStringEnumConverter()}};
        string msgString = JsonSerializer.Serialize(msg, options);
        return msgString;
    }
    
    // Might need this method for working with IDs
    public static Dictionary<string, object> ConvertMsgToDict(Message msg)
    {
        string serializedMsg = JsonSerializer.Serialize(msg);
        Dictionary<string, object> msgDict = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedMsg);

        return msgDict;
    }



    public static void sendAcknowledgmentMessage(Message msg)
    {
        if (msg.MsgType == MessageType.DNSLookupReply)
        {
            Message acknowledgement1 = new Message();
            acknowledgement1.MsgId = 4112;
            acknowledgement1.MsgType = MessageType.Ack;
            acknowledgement1.Content = msg.MsgId;
            SendMessage(acknowledgement1);
            Console.WriteLine($"Sending acknowledgement of msgID : {acknowledgement1.MsgId}");
        }
        else
        {
            Console.WriteLine("could not send an acknowledgement");
        }
            
        
    }
}