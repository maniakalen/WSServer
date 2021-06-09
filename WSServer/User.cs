namespace WatsonWebsocketServer
{
    class User : Receiver
    {
        public string Username;
        public int Uid;
        public string Hash;

        public ClientHandler Handler;

        public void SetName(string name)
        {
            this.Username = name;
        }
        public bool IsReceiver(string name)
        {
            return this.Username == name;
        }

        public bool Authenticate()
        {
            int count = ClientHandler.Db.AuthUser(this.Uid, this.Hash);

            return count > 0;
        }

        public void SendMessage(Message msg)
        {
            msg.Receiver = this.Username;
            msg.SendMessage(this.Handler.ClientIpPort);
        }
    }
}
