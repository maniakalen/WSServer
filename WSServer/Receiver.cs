namespace WatsonWebsocketServer
{
    /// <summary>
    /// Defines basic reciever interface with common methods to use as receiver
    /// </summary>
    interface Receiver
    {
        /// <summary>
        /// Sets the receiver name for identification
        /// </summary>
        /// <param name="name">The name of receiver to be searched</param>
        public void SetName(string name);

        /// <summary>
        /// Checks whether the receiver object matches the name and returns accordingly
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Returns boolean if the receiver matches the name provided</returns>
        public bool IsReceiver(string name);

        /// <summary>
        /// Sends the provided Message to the receiver 
        /// </summary>
        /// <param name="msg">Generated Message object to be sent</param>
        public void SendMessage(Message msg);
    }
}
