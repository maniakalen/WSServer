using System.Net.Sockets;
using Newtonsoft.Json;

namespace WatsonWebsocketServer
{
    /// <summary>
    /// Message to be sent to the Receiver in the name of Sender
    /// </summary>
    class Message
    {
        public string Receiver;
        public string Sender;
        public string Body;

        protected Communication.Types Type = Communication.Types.Message;

        public void SendMessage(string ClientIpPort)
        {
            string body = JsonConvert.SerializeObject(this);
            Communication.Send(ClientIpPort, this.Type, this);
        }
    }
}

