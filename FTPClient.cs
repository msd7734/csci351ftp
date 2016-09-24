using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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

        public const int NEW_USER = 220;
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

            ServerMessage initialMsg = con.ReadMessage();
            //Console.WriteLine(initialMsg); <-- do this in HandleReply
            HandleReply(initialMsg);
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
                    Quit();
                    break;
                case ASCII:
                    Ascii();
                    break;
                case BINARY:
                    Binary();
                    break;
                case CD:
                    Cd(args[0]);
                    break;
                case CDUP:
                    Cdup();
                    break;
                case DEBUG:
                    IsDebug = (IsDebug ? false : true);
                    Console.WriteLine("Debugging turned {0}.", (IsDebug ? "on" : "off"));
                    break;
                case DIR:
                    Dir();
                    break;
                case GET:
                    GetFile(args[0]);
                    break;
                case HELP:
                    if (args.Length > 0)
                        Help(args[0]);
                    else
                        Help(String.Empty);
                    break;
                case PASSIVE:
                    Passive();
                    break;
                case PUT:
                    PutFile(args[0]);
                    break;
                case PWD:
                    Pwd();
                    break;
                case USER:
                    User(args[0]);
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }

        /// <summary>
        /// Act given a server reply with a 3 digit code attached.
        /// </summary>
        /// <param name="reply">The ServerMessage to process.</param>
        private void HandleReply(ServerMessage reply)
        {
            Console.Write(reply);

            switch (reply.Code)
            {
                case NEW_USER:

                default:
                    break;
            }
        }

#region Client->Server operations

        private void Quit()
        {
            IsOpen = false;
            //Send QUIT
            con.Close();
        }

        private void Ascii()
        {
            //Send TYPE A
            DatMode = DataMode.ASCII;
            Console.WriteLine("Switching to ASCII mode.");
        }

        private void Binary()
        {
            //Send TYPE I
            DatMode = DataMode.Binary;
            Console.WriteLine("Switching to Binary mode.");
        }

        private void Cd(String dir)
        {

        }

        private void Cdup()
        {

        }

        private void Dir()
        {

        }

        private void GetFile(String fileName)
        {

        }

        private void Passive()
        {
            //Send PASV
            CliMode = (CliMode == ClientMode.Active ? ClientMode.Passive : ClientMode.Active);
            Console.WriteLine("Switching to {0} mode.", CliMode.ToString());
        }

        private void PutFile(String fileName)
        {

        }

        private void Pwd()
        {

        }

        private void User(String username)
        {

        }

#endregion


        private void Help(String command)
        {
            if (String.IsNullOrWhiteSpace(command))
            {
                Console.WriteLine(HELP_MESSAGE);
            }
            else
            {
                String[] lines = HELP_MESSAGE.Split('\n');
                foreach (String l in lines) {
                    if (command.Length <= l.Length && l.Substring(0, command.Length) == command)
                    {
                        Console.WriteLine(l);
                        return;
                    }
                }
                Console.WriteLine("Inavlid help command {0}.", command);
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
