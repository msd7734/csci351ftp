﻿/*
 * By: Matthew Dennis (msd7734)
 */

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

        /// <summary>
        /// Construct an FTP client to connect to the given remote.
        /// </summary>
        /// <param name="remote"></param>
        public FTPClient(String remote)
        {
            try
            {
                cmdCon = new FTPConnection(remote);
                dataCon = null;
                IsOpen = true;
                IsDebug = false;
                CliMode = ClientMode.Passive;
                DatMode = DataMode.Binary;

                ServerMessage initialMsg = cmdCon.ReadMessage();
                HandleReply(initialMsg);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                IsOpen = false;
            }
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

        /// <summary>
        /// Send a command to the server.
        /// </summary>
        /// <param name="cmd">The identifying command term.</param>
        /// <param name="args">Any arguments to go with the command.</param>
        private void SendCmd(String cmd, params string[] args)
        {
            String argStr = " " + String.Join(" ", args);
            String sendStr = String.Format("{0}{1}\r\n", cmd, argStr);
            DebugLine(String.Format("---> {0}", sendStr), IsDebug);

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

        /// <summary>
        /// Quit from the server and end the connection.
        /// </summary>
        /// <returns>A server message in response to this action.</returns>
        private ServerMessage Quit()
        {
            try
            {
                SendCmd("QUIT");
                ServerMessage reply = cmdCon.ReadMessage();
                IsOpen = false;
                //Send QUIT
                cmdCon.Close();
                return reply;
            }
            catch (Exception e)
            {
                Console.WriteLine("The connection has been closed.");
                IsOpen = false;
                return new ServerMessage();
            }
            
        }
        
        /// <summary>
        /// Set the server to ASCII mode.
        /// </summary>
        /// <returns>A server message in response to this action.</returns>
        private ServerMessage Ascii()
        {
            //Send TYPE A
            SendCmd("TYPE", "A");
            ServerMessage reply = cmdCon.ReadMessage();
            DatMode = DataMode.ASCII;
            Console.WriteLine("Switching to ASCII mode.");
            return reply;
        }

        /// <summary>
        /// Set the server to Binary mode.
        /// </summary>
        /// <returns>A server message in response to this action.</returns>
        private ServerMessage Binary()
        {
            //Send TYPE I
            SendCmd("TYPE", "I");
            ServerMessage reply = cmdCon.ReadMessage();
            DatMode = DataMode.Binary;
            Console.WriteLine("Switching to Binary mode.");
            return reply;
        }

        /// <summary>
        /// Change the working directory.
        /// </summary>
        /// <param name="dir">The new directory</param>
        /// <returns>A server message in response to this action.</returns>
        private ServerMessage Cd(String dir)
        {
            SendCmd("CWD", dir);
            ServerMessage reply = cmdCon.ReadMessage();
            return reply;
        }

        /// <summary>
        /// Go up one level in the directory structure.
        /// </summary>
        /// <returns>A server message in response to this action.</returns>
        private ServerMessage Cdup()
        {
            return Cd("..");
        }

        /// <summary>
        /// Get a list of all the files in the current working directory from the server.
        /// </summary>
        /// <returns>A server message in response to this action.</returns>
        private ServerMessage Dir()
        {
            ServerMessage reply;

            if (CliMode == ClientMode.Passive)
            {
                SendCmd("PASV");
                reply = cmdCon.ReadMessage();
                // if successful, this will initiate a new data connection
                HandleReply(reply);

                if (!dataCon.IsConnected())
                {
                    Console.Error.WriteLine("No connection could be established to retrieve the directory listing.");
                    return reply;
                }

                SendCmd("LIST");
                reply = cmdCon.ReadMessage();
                HandleReply(reply);

                Console.Write( dataCon.ReadDirectory() );

                return cmdCon.ReadMessage();
            }
            else
            {
                using (FTPListener dirCon = new FTPListener())
                {
                    String commaSepIP = String.Join(",", dirCon.LocalIP.ToString().Split('.'));
                    int octet1 = dirCon.Port / 256;
                    int octet2 = dirCon.Port % 256;
                    String ipParam = String.Format("{0},{1},{2}", commaSepIP, octet1, octet2);

                    SendCmd("PORT", ipParam);
                    reply = cmdCon.ReadMessage();
                    if (reply.Code != SUCCESS)
                    {
                        return reply;
                    }
                    HandleReply(reply);

                    SendCmd("LIST");
                    reply = cmdCon.ReadMessage();
                    HandleReply(reply);

                    Console.Write( dirCon.AcceptDirectory() );
                }

                return cmdCon.ReadMessage();
            }
        }

        /// <summary>
        /// Get a file from the server.
        /// </summary>
        /// <param name="fileName">The name of the file to request.</param>
        /// <returns>A server message in response to this action.</returns>
        private ServerMessage GetFile(String fileName)
        {
            ServerMessage reply;

            if (CliMode == ClientMode.Passive)
            {
                SendCmd("PASV");
                reply = cmdCon.ReadMessage();
                // if successful, this will initiate a new data connection
                HandleReply(reply);

                if (dataCon == null || !dataCon.IsConnected())
                {
                    Console.Error.WriteLine("No connection could be established to retrieve the file.");
                    return reply;
                }

                SendCmd("RETR", fileName);
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
                using (FTPListener fileCon = new FTPListener())
                {
                    String commaSepIP = String.Join(",", fileCon.LocalIP.ToString().Split('.'));
                    int octet1 = fileCon.Port / 256;
                    int octet2 = fileCon.Port % 256;
                    String ipParam = String.Format("{0},{1},{2}", commaSepIP, octet1, octet2);

                    SendCmd("PORT", ipParam);
                    reply = cmdCon.ReadMessage();
                    if (reply.Code != SUCCESS)
                    {
                        return reply;
                    }
                    HandleReply(reply);

                    SendCmd("RETR", fileName);
                    reply = cmdCon.ReadMessage();
                    HandleReply(reply);

                    if (reply.Code == PERMISSION_DENIED)
                    {
                        return new ServerMessage();
                    }

                    using (FileStream f = File.Create(fileName))
                    {
                        fileCon.AcceptFile(f, DatMode);
                    }
                }

                return cmdCon.ReadMessage();
            }
        }

        /// <summary>
        /// Remains a NOOP because the client is not intended to support file uploading.
        /// </summary>
        /// <param name="file">The name of the file</param>
        /// <returns>An empty ServerMessage</returns>
        private ServerMessage PutFile(String file)
        {
            return new ServerMessage();
        }

        /// <summary>
        /// Set the client to modify its actions to conform with passive mode.
        /// </summary>
        private void Passive()
        {
            CliMode = (CliMode == ClientMode.Active ? ClientMode.Passive : ClientMode.Active);
            Console.WriteLine("Passive mode {0}.", CliMode == ClientMode.Active ? "off" : "on");
        }


        /// <summary>
        /// Print the current working directory from the server.
        /// </summary>
        /// <returns>A server message in response to this action.</returns>
        private ServerMessage Pwd()
        {
            SendCmd("XPWD");
            ServerMessage reply = cmdCon.ReadMessage();
            return reply;
        }

        /// <summary>
        /// Send the server a username to log in.
        /// </summary>
        /// <param name="username">The username used to idenfity the client.</param>
        /// <returns>A server message in response to this action.</returns>
        private ServerMessage User(String username)
        {
            SendCmd("USER", username);
            ServerMessage reply = cmdCon.ReadMessage();
            return reply;
        }

        /// <summary>
        /// Pause the client to prompt for a username to log in with.
        /// </summary>
        /// <returns>A server message in response to this action.</returns>
        private ServerMessage User()
        {
            Console.Write("User ({0}): ", cmdCon.HostName);
            String username = Console.ReadLine();
            SendCmd("USER", username);
            ServerMessage reply = cmdCon.ReadMessage();
            return reply;
        }

        /// <summary>
        /// Pause the client to prompt for a password to log in with.
        /// </summary>
        /// <returns>A server message in response to this action.</returns>
        private ServerMessage Password()
        {
            Console.Write("Password: ");
            String pass = Console.ReadLine();
            SendCmd("PASS", pass);
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

        /// <summary>
        /// Print the help information associated with a given command.
        /// </summary>
        /// <param name="command">The command to get help information for.
        /// If empty, help info for all commands will be printed.
        /// </param>
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

        /// <summary>
        /// Write a line to the console based on the client's debug settings.
        /// </summary>
        /// <param name="str">The line to write</param>
        /// <param name="debug">The debug state</param>
        private void DebugLine(String str, bool debug)
        {
            if (debug)
            {
                Console.Write(str);
            }
        }
    }
}
