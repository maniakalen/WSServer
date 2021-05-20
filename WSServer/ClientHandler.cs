using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Timers;

namespace WSServer
{
    class ClientHandler
    {
        public static List<ClientHandler> HandlersStack;
        public TcpClient Client;
        public static List<Receiver> Receivers;
        public static DbConnector Db;

        public User User;
        private Thread Thread;
        private NetworkStream Stream;
        public bool Pinged = false;
        public ClientHandler(TcpClient client)
        {
            this.Client = client;
            HandlersStack.Add(this);
        }

        public void StartClient()
        {
            this.Thread = new Thread(DoHandle);
            Thread.Start();
        }

        private void DoHandle()
        {
            Stream = Client.GetStream();
            System.Timers.Timer timer = new System.Timers.Timer(30000);
            timer.Elapsed += new ElapsedEventHandler(this.PingPong);
            timer.AutoReset = true;
            timer.Enabled = true;

            // enter to an infinite cycle to be able to handle every change in stream
            while (true)
            {
                while (Stream.CanRead && !Stream.DataAvailable) ;
                if (!Stream.CanRead || !this.Client.Connected) break;
                while (Client.Available < 3) ; // match against "get"

                byte[] bytes = new byte[Client.Available];
                Stream.Read(bytes, 0, Client.Available);
                string s = Encoding.UTF8.GetString(bytes);

                if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
                {
                    this.Handshake(s);
                }
                else if (!this.HandleIncomingMessage(bytes))
                {
                    break;
                }


            }
            this.Close();
            Console.WriteLine("Closing thread!");
            timer.Stop();
            timer.Dispose();
            this.Thread.Join();
        }

