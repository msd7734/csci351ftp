using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Csci351ftp
{
    /// <summary>
    /// Manage network resources.
    /// </summary>
    public class FTPConnection
    {
        private TcpClient tcp;
        private byte[] buf;
        
        public IPAddress IP { get; private set; }

        public String HostName { get; private set; }

        public FTPConnection(String hostName, int port = 21, int bufferSize = 0x40000)
        {
            try
            {
                IP = Dns.GetHostAddresses(hostName)[0];
                HostName = Dns.GetHostEntry(IP).HostName;
            }
            catch (SocketException se)
            {
                throw new ArgumentException("Host " + hostName + " could not be reached.", se);
            }

            // initialize TcpClient  (always on port 21, or 2121 for testing our server)
            tcp = new TcpClient();
            buf = new byte[bufferSize];

            try
            {
                tcp.Connect(hostName, port);
            }
            catch (SocketException se)
            {
                try
                {
                    tcp.Connect(hostName, 2121);
                }
                catch (Exception einner)
                {
                    throw einner;
                }
            }
            catch (Exception eouter)
            {
                Console.Error.WriteLine(eouter.Message);
                tcp.Close();
            }

            /*
            if (tcp.Connected)
            {
                NetworkStream stream = tcp.GetStream();
                //test
                int bytesRead = stream.Read(buf, 0, buf.Length);
                Console.WriteLine("Bytes read from server: " + bytesRead.ToString());
                StringBuilder resp = new StringBuilder(bytesRead);
                for (int i = 0; i < bytesRead; ++i)
                {
                    resp.Append(Convert.ToChar(buf[i]));
                }
                Console.WriteLine(resp.ToString());
            }
             * */
            
        }

        public FTPConnection(IPAddress address, int port = 21, int bufferSize = 0x40000)
        {
            IP = address;
            try
            {
                HostName = Dns.GetHostEntry(IP).HostName;
            }
            catch (SocketException se)
            {
                throw new ArgumentException("Host at " + IP.ToString() + " could not be reached.", se);
            }

            // initialize TcpClient  (always on port 21, or 2121 for testing our server)
            tcp = new TcpClient();
            buf = new byte[bufferSize];

            try
            {
                tcp.Connect(IP, port);
            }
            catch (SocketException se)
            {
                try
                {
                    tcp.Connect(IP, 2121);
                }
                catch (Exception einner)
                {
                    throw einner;
                }
            }
            catch (Exception eouter)
            {
                Console.Error.WriteLine(eouter.Message);
                tcp.Close();
            }
        }

        /// <summary>
        /// Destructor to ensure connection is released.
        /// </summary>
        ~FTPConnection()
        {
            try
            {
                tcp.Close();
            }
            catch (NullReferenceException nre)
            {
                // If it's never been opened, no need to do anything
            }
        }

        /// <summary>
        /// Read from the server expecting a single command message including a status code.
        /// </summary>
        /// <returns></returns>
        public ServerMessage ReadMessage()
        {
            Array.Clear(buf, 0, buf.Length);
            NetworkStream stream = tcp.GetStream();
            int bytesRead = stream.Read(buf, 0, buf.Length);
            return new ServerMessage(buf.Take<byte>(bytesRead).ToArray<byte>());
        }

        public String ReadDirectory()
        {
            Array.Clear(buf, 0, buf.Length);
            NetworkStream stream = tcp.GetStream();
            StringBuilder resultBuilder = new StringBuilder();

            using (StreamReader reader = new StreamReader(stream, Encoding.ASCII))
            {
                String line;
                while ((line = reader.ReadLine()) != null)
                {
                    resultBuilder.Append(line+"\r\n");
                }
            }
            return resultBuilder.ToString();
        }

        public ServerMessage ReadFile()
        {
            //stub
            return null;
        }

        public void SendMessage(String message)
        {
            NetworkStream stream = tcp.GetStream();
            byte[] msgBytes = Encoding.ASCII.GetBytes(message);
            stream.Write(msgBytes, 0, msgBytes.Length);
        }

        /// <summary>
        /// Close the underlying TCPClient
        /// </summary>
        public void Close()
        {
            tcp.Close();
        }

        public bool IsConnected()
        {
            return tcp.Connected;
        }
    }
}
