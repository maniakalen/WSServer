using System.Net.Sockets;
using Newtonsoft.Json;

namespace WSServer
{
    class SystemMessage : Message
    {
        public string Sender;
        public string Receiver;
        public string Body;

        public void HandleMessage(ClientHandler handler)
        {
            if (this.Body == "EXIT")
            {
                Message msg = new Message() { Body = "<[Off]>", Sender = handler.User.Username };
                ClientHandler.Broadcast(msg);
                handler.Close();
            } else if (this.Body == "Statuses")
            {
                handler.SendStatuses();
            }
        }

        public void SendMessage(NetworkStream clientStream)
        {
            string body = JsonConvert.SerializeObject(this);
            Communication.Send(clientStream, Communication.Types.System, this);
        }
    }
}
