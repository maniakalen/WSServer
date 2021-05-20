using System;
using System.Text;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace WSServer
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
        public void Send(NetworkStream clientStream)
        {
            if (clientStream.CanWrite)
            {
                lock (clientStream)
                {
                    string msg = JsonConvert.SerializeObject(this);
                    try
                    {
                        byte fin = 0b10000000;
                        byte opcode = 0b00000001;
                        byte firstByte = opcode;
                        int len = 125;
                        do
                        {
                            if (msg.Length < len)
                            {
                                len = msg.Length;
                                firstByte = (byte)(firstByte | fin);
                            }
                            string message = msg.Substring(0, len);
                            msg = msg.Remove(0, len);

                            byte[] byteMessage = Encoding.ASCII.GetBytes(message);
                            byte[] empty = new byte[2] { firstByte, Convert.ToByte(message.Length) };
                            byte[] preparedMsg = new byte[byteMessage.Length + empty.Length];
                            Buffer.BlockCopy(empty, 0, preparedMsg, 0, empty.Length);
                            Buffer.BlockCopy(byteMessage, 0, preparedMsg, empty.Length, byteMessage.Length);
                            clientStream.Write(preparedMsg, 0, preparedMsg.Length);
                            firstByte = 0; 
                        } while (msg.Length > 0);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: {0}", e.Message);
                    }
                }
            }
        }

        public static void Send(NetworkStream clientStream, Communication.Types Type, Message msg)
        {
            Communication com = new Communication() { Type = (int)Type, Body = JsonConvert.SerializeObject(msg) };
            com.Send(clientStream);
        }
    }
}
