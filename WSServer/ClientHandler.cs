using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Timers;
using System.Threading.Tasks;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using WatsonWebsocket;


namespace WatsonWebsocketServer
{
    class ClientHandler
    {
        public static Dictionary<string, ClientHandler> HandlersDict;
        public string ClientIpPort;
        public static List<Receiver> Receivers;
        public static DbConnector Db;
        public static WatsonWsServer Server;
        public User User;
        public bool Pinged = false;

        private System.Timers.Timer Timer;

        public ClientHandler(string clientIpPort)
        {
            this.ClientIpPort = clientIpPort;
            
        }

        public static void AddNewHandler(string clientIpPort)
        {
            Console.WriteLine("Adding new client from {0}.", clientIpPort); 
            if (!ClientHandler.HandlersDict.ContainsKey(clientIpPort))
            {
                HandlersDict.Add(clientIpPort, new ClientHandler(clientIpPort));
            }
            
        }

        public bool HandleMessage(string msg)
        {
            var comDefinition = new Communication();
            try
            {
                var comm = JsonConvert.DeserializeAnonymousType(msg, comDefinition);

                var type = (Communication.Types)comm.Type;

                switch (type)
                {
                    case Communication.Types.User:
                        return this.UserLogin(comm);
                    case Communication.Types.Invitation:
                        this.HandleInvitation(comm);
                        break;
                    case Communication.Types.Message:
                        this.HandleMessage(comm);
                        break;
                    case Communication.Types.System:
                        var systemMessage = this.Extract(comm.Body, new SystemMessage());
                        systemMessage.HandleMessage(this);
                        break;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return true;
            }
            return true;
        }

        public static void Broadcast(Message msg)
        {
            foreach (KeyValuePair<string, ClientHandler> entry in ClientHandler.HandlersDict)
            {
                if (msg.Sender == null || (entry.Value.User != null && entry.Value.User.Username != msg.Sender))
                {
                    msg.Receiver = entry.Value.User.Username;
                    Communication.Send(entry.Key, Communication.Types.Message, msg);
                }
            }
        }

        public static void Broadcast(Message msg, Communication.Types Type)
        {
            foreach (KeyValuePair<string, ClientHandler> entry in ClientHandler.HandlersDict)
            {
                if (msg.Sender == null || (entry.Value.User != null && entry.Value.User.Username != msg.Sender))
                {
                    msg.Receiver = entry.Value.User.Username;
                    Communication.Send(entry.Key, Type, msg);
                }
            }
        }

        public bool IsOnline()
        {
            return ClientHandler.Server.IsClientConnected(this.ClientIpPort);
        }

        public void SendStatuses()
        {
            List<string> users = new List<string>();
            foreach (KeyValuePair<string, ClientHandler> entry in ClientHandler.HandlersDict)
            {
                if (entry.Value.IsOnline() && entry.Value.User != null && entry.Value.User.Username != this.User.Username)
                {
                    users.Add(entry.Value.User.Username);
                }
            }

            SystemMessage msg = new SystemMessage() { Sender = "System.Statuses", Receiver = this.User.Username, Body = JsonConvert.SerializeObject(users.ToArray()), Status = true };
            Communication.Send(this.ClientIpPort, Communication.Types.System, msg);
        }

        private T Extract<T>(string json, T definition)
        {
            try
            {
                return JsonConvert.DeserializeAnonymousType(json, definition);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw e;
            }
        }

        public void Close()
        {
            ClientHandler.Server.DisconnectClient(this.ClientIpPort);
            ClientHandler.HandlersDict.Remove(this.ClientIpPort);
        }

        private void PingPong(object source, ElapsedEventArgs e)
        {

            if (this.Pinged)
            {
                Console.WriteLine("Closing socket");
                Message msg = new SystemMessage() { Body = "<[Off]>", Sender = this.User.Username };
                ClientHandler.Broadcast(msg, Communication.Types.System);
                this.Close();
            }
            else
            {
                
                this.Pinged = true;
                ClientHandler.Server.SendAsync(this.ClientIpPort, "Ping").Wait();
                Console.Write(this.User.Username + ": Ping - ");
            }
        }

        private bool UserLogin(Communication comm)
        {
            var user = this.Extract(comm.Body, new User());
            SystemMessage msg;
            if (!user.Authenticate())
            {
                msg = new SystemMessage() { Sender = "System", Receiver = user.Username, Body = "Failed to authenticate", Status = false };
                msg.SetType(Communication.Types.User);
                msg.SendMessage(this.ClientIpPort);
                return false;
            }
            msg = new SystemMessage() { Sender = "System", Receiver = user.Username, Body = "Authentication successful", Status = true };
            msg.SetType(Communication.Types.User);
            msg.SendMessage(this.ClientIpPort);
            user.Handler = this;
            this.User = user;
            this.ClearObsoleteDictionaryRecords(this.ClientIpPort, user);
            ClientHandler.Receivers.Add(user);
            //this.StartPingPong();
            SystemMessage.SendJoinMessage(user);
            return true;
        }

        private void ClearObsoleteDictionaryRecords(string clientIpPort, User user)
        {
            foreach (KeyValuePair<string, ClientHandler> entry in ClientHandler.HandlersDict)
            {
                if (!entry.Value.IsOnline() || (clientIpPort != entry.Key && (entry.Value.User == null || entry.Value.User.Username == user.Username)))
                {
                    ClientHandler.HandlersDict.Remove(entry.Key);
                }
            }
        }

        private void HandleInvitation(Communication comm)
        {
            Console.WriteLine("Handling invitation");
            var inv = this.Extract(comm.Body, new Invitation());
            inv.HandleInvitation();
        }

        private void HandleMessage(Communication comm)
        {
            var message = this.Extract(comm.Body, new Message());
            foreach (Receiver r in ClientHandler.Receivers)
            {
                if (r.IsReceiver(message.Receiver))
                {
                    r.SendMessage(message);
                }
            }
        }

        private void StartPingPong()
        {
            Timer = new System.Timers.Timer(30000);
            Timer.Elapsed += new ElapsedEventHandler(this.PingPong);
            Timer.AutoReset = true;
            Timer.Enabled = true;
        }

        private void StopPingPong()
        {
            if (Timer != null && Timer.Enabled)
            {
                Timer.Stop();
                Timer.Dispose();
                Timer = null;
            }
        }
    }
}
