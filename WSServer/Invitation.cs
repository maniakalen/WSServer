using System;
using Newtonsoft.Json;

namespace WSServer
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
            Communication.Send(rec.Handler.GetStream(), this.Type, this);
        }

        public void HandleInvitation()
        {
            if (this.isAccepted)
            {
                ChatRoom room = new ChatRoom();
                foreach (ClientHandler h in ClientHandler.HandlersStack)
                {
                    if (h.User.IsReceiver(this.Sender) || h.User.IsReceiver(this.Receiver))
                    {
                        room.Add(h);
                    }
                }
                ClientHandler.Receivers.Add(room);
            }
            foreach (ClientHandler ch in ClientHandler.HandlersStack)
            {
                if (ch.User.Username == this.Receiver && ch.IsOnline())
                {
                    this.SendInvitation(ch.User);
                    break;
                }
            }
        }
    }
}
