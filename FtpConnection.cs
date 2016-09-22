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
        TcpClient tcp;

        public FTPConnection(String hostName)
        {
            // initialize TcpClient  (always on port 21, or 2121 for testing our server)
            tcp = new TcpClient();
            //when connecting, don't forget to check for port 2121 if 21 fails
            //that will be the way to know if something has gone wront
        }
    }
}
