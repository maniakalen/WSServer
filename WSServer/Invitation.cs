using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WatsonWebsocketServer
{
    /// <summary>
    /// Chat invitation class
    /// </summary>
    class Invitation : Message
    {
        /// <summary>
        /// Whether invitation is accepted or not
        /// </summary>
        public bool isAccepted = false;

        protected new Communication.Types Type = Communication.Types.Invitation;

        /// <summary>
        /// Send invitation/invitation response
        /// </summary>
        /// <param name="rec">User to receive the invitation</param>
        public void SendInvitation(User rec)
        {
            Console.WriteLine("Type {0}", this.Type.ToString());
            Communication.Send(rec.Handler.ClientIpPort, this.Type, this);
        }

        public void HandleInvitation()
        {
            if (this.isAccepted)
            {
                ChatRoom room = new ChatRoom();
                foreach (KeyValuePair<string, ClientHandler> entry in ClientHandler.HandlersDict)
                {
                    if (entry.Value.User.IsReceiver(this.Sender) || entry.Value.User.IsReceiver(this.Receiver))
                    {
                        room.Add(entry.Value);
                    }
                }
                ClientHandler.Receivers.Add(room);
            }
            foreach (KeyValuePair<string, ClientHandler> entry in ClientHandler.HandlersDict)
            {
                if (entry.Value.User != null && entry.Value.User.Username == this.Receiver && entry.Value.IsOnline())
                {
                    this.SendInvitation(entry.Value.User);
                    break;
                }
            }
        }
    }
}
