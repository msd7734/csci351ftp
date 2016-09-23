using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Csci351ftp
{
    public enum ClientMode
    {
        Active,
        Passive
    }

    public enum DataMode
    {
        ASCII,
        Binary
    }

    /// <summary>
    /// Interpret and execute client commands for an FTP connection.
    /// This basically holds all the state for the CLI.
    /// </summary>
    public class FTPClient
    {

        /*
         * FYI:
         * An FTP reply consists of a three digit number (transmitted as
         *  three alphanumeric characters) followed by some text.  The number
         *  is intended for use by automata to determine what state to enter
         *  next; the text is intended for the human user.
         */

        #region Constants
        // Information to parse commands
        public static readonly string[] COMMANDS = {
            "ascii",
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
            "user"
        };

        // Help message

        public static readonly String HELP_MESSAGE = 
	        "ascii      --> Set ASCII transfer type\n"+
	        "binary     --> Set binary transfer type\n"+
	        "cd <path>  --> Change the remote working directory\n"+
	        "cdup       --> Change the remote working directory to the\n"+
                "               parent directory (i.e., cd ..)\n"+
	        "debug      --> Toggle debug mode\n"+
	        "dir        --> List the contents of the remote directory\n"+
	        "get path   --> Get a remote file\n"+
	        "help       --> Displays this text\n"+
	        "passive    --> Toggle passive/active mode\n"+
            "put path   --> Transfer the specified file to the server\n"+
	        "pwd        --> Print the working directory on the server\n"+
            "quit       --> Close the connection to the server and terminate\n"+
	        "user login --> Specify the user name (will prompt for password)";

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
        
        /// <summary>
        /// Indicate whether the client is maintaining the connection or if it has closed.
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Indicate whether the client is in debug mode, which will determine whether to print true FTP commands.
        /// </summary>
        public bool IsDebug { get; private set; }

        /// <summary>
        /// Active or Passive mode, which determines how files are transferred to the remote server.
        /// </summary>
        public ClientMode CliMode { get; private set; }

        /// <summary>
        /// ASCII or Binary, the way to interpret incoming bits.
        /// </summary>
        public DataMode DatMode { get; private set; }
#endregion

        public FTPClient(String remote)
        {
            con = new FTPConnection(remote);
            IsOpen = true;
            IsDebug = false;
            CliMode = ClientMode.Passive;
            DatMode = DataMode.Binary;
        }

        /// <summary>
        /// Handle a CLIENT command and translate it into one or more FTP commands and manage the connection(s)
        /// associated with the execution thereof.
        /// </summary>
        /// <param name="cmd">A case-insensitive command string.</param>
        /// <param name="args">The arguments to be used with the command. Unused arugments will be dropped.</param>
        public void Exec(String cmd, String[] args) {
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
                    IsOpen = false;
                    //Send QUIT
                    con.Close();
                    break;
                case ASCII:
                    //Send TYPE A
                    DatMode = DataMode.ASCII;
                    Console.WriteLine("Switching to ASCII mode.");
                    break;
                case BINARY:
                    //Send TYPE I
                    DatMode = DataMode.Binary;
                    Console.WriteLine("Switching to Binary mode.");
                    break;
                case CD:
                    break;
                case CDUP:
                    break;
                case DEBUG:
                    IsDebug = (IsDebug ? false : true);
                    Console.WriteLine("Debugging turned {0}.", (IsDebug ? "on" : "off"));
                    break;
                case DIR:
                    break;
                case GET:
                    break;
                case HELP:
                    break;
                case PASSIVE:
                    //Send PASV
                    CliMode = (CliMode == ClientMode.Active ? ClientMode.Passive : ClientMode.Active);
                    Console.WriteLine("Switching to {0} mode.", CliMode.ToString());
                    break;
                case PUT:
                    break;
                case PWD:
                    break;
                case USER:
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }

        private void DebugLine(String str, bool debug)
        {
            if (debug)
            {
                Console.WriteLine(str);
            }
        }
    }
}
