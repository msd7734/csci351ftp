using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

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

        //CLI commands
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

        //Server responses
        public const int SUCCESS =              200;
        public const int NEW_USER =             220;
        public const int PASSIVE_MODE =         227;
        public const int LOGIN_SUCCESS =        230;
        public const int PASSWORD =             331;

        public const int UNAVAILABLE =          421;
        public const int PERMISSION_DENIED =    550;
        
#endregion

#region Members
        FTPConnection cmdCon;
        FTPConnection dataCon;

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
            cmdCon = new FTPConnection(remote);
            dataCon = null;
            IsOpen = true;
            IsDebug = false;
            CliMode = ClientMode.Passive;
            DatMode = DataMode.Binary;

            ServerMessage initialMsg = cmdCon.ReadMessage();
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

            //This will be replaced by the result message after an operation
            ServerMessage reply = new ServerMessage();

            switch (cid)
            {
                case QUIT:
                    reply = Quit();
                    break;
                case ASCII:
                    reply = Ascii();
                    break;
                case BINARY:
                    reply = Binary();
                    break;
                case CD:
                    reply = Cd(args[0]);
                    break;
                case CDUP:
                    reply = Cdup();
                    break;
                case DEBUG:
                    IsDebug = (IsDebug ? false : true);
                    Console.WriteLine("Debugging turned {0}.", (IsDebug ? "on" : "off"));
                    break;
                case DIR:
                    reply = Dir();
                    break;
                case GET:
                    reply = GetFile(args[0]);
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
                    reply = PutFile(args[0]);
                    break;
                case PWD:
                    reply = Pwd();
                    break;
                case USER:
                    reply = User(args[0]);
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }

            if (!reply.IsEmpty())
            {
                HandleReply(reply);
            }
        }

        private void SendCmd(String cmd, ref FTPConnection con, params string[] args)
        {
            String argStr = " " + String.Join(" ", args);
            String sendStr = String.Format("{0}{1}\r\n", cmd, argStr);
            // TODO: Change to use debug setting. Keeping as true for now, for testing
            DebugLine(String.Format("---> {0}", sendStr), true);

            cmdCon.SendMessage(sendStr);            
        }

        /// <summary>
        /// Act based on a server reply with a 3 digit code attached.
        /// All codes and their messages will be printed, and some will trigger
        ///     further action from the client.
        /// </summary>
        /// <param name="reply">The ServerMessage to process.</param>
        private void HandleReply(ServerMessage reply)
        {
            Console.Write(reply);

            switch (reply.Code)
            {
                case NEW_USER:
                    HandleReply( User() );
                    break;
                case PASSWORD:
                    HandleReply( Password() );
                    break;
                case PASSIVE_MODE:
                    SetDataConnection(reply);
                    break;
                case UNAVAILABLE:
                    IsOpen = false;
                    cmdCon.Close();
                    break;
                case PERMISSION_DENIED:
                    if (dataCon != null)
                        dataCon.Close();
                    break;
                default:
                    break;
            }
        }

