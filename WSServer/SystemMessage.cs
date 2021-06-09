using System.Net.Sockets;
using Newtonsoft.Json;
using System;

namespace WatsonWebsocketServer
{
    class SystemMessage : Message
    {
        public bool Status;

        protected new Communication.Types Type = Communication.Types.System;

        public void HandleMessage(ClientHandler handler)
        {
            if (this.Body == "EXIT")
            {
                try
                {
                    Message msg = new Message() { Body = "<[Off]>", Sender = handler.User.Username };
                    ClientHandler.Broadcast(msg, Communication.Types.System);
                    handler.Close();
                } catch (Exception ee)
                {

                }
            }
            else if (this.Body == "STATUSES")
            {
                handler.SendStatuses();
            }
        }

        public new void SendMessage(string ClientIpPort)
        {
            string body = JsonConvert.SerializeObject(this);
            Communication.Send(ClientIpPort, this.Type, this);
        }

        public static void SendJoinMessage(User user)
        {
            Message msg = new Message() { Body = "<[On]>", Sender = user.Username };
            ClientHandler.Broadcast(msg, Communication.Types.System);
        }

        public void SetType(Communication.Types type)
        {
            this.Type = type;
        }
    }
}
