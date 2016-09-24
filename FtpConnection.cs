using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

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

        public FTPConnection(String hostName, int bufferSize = 0x40000)
        {
            try
            {
                IP = Dns.GetHostAddresses(hostName)[0];
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
                tcp.Connect(hostName, 21);
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
        /// Basic a server message incl. a status code.
        /// </summary>
        /// <returns></returns>
        public ServerMessage ReadMessage()
        {
            Array.Clear(buf, 0, buf.Length);
            NetworkStream stream = tcp.GetStream();
            int bytesRead = stream.Read(buf, 0, buf.Length);
            return new ServerMessage(buf.Take<byte>(bytesRead).ToArray<byte>());
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
