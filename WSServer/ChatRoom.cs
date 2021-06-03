using System;
using System.Collections.Generic;

namespace WSServer
{
    class ChatRoom : Receiver
    {
        private List<ClientHandler> Participants { get; set; }

        private List<Message> History;

        private string Name;

        public ChatRoom()
        {
            this.Name = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();
            this.Participants = new List<ClientHandler>();
            this.History = new List<Message>();
        }

        public ChatRoom(String name)
        {
            this.Name = name;
            this.Participants = new List<ClientHandler>();
        }

        public void SetName(string name)
        {
            this.Name = name;
        }

        public bool IsReceiver(string name)
        {
            return this.Name == name;
        }


        public void Add(ClientHandler handler)
        {
            this.Participants.Add(handler);
        }

        public void SendMessage(Message msg)
        {
            this.History.Add(msg);
            foreach (ClientHandler handler in this.Participants)
            {
                if (handler.Client.Connected && handler.GetStream().CanWrite && handler.User != null && handler.User.Username != msg.Sender)
                {
                    msg.SendMessage(handler.GetStream());
                }
            }
        }

        public void SendHistory(ClientHandler handler)
        {
            if (this.Participants.Contains(handler))
            {
                foreach (Message msg in this.History)
                {
                    msg.SendMessage(handler.GetStream());
                }
            }
        }
    }
}
