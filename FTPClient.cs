using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Csci351ftp
{
    /// <summary>
    /// Interpret and execute client commands for an FTP connection.
    /// </summary>
    public class FTPClient
    {

#region Constants
        // Information to parse commands
        public static readonly string[] COMMANDS = { "ascii",
         "binary",
         "cd",
         "cdup",
         "debug",
         "dir",
         "get",
         "help",
         "passive",
         "put",
         "pwd",
         "quit",
         "user" };

        public const int ASCII = 0;
        public const int BINARY = 1;
        public const int CD = 2;
        public const int CDUP = 3;
        public const int DEBUG = 4;
        public const int DIR = 5;
        public const int GET = 6;
        public const int HELP = 7;
        public const int PASSIVE = 8;
        public const int PUT = 9;
        public const int PWD = 10;
        public const int QUIT = 11;
        public const int USER = 12;
#endregion

#region Members
        FTPConnection con;
        UserPrompt prompt;

        /// <summary>
        /// Indicate whether the client is maintaining the connection or if it has closed.
        /// </summary>
        public bool Open { get; private set; }
#endregion

        public FTPClient(String remote)
        {
            // initialize FTPConnection on remote
            con = new FTPConnection(remote);

            prompt = new UserPrompt();

            Open = true;
        }

        public String Exec(String cmd, String[] args) {
            int cid = -1;
            for (int i = 0; i < COMMANDS.Length && cid == -1; ++i)
            {
                if (COMMANDS[i].Equals(cmd, StringComparison.CurrentCultureIgnoreCase))
                {
                    cid = i;
                }
            }

            switch (cid)
            {
                case QUIT:
                    Open = false;
                    goto case 99;   //TEMPORARY setup to test cmd checking
                case ASCII:
                case BINARY:
                case CD:
                case CDUP:
                case DEBUG:
                case DIR:
                case GET:
                case HELP:
                case PASSIVE:
                case PUT:
                case PWD:
                case USER:
                case 99:
                    return "Command is good.";
                default:
                    return "Unknown command.";
            }
        }

        public String? GetPrompt()
        {
            if (prompt == null)
            {
                return null;
            }
            else
            {
                String msg = prompt.Message;
                prompt = prompt.Next;
                return msg;
            }
        }
    }
}
