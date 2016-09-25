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
    class FTPListener : IDisposable
    {
        TcpListener tcp;
        byte[] buf;

        public IPAddress LocalIP { get; private set; }

        public int Port { get; private set; }

        public FTPListener(int bufferSize = 0x40000)
        {
            buf = new byte[bufferSize];
            tcp = new TcpListener(IPAddress.Any, 0);
            tcp.Start(1);

            IPHostEntry host = Dns.GetHostByName(Dns.GetHostName());
            LocalIP = host.AddressList[0];

            Port = Int32.Parse(tcp.LocalEndpoint.ToString().Split(':')[1]);
        }

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

        public void Dispose()
        {
            tcp.Stop();
        }

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

        public void AcceptFile(DataMode mode)
        {

        }
    }
}
