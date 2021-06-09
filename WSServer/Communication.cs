using System;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WatsonWebsocketServer
{
    /// <summary>
    /// Communication class to define communication protocol
    /// 
    /// Example: {"Type":1, "Body":"{\"Sender\":\"User1\",\"Receiver\":\"User2\", \"Body\", \"Body message\"}"}
    /// 
    /// </summary>
    class Communication
    {
        /// <summary>
        /// Communication types enumeration to identify the type of communication received
        /// </summary>
        public enum Types
        {
            User,
            Message,
            Invitation,
            System
        };

        /// <summary>
        /// The type of communication received
        /// </summary>
        public int Type;

        /// <summary>
        /// Communication body. Most commonly json
        /// </summary>
        public string Body;


        /// <summary>
        /// Sending Communication Body to provided clientStream if clientStream is Writable.
        /// </summary>
        /// <param name="clientStream">Stream to send the message to</param>
        public void Send(string ClientIpPort)
        {
            ClientHandler.Server.SendAsync(ClientIpPort, JsonConvert.SerializeObject(this)).Wait();
        }

        public static void Send(string ClientIpPort, Communication.Types Type, Message msg)
        {
            Communication com = new Communication() { Type = (int)Type, Body = JsonConvert.SerializeObject(msg) };
            com.Send(ClientIpPort);
        }
    }
}

