/*
 * By: Matthew Dennis (msd7734)
 */

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
    /// Manage network resources by wrapping a single TcpClient.
    /// </summary>
    public class FTPConnection
    {
        // Ports
        private const int MAIN_PORT= 21;
        private const int FALLBACK_PORT = 2121;

        private TcpClient tcp;
        private byte[] buf;
        
        /// <summary>
        /// The IP address used for this connection.
        /// </summary>
        public IPAddress IP { get; private set; }

        /// <summary>
        /// The DNS-provided host name for this connection.
        /// </summary>
        public String HostName { get; private set; }

        /// <summary>
        /// Create a new FTPConnection and connect to the remote.
        /// </summary>
        /// <param name="hostName">The hostname of the remote.</param>
        /// <param name="port">The port to connect to. Port 21 by default.</param>
        /// <param name="bufferSize">The size of the buffer used for read operations. Default 512kb.</param>
        public FTPConnection(String hostName, int port = MAIN_PORT, int bufferSize = 0x40000)
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
                    tcp.Connect(hostName, FALLBACK_PORT);
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
        /// Create a new FTPConnection and connect to the remote.
        /// </summary>
        /// <param name="address">TheIP address of the remote.</param>
        /// <param name="port">The port to connect to. Port 21 by default.</param>
        /// <param name="bufferSize">The size of the buffer used for read operations. Default 512kb.</param>
        public FTPConnection(IPAddress address, int port = MAIN_PORT, int bufferSize = 0x40000)
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
                    tcp.Connect(IP, FALLBACK_PORT);
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

        /// <summary>
        /// Read a directory listing provided by the remote. Used with passive mode.
        /// This will not receive data if the remote has not established this FTPConnection
        ///     as a data connection.
        /// </summary>
        /// <returns>The directory string provided by the server.</returns>
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

        /// <summary>
        /// Read a file provided by the remote. Used with passive mode.
        /// This will not receive data if the remote has not established this FTPConnection
        ///     as a data connection.
        /// </summary>
        /// <param name="file">The stream to write the file data to.</param>
        /// <param name="mode">The mode in which to interpret the file data.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Send a message to the server.
        /// This message may not be interpreted if the server has not accepted this
        ///     FTPConnection as a command connection.
        /// </summary>
        /// <param name="message"></param>
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
