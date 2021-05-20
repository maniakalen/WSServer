
using Newtonsoft.Json;

namespace WSServer
{
    /// <summary>
    /// Chat invitation class
    /// </summary>
    class Invitation
    {
        /// <summary>
        /// Sender name
        /// </summary>
        public string Sender;
        /// <summary>
        /// Receiver name
        /// </summary>
        public string Target;

        /// <summary>
        /// Whether invitation is accepted or not
        /// </summary>
        public bool isAccepted;

        /// <summary>
        /// Send invitation/invitation response
        /// </summary>
        /// <param name="rec">User to receive the invitation</param>
        public void SendInvitation(User rec)
        {
            Message msg = new Message();
            msg.Body = JsonConvert.SerializeObject(this);
            msg.Receiver = this.isAccepted ? this.Sender : this.Target;
            msg.Sender = this.isAccepted ? this.Target : this.Sender;

            rec.SendMessage(msg);
        }
    }
}
