using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Csci351ftp
{
    

    class FTP
    {
        // The prompt
        public const string PROMPT = "FTP> ";

        // Help message

        public static readonly String[] HELP_MESSAGE = {
	"ascii      --> Set ASCII transfer type",
	"binary     --> Set binary transfer type",
	"cd <path>  --> Change the remote working directory",
	"cdup       --> Change the remote working directory to the",
        "               parent directory (i.e., cd ..)",
	"debug      --> Toggle debug mode",
	"dir        --> List the contents of the remote directory",
	"get path   --> Get a remote file",
	"help       --> Displays this text",
	"passive    --> Toggle passive/active mode",
    "put path   --> Transfer the specified file to the server",
	"pwd        --> Print the working directory on the server",
    "quit       --> Close the connection to the server and terminate",
	"user login --> Specify the user name (will prompt for password" };

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: [mono] Ftp server");
                Environment.Exit(1);
            }

            FTPClient client = new FTPClient(args[0]);
            String input = String.Empty;
            
            do
            {
                input = Console.ReadLine();
                Console.WriteLine(input);
                String[] tokens = input.Split(' ');
                String cmd = tokens[0];
                String[] cmdargs = GetTail<String>(tokens).ToArray<String>();
                Console.WriteLine(tokens[0]);
                Console.WriteLine(String.Join(",", cmdargs));
                Console.WriteLine(client.Exec(cmd, cmdargs));

            } while (client.Open);
        }

        static IList<T> GetTail<T>(IList<T> arr)
        {
            if (arr.Count < 2)
                return new List<T>();
            else {
                arr.RemoveAt(0);
                return arr;
            }
        }
    }
}
