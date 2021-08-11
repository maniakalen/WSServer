using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace WatsonWebsocketServer
{
    class Program
    {
        /*static string _Hostname = "ws2.veslive.com";
        static int _Port = 443;*/

        static void Main(string[] args)
        {
            bool serverRunning = true;
            string _Hostname = System.Configuration.ConfigurationManager.AppSettings.Get("hostname");
            int _Port = Int32.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("port"));

            string dbServer = System.Configuration.ConfigurationManager.AppSettings.Get("db_server");
            int port = Int32.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("db_port"));
           
            string db = System.Configuration.ConfigurationManager.AppSettings.Get("db_database");
            string uid = System.Configuration.ConfigurationManager.AppSettings.Get("db_uid");
            string pwd = System.Configuration.ConfigurationManager.AppSettings.Get("db_pass");
            string tableName = System.Configuration.ConfigurationManager.AppSettings.Get("db_auth_table");
            ClientHandler.Db = new DbConnector(dbServer, port, db, uid, pwd, tableName);

            using (WatsonWsServer wss = new WatsonWsServer(_Hostname, _Port, true))
            {
                ClientHandler.Server = wss;
                ClientHandler.HandlersDict = new Dictionary<string, ClientHandler>();
                ClientHandler.Receivers = new List<Receiver>();
                wss.ClientConnected += (s, e) => {
                    ClientHandler.AddNewHandler(e.IpPort);
                };
                wss.ClientDisconnected += (s, e) => {
                    ClientHandler handler = null;
                    if (ClientHandler.HandlersDict.TryGetValue(e.IpPort, out handler))
                    {
                        try
                        {
                            if (handler.User != null)
                            {
                                Message msg = new SystemMessage() { Body = "<[Off]>", Sender = handler.User.Username };
                                ClientHandler.Broadcast(msg, Communication.Types.System);
                            }
                        } catch (Exception ee) { 
                        
                        }
                    }
                };
                wss.MessageReceived += (s, e) =>
                {
                    ClientHandler handler = null;
                    if (ClientHandler.HandlersDict.TryGetValue(e.IpPort, out handler))
                    {
                        handler.HandleMessage(Encoding.UTF8.GetString(e.Data));
                    }
                };

                wss.Start();
                while (serverRunning)
                {
                    Thread.Sleep(150000);
                } 
            }
        }
    }
}
