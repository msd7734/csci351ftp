/*
 * By: Matthew Dennis (msd7734)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace Csci351ftp
{
    /// <summary>
    /// A wrapper for a TcpListener that will accept the server's file in active mode.
    /// </summary>
    class FTPListener : IDisposable
    {
        TcpListener tcp;
        byte[] buf;

        /// <summary>
        /// The client's IP.
        /// </summary>
        public IPAddress LocalIP { get; private set; }

        /// <summary>
        /// The port the client is listening on.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Construct an FTPListener intended to accept 1 connection.
        /// </summary>
        /// <param name="bufferSize">The buffer size to allow the FTPListener. 512kb by default.</param>
        public FTPListener(int bufferSize = 0x40000)
        {
            buf = new byte[bufferSize];
            tcp = new TcpListener(IPAddress.Any, 0);
            tcp.Start(1);

            IPHostEntry host = Dns.GetHostByName(Dns.GetHostName());
            LocalIP = host.AddressList[0];

            Port = Int32.Parse(tcp.LocalEndpoint.ToString().Split(':')[1]);
        }

        /// <summary>
        /// Destructor for the FTPListener.
        /// </summary>
        ~FTPListener()
        {
            try
            {
                tcp.Stop();
            }
            catch (NullReferenceException nre)
            {
                // If it's already closed, no need to do anything
            }
        }

        /// <summary>
        /// Make the FTPListener disposable.
        /// </summary>
        public void Dispose()
        {
            tcp.Stop();
        }

        /// <summary>
        /// Receive a directory string from the server in active mode.
        /// </summary>
        /// <returns></returns>
        public String AcceptDirectory()
        {
            StringBuilder resultBuilder = new StringBuilder();
            using (TcpClient con = tcp.AcceptTcpClient())
            {
                NetworkStream stream = con.GetStream();
                using (StreamReader reader = new StreamReader(stream, Encoding.ASCII))
                {
                    String line = String.Empty;
                    while ((line = reader.ReadLine()) != null)
                    {
                        resultBuilder.Append(line + "\r\n");
                    }
                }
            }

            return resultBuilder.ToString();
        }

        /// <summary>
        /// Receive a file from the server in active mode.
        /// </summary>
        /// <param name="file">A stream where the incoming file will be written.</param>
        /// <param name="mode">The mode in which to interpret the incoming data.</param>
        /// <returns></returns>
        public FileStream AcceptFile(FileStream file, DataMode mode)
        {
            using (TcpClient con = tcp.AcceptTcpClient())
            {
                NetworkStream stream = con.GetStream();

                if (mode == DataMode.ASCII)
                {
                    using (StreamReader reader = new StreamReader(stream, Encoding.ASCII))
                    {
                        String line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line + "\r\n";
                            byte[] b = Encoding.ASCII.GetBytes(line);
                            file.Write(b, 0, b.Length);
                        }
                    }
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
                }
            }
            return file;
        }
    }
}
