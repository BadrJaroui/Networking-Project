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
    static string configFile = @"../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);

    // TODO: [Read the JSON file and return the list of DNSRecords]
    public static List<DNSRecord> ParseDNS()
    {
        List<DNSRecord> records = new();
        using (StreamReader rdr = new("DNSrecords.json"))
        {
            string json = rdr.ReadToEnd();
            records = JsonSerializer.Deserialize<List<DNSRecord>>(json);
        }

        return records;
    }

    public static void start()
    {
        // TODO: [Create a socket and endpoints and bind it to the server IP address and port number]
        ServerBinding(IPAddress.Any, 8080);
        
        // TODO:[Receive and print a received Message from the client]
        
        
        // TODO:[Receive and print Hello]



        // TODO:[Send Welcome to the client]


        // TODO:[Receive and print DNSLookup]


        // TODO:[Query the DNSRecord in Json file]

        // TODO:[If found Send DNSLookupReply containing the DNSRecord]



        // TODO:[If not found Send Error]


        // TODO:[Receive Ack about correct DNSLookupReply from the client]


        // TODO:[If no further requests receieved send End to the client]

    }

    public static void ServerBinding(IPAddress ip, int port)
    {
        Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Udp);

        //IPEndPoint endpoint = new(IPAddress.Any, 8080);
        IPEndPoint endpoint = new(ip, port);

        socket.Bind(endpoint);
        socket.Listen(3);
    }
}