        private bool HandleMessage(string msg, NetworkStream stream)
        {
            var comDefinition = new Communication();
            try
            {
                var comm = JsonConvert.DeserializeAnonymousType(msg, comDefinition);
            
                var type = (Communication.Types)comm.Type;
            
                switch (type)
                {
                    case Communication.Types.User:
                        return this.UserLogin(comm);
                        break;
                    case Communication.Types.Invitation:
                        this.HandleInvitation(comm);
                        break;
                    case Communication.Types.Message:
                        this.HandleMessage(comm);
                        break;
                    case Communication.Types.System:
                        var systemMessage = this.Extract(comm.Body, new SystemMessage());
                        systemMessage.HandleMessage(this);
                        break;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return true;
            }
            return true;
        }

        public static void Broadcast(Message msg)
        {
            foreach (ClientHandler h in ClientHandler.HandlersStack)
            {
                if (msg.Sender == null || (h.User != null && h.User.Username != msg.Sender))
                {
                    msg.Receiver = h.User.Username;
                    Communication.Send(h.GetStream(), Communication.Types.Message, msg);
                }
            }
        }

        public static void Broadcast(Message msg, Communication.Types Type)
        {
            foreach (ClientHandler h in ClientHandler.HandlersStack)
            {
                if (msg.Sender == null || (h.User != null && h.User.Username != msg.Sender))
                {
                    msg.Receiver = h.User.Username;
                    Communication.Send(h.GetStream(), Type, msg);
                }   
            }
        }

        public void Close()
        {
            this.Stream.Close();
            this.Client.Close();
        }

        public NetworkStream GetStream()
        {
            return Stream;
        }

        public bool IsOnline()
        {
            return this.Client.Connected && this.Stream.CanRead && this.Stream.CanWrite;
        }

        public void SendStatuses()
        {
            List<string> users = new List<string>();
            foreach (ClientHandler h in HandlersStack)
            {
                if (h.IsOnline() && h.User != null && h.User.Username != this.User.Username)
                {
                    users.Add(h.User.Username);
                }
            }

            Message msg = new Message() { Sender = "System.Statuses", Body = JsonConvert.SerializeObject(users.ToArray()) };
            this.User.SendMessage(msg);
        }

        private T Extract<T>(string json, T definition)
        {
            try
            {
                return JsonConvert.DeserializeAnonymousType(json, definition);
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw e;
            }
        }

        private void PingPong(object source, ElapsedEventArgs e)
        {
            
            if (this.Pinged)
            {
                Console.WriteLine("Closing socket");
                Message msg = new Message() { Body = "off", Sender = this.User.Username };
                ClientHandler.Broadcast(msg);
                this.Close();
            }
            else
            {
                //Console.WriteLine("Pinging");
                byte pingCode = 0x9;
                byte fin = 0b10000000;
                string message = "Ping";
                byte[] byteMessage = Encoding.ASCII.GetBytes(message);
                byte[] empty = new byte[2] { (byte)(pingCode | fin), Convert.ToByte(message.Length) };
                byte[] preparedMsg = new byte[byteMessage.Length + empty.Length];
                Buffer.BlockCopy(empty, 0, preparedMsg, 0, empty.Length);
                Buffer.BlockCopy(byteMessage, 0, preparedMsg, empty.Length, byteMessage.Length);

                this.Stream.Write(preparedMsg, 0, preparedMsg.Length);
                this.Pinged = true;
                Console.Write(this.User.Username + ": Ping - ");
            }
        }

        private void Handshake(string s)
        {
            Console.WriteLine("=====Handshaking from client=====\n{0}", s);

            // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
            // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
            // 3. Compute SHA-1 and Base64 hash of the new value
            // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
            string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
            string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
            string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

            // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
            byte[] response = Encoding.UTF8.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Connection: Upgrade\r\n" +
                "Upgrade: websocket\r\n" +
                "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

            Stream.Write(response, 0, response.Length);
        }

        private bool HandleIncomingMessage(byte[] bytes)
        {
            bool fin = (bytes[0] & 0b10000000) != 0,
                        mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

            int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                msglen = bytes[1] - 128, // & 0111 1111
                offset = 2;
            if (msglen == 126)
            {
                // was ToUInt16(bytes, offset) but the result is incorrect
                msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                offset = 4;
            }
            else if (msglen == 127)
            {
                Console.WriteLine("TODO: msglen == 127, needs qword to store msglen");
                // i don't really know the byte order, please edit this
                // msglen = BitConverter.ToUInt64(new byte[] { bytes[5], bytes[4], bytes[3], bytes[2], bytes[9], bytes[8], bytes[7], bytes[6] }, 0);
                // offset = 10;
            }

            if (opcode == 0xA)
            {
                this.Pinged = false;
                Console.WriteLine("Pong!");
            }
            else if (msglen == 0)
            {
                Console.WriteLine("msglen == 0");
            }
            else if (mask)
            {
                byte[] decoded = new byte[msglen];
                byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                offset += 4;

                for (int i = 0; i < msglen; ++i)
                    decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

                string text = Encoding.UTF8.GetString(decoded);
                Console.WriteLine("{0}", text);

                return (text != "[Leaving]" && this.HandleMessage(text, Stream));
                    
            }
            else
                Console.WriteLine("mask bit not set");

            return false;
        }

        private bool UserLogin(Communication comm)
        {
            var user = this.Extract(comm.Body, new User());
            if (!user.Authenticate())
            {
                return false;
            }
            user.Handler = this;
            this.User = user;
            ClientHandler.Receivers.Add(user);
            return true;
        }

        private void HandleInvitation(Communication comm)
        {
            var inv = this.Extract(comm.Body, new Invitation());
            if (inv.isAccepted)
            {
                ChatRoom room = new ChatRoom();
                foreach (ClientHandler h in ClientHandler.HandlersStack)
                {
                    if (h.User.IsReceiver(inv.Sender) || h.User.IsReceiver(inv.Target))
                    {
                        room.Add(h);
                    }
                }
                ClientHandler.Receivers.Add(room);
            }
            foreach (ClientHandler ch in ClientHandler.HandlersStack)
            {
                if (ch.User.Username == inv.Target)
                {
                    inv.SendInvitation(ch.User);
                    break;
                }
            }
        }

        private void HandleMessage(Communication comm)
        {
            var message = this.Extract(comm.Body, new Message());
            foreach (Receiver r in ClientHandler.Receivers)
            {
                if (r.IsReceiver(message.Receiver))
                {
                    r.SendMessage(message);
                }
            }
        }
    }
}