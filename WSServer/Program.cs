using System;
using System.Net;
using System.Net.Sockets;
using WSServer;
using System.Collections.Generic;

class Server
{

    public static void Main()
    {
        int port = Int32.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("port"));
        var server = new TcpListener(IPAddress.Any, port);

        server.Start();
        Console.WriteLine("Server has started on Any IP:{0}, Waiting for a connection...", port);
        ClientHandler.handlersStack = new List<ClientHandler>();
        ClientHandler.receivers = new List<Receiver>();
        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            Console.WriteLine("A client connected from {0}.", clientIp);
            ClientHandler handler = new ClientHandler(client);
            handler.startClient();
        }

        
    }
}