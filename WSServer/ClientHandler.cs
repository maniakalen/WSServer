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
        public static List<ClientHandler> handlersStack;
        public TcpClient client;
        public static List<Receiver> receivers;


        public User User;
        private Thread th;
        private NetworkStream stream;
        public bool pinged = false;
        public ClientHandler(TcpClient client)
        {
            this.client = client;
            handlersStack.Add(this);
        }

        public void startClient()
        {
            this.th = new Thread(doHandle);
            th.Start();
        }

        private void doHandle()
        {
            stream = client.GetStream();
            System.Timers.Timer timer = new System.Timers.Timer(10000);
            timer.Elapsed += new ElapsedEventHandler(this.PingPong);
            timer.AutoReset = true;
            timer.Enabled = true;

            // enter to an infinite cycle to be able to handle every change in stream
            while (true)
            {
                while (stream.CanRead && !stream.DataAvailable) ;
                if (!stream.CanRead || !this.client.Connected) break;
                while (client.Available < 3) ; // match against "get"

                byte[] bytes = new byte[client.Available];
                stream.Read(bytes, 0, client.Available);
                string s = Encoding.UTF8.GetString(bytes);

                if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
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

                    stream.Write(response, 0, response.Length);
                }
                else
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
                        this.pinged = false;
                        //Console.WriteLine("Ponged!");
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

                        if (text == "[Leaving]" || !this.handleMessage(text, stream))
                            break;
                    }
                    else
                        Console.WriteLine("mask bit not set");

                    Console.WriteLine();
                }


            }
            this.stream.Close();
            this.client.Close();
            Console.WriteLine("Closing thread!");
            timer.Stop();
            timer.Dispose();
            this.th.Join();
        }

        private bool handleMessage(string msg, NetworkStream stream)
        {
            var comDefinition = new Communication();
            var comm = JsonConvert.DeserializeAnonymousType(msg, comDefinition);
            var type = (Communication.Types)comm.Type;
            
            switch (type)
            {
                case Communication.Types.User:
                    var user = this.Extract(comm.Body, new User());
                    if (!user.Authenticate())
                    {
                        return false;
                    }
                    user.Handler = this;
                    this.User = user;
                    ClientHandler.receivers.Add(user);
                    //this.SendStatuses();
                    Console.WriteLine("Username: {0}", user.Username);
                    Console.WriteLine("Password: {0}", user.Password);
                    break;
                case Communication.Types.Invitation:
                    var inv = this.Extract(comm.Body, new Invitation());
                    if (inv.isAccepted)
                    {
                        ChatRoom room = new ChatRoom();
                        foreach (ClientHandler h in ClientHandler.handlersStack)
                        {
                            if (h.User.IsReceiver(inv.Sender) || h.User.IsReceiver(inv.Target))
                            {
                                room.Add(h);
                            }
                        }
                        ClientHandler.receivers.Add(room);
                    }
                    foreach (ClientHandler ch in ClientHandler.handlersStack)
                    {
                        if (ch.User.Username == inv.Target)
                        {
                            inv.SendInvitation(ch.User);
                            break;
                        }
                    }
                    break;
                case Communication.Types.Message:
                    var message = this.Extract(comm.Body, new Message());
                    foreach (Receiver r in ClientHandler.receivers)
                    {
                        if (r.IsReceiver(message.Receiver))
                        {
                            r.SendMessage(message);
                        }
                    }
                    /*foreach (ClientHandler handler in handlersStack)
                    {
                        if (handler.User != null && handler.User.Username == message.Receiver && handler.client.Connected)
                        {
                            handler.User.SendMessage(message);
                        }
                    }*/
                    break;
            }

            return true;
        }

        public NetworkStream GetStream()
        {
            return stream;
        }

        public bool isOnline()
        {
            return this.client.Connected && this.stream.CanRead && this.stream.CanWrite;
        }

        private void SendStatuses()
        {
            List<string> users = new List<string>();
            foreach (ClientHandler h in handlersStack)
            {
                if (h.isOnline() && h.User != null && h.User.Username != this.User.Username)
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
            
            if (this.pinged)
            {
                Console.WriteLine("Closing socket");
                Message msg = new Message() { Body = "off", Sender = this.User.Username };
                foreach (ClientHandler h in ClientHandler.handlersStack)
                {

                }
                this.stream.Close();
                this.client.Close();
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

                this.stream.Write(preparedMsg, 0, preparedMsg.Length);
                this.pinged = true;
            }
        }
    }

    class User : Receiver
    {
        public string Username;
        public string Password;

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
            return true;
        }
        
        public void SendMessage(Message msg)
        {
            msg.Receiver = this.Username;
            msg.SendMessage(this.Handler.GetStream());
        }
    }

    class Message
    {
        public string Receiver;
        public string Sender;
        public string Body;

        public void SendMessage(NetworkStream clientStream)
        {
            lock (clientStream)
            {
                string msg = JsonConvert.SerializeObject(this);
                Console.WriteLine("Sending {0} to {1}", msg, this.Receiver);
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

    class ChatRoom : Receiver
    {
        private List<ClientHandler> Participants { get; set; }

        private string Name;

        public void SetName(string name)
        {
            this.Name = name;
        }

        public bool IsReceiver(string name)
        {
            return this.Name == name;
        }
        

        public void Add(ClientHandler handler)
        {
            this.Participants.Add(handler);
        }

        public void SendMessage(Message msg)
        {
            foreach (ClientHandler handler in this.Participants)
            {
                if (handler.client.Connected && handler.GetStream().CanWrite && handler.User != null && handler.User.Username != msg.Sender)
                {
                    msg.SendMessage(handler.GetStream());
                }
            }
        }
    }

    class Communication
    {
        public enum Types
        {
            User,
            Message,
            Invitation
        };
        public int Type;
        public string Body;
    }

    class Invitation
    {
        public string Sender;
        public string Target;

        public bool isAccepted;


        public void SendInvitation(User rec)
        {
            Message msg = new Message();
            msg.Body = JsonConvert.SerializeObject(this);
            msg.Receiver = this.isAccepted ? this.Sender : this.Target;
            msg.Sender = this.isAccepted ? this.Target : this.Sender;
            rec.SendMessage(msg);
        }
    }

    interface Receiver
    {
        public void SetName(string name);
        public bool IsReceiver(string name);
        public void SendMessage(Message msg);
    }
}