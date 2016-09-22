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
                client.Exec(cmd, cmdargs);

            } while (client.IsOpen);
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
