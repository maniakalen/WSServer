using System.Collections.Generic;

namespace WSServer
{
    class ChatRoom : Receiver
    {
        private List<ClientHandler> Participants { get; set; }

        private string Name;

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
            foreach (ClientHandler handler in this.Participants)
            {
                if (handler.Client.Connected && handler.GetStream().CanWrite && handler.User != null && handler.User.Username != msg.Sender)
                {
                    msg.SendMessage(handler.GetStream());
                }
            }
        }
    }
}
