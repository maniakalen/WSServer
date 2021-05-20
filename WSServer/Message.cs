using System.Net.Sockets;
using Newtonsoft.Json;

namespace WSServer
{
    /// <summary>
    /// Message to be sent to the Receiver in the name of Sender
    /// </summary>
    class Message
    {
        public string Receiver;
        public string Sender;
        public string Body;

        public void SendMessage(NetworkStream clientStream)
        {
            string body = JsonConvert.SerializeObject(this);
            Communication.Send(clientStream, Communication.Types.Message, this);
        }
    }
}
