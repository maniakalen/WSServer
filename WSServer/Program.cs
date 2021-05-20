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
        ClientHandler.HandlersStack = new List<ClientHandler>();
        ClientHandler.Receivers = new List<Receiver>();
        string dbServer = System.Configuration.ConfigurationManager.AppSettings.Get("db_server");
        string db = System.Configuration.ConfigurationManager.AppSettings.Get("db_database");
        string uid = System.Configuration.ConfigurationManager.AppSettings.Get("db_uid");
        string pwd = System.Configuration.ConfigurationManager.AppSettings.Get("db_pass");
        string tableName = System.Configuration.ConfigurationManager.AppSettings.Get("db_auth_table");
        ClientHandler.Db = new DbConnector(dbServer, db, uid, pwd, tableName);
        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            Console.WriteLine("A client connected from {0}.", clientIp);
            ClientHandler handler = new ClientHandler(client);
            
            handler.StartClient();
        }

        
    }
}