#region Client->Server operations

        private ServerMessage Quit()
        {
            SendCmd("QUIT", ref cmdCon);
            ServerMessage reply = cmdCon.ReadMessage();
            IsOpen = false;
            //Send QUIT
            cmdCon.Close();
            return reply;
        }

        private ServerMessage Ascii()
        {
            //Send TYPE A
            SendCmd("TYPE", ref cmdCon, "A");
            ServerMessage reply = cmdCon.ReadMessage();
            DatMode = DataMode.ASCII;
            Console.WriteLine("Switching to ASCII mode.");
            return reply;
        }

        private ServerMessage Binary()
        {
            //Send TYPE I
            SendCmd("TYPE", ref cmdCon, "I");
            ServerMessage reply = cmdCon.ReadMessage();
            DatMode = DataMode.Binary;
            Console.WriteLine("Switching to Binary mode.");
            return reply;
        }

        private ServerMessage Cd(String dir)
        {
            SendCmd("CWD", ref cmdCon, dir);
            ServerMessage reply = cmdCon.ReadMessage();
            return reply;
        }

        private ServerMessage Cdup()
        {
            return Cd("..");
        }

        private ServerMessage Dir()
        {
            ServerMessage reply;

            if (CliMode == ClientMode.Passive)
            {
                SendCmd("PASV", ref cmdCon);
                reply = cmdCon.ReadMessage();
                // if successful, this will initiate a new data connection
                HandleReply(reply);

                if (!dataCon.IsConnected())
                {
                    Console.Error.WriteLine("No connection could be established to retrieve the directory listing.");
                    return reply;
                }

                SendCmd("LIST", ref cmdCon);
                reply = cmdCon.ReadMessage();
                HandleReply(reply);

                Console.Write( dataCon.ReadDirectory() );

                return cmdCon.ReadMessage();
            }
            else
            {
                // send PORT command
                using (FTPListener dirCon = new FTPListener())
                {
                    String commaSepIP = String.Join(",", dirCon.LocalIP.ToString().Split('.'));
                    int octet1 = dirCon.Port / 256;
                    int octet2 = dirCon.Port % 256;
                    String ipParam = String.Format("{0},{1},{2}", commaSepIP, octet1, octet2);

                    SendCmd("PORT", ref cmdCon, ipParam);
                    reply = cmdCon.ReadMessage();
                    if (reply.Code != SUCCESS)
                    {
                        return reply;
                    }
                    HandleReply(reply);

                    SendCmd("LIST", ref cmdCon);
                    reply = cmdCon.ReadMessage();
                    HandleReply(reply);

                    Console.Write( dirCon.AcceptDirectory() );
                }

                return cmdCon.ReadMessage();
            }
        }

        private ServerMessage GetFile(String fileName)
        {
            ServerMessage reply;

            if (CliMode == ClientMode.Passive)
            {
                SendCmd("PASV", ref cmdCon);
                reply = cmdCon.ReadMessage();
                // if successful, this will initiate a new data connection
                HandleReply(reply);

                if (dataCon == null || !dataCon.IsConnected())
                {
                    Console.Error.WriteLine("No connection could be established to retrieve the file.");
                    return reply;
                }

                SendCmd("RETR", ref dataCon, fileName);
                reply = cmdCon.ReadMessage();
                HandleReply(reply);

                if (dataCon == null || !dataCon.IsConnected())
                {
                    return new ServerMessage();
                }

                using (FileStream f = File.Create(fileName))
                {
                    dataCon.ReadFile(f, DatMode);
                }
                

                return cmdCon.ReadMessage();
            }
            else
            {
                // send PORT command
                reply = cmdCon.ReadMessage();
            }

            return null;
        }

        private void Passive()
        {
            CliMode = (CliMode == ClientMode.Active ? ClientMode.Passive : ClientMode.Active);
            Console.WriteLine("Passive mode {0}.", CliMode == ClientMode.Active ? "off" : "on");
        }

        private ServerMessage PutFile(String fileName)
        {
            ServerMessage reply = cmdCon.ReadMessage();
            return reply;
        }

        private ServerMessage Pwd()
        {
            SendCmd("XPWD", ref cmdCon);
            ServerMessage reply = cmdCon.ReadMessage();
            return reply;
        }

        private ServerMessage User(String username)
        {
            SendCmd("USER", ref cmdCon, username);
            ServerMessage reply = cmdCon.ReadMessage();
            return reply;
        }

        private ServerMessage User()
        {
            Console.Write("User ({0}): ", cmdCon.HostName);
            String username = Console.ReadLine();
            SendCmd("USER", ref cmdCon, username);
            ServerMessage reply = cmdCon.ReadMessage();
            return reply;
        }

        private ServerMessage Password()
        {
            Console.Write("Password: ");
            String pass = Console.ReadLine();
            SendCmd("PASS", ref cmdCon, pass);
            ServerMessage reply = cmdCon.ReadMessage();
            return reply;
        }

#endregion

        /// <summary>
        /// Open a data connection (a la passive mode) on the remote to transfer files (or directory info).
        /// This will extract the IP address and port number from a server message.
        /// </summary>
        /// <param name="msg">The server message that provoked opening a new data connection.</param>
        private void SetDataConnection(ServerMessage msg)
        {
            if (msg.Code != PASSIVE_MODE)
            {
                Console.Error.Write(
                    "Attempted to open remote data connection in passive mode.\n{0}",
                    msg
                );
                return;
            }

            if (dataCon != null && dataCon.IsConnected())
            {
                dataCon.Close();
            }


            String targetRegex = @"(\d{1,3},){5}\d{1,3}";
            Match m = Regex.Match(msg.Text, targetRegex);

            if (!m.Success)
            {
                Console.Error.Write(
                    "Attempted to open remote data connection but server seems to have given no connection information:\n{0}",
                    msg
                );
                return;
            }

            String target = m.Value;
            String[] byteStrs = target.Split(',');
            String ipStr = String.Join<String>(".", byteStrs.Take<String>(4));
            System.Net.IPAddress IP = System.Net.IPAddress.Parse(ipStr);

            String[] octetStrs= { byteStrs[byteStrs.Length-2], byteStrs[byteStrs.Length-1] };
            int port = (Int32.Parse(octetStrs[0]) * 256) + Int32.Parse(octetStrs[1]);

            dataCon = new FTPConnection(IP, port);
        }

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
                Console.Write(str);
            }
        }
    }
}
