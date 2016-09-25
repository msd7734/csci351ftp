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
                // If it's already closed, no need to do anything
            }
        }

        /// <summary>
        /// Read from the server expecting a single command message including a status code.
        /// </summary>
        /// <returns>A ServerMessage object containing the parsed status code and message.</returns>
        public ServerMessage ReadMessage()
        {
            Array.Clear(buf, 0, buf.Length);
            NetworkStream stream = tcp.GetStream();
            StringBuilder msg = new StringBuilder();
            StreamReader reader = new StreamReader(stream);
            String line = String.Empty;
            try
            {
                while (!ServerMessage.IsLastMessageLine(line))
                {
                    line = reader.ReadLine();
                    msg.Append(line + "\r\n");
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
            return new ServerMessage(msg.ToString());
        }

        public String ReadDirectory()
        {
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

        public FileStream ReadFile(FileStream file, DataMode mode)
        {
            NetworkStream stream = tcp.GetStream();

            if (mode == DataMode.ASCII)
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.ASCII))
                {
                    String line;

                    try
                    {
                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line + "\r\n";
                            byte[] b = Encoding.ASCII.GetBytes(line);
                            file.Write(b, 0, b.Length);
                        }
                    }
                    catch (IOException ioe)
                    {
                        Console.Error.WriteLine(ioe.Message);
                    }
                }
                return file;
            }
            else
            {
                // implicitly binary mode
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    int bytesRead = 0;

                    try
                    {
                        do
                        {
                            Array.Clear(buf, 0, buf.Length);
                            bytesRead = reader.Read(buf, 0, buf.Length);
                            file.Write(buf, 0, bytesRead);
                        }
                        while (bytesRead != 0);
                    }
                    catch (IOException ioe)
                    {
                        Console.Error.WriteLine(ioe.Message);
                    }
                }

                return file;
            }
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
            try
            {
                return tcp.Connected;
            }
            catch (NullReferenceException nre)
            {
                return false;
            }
        }
    }
}
