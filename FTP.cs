﻿using System;
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
                Console.ReadKey(true);
                return;
            }

            FTPClient client;
            try
            {
                client = new FTPClient(args[0]);
            }
            catch (ArgumentException ae)
            {
                Console.Error.WriteLine(ae.Message, ae.StackTrace);
                Console.ReadKey(true);
                return;
            }
            
            String input = String.Empty;
            
            do
            {
                Console.Write(PROMPT);
                input = Console.ReadLine();
                String[] tokens = input.Split(' ');
                String cmd = tokens[0];
                String[] cmdargs = GetTail<String>(tokens).ToArray<String>();

                if (cmd.Length > 0) {
                    client.Exec(cmd, cmdargs);
                }

            } while (client.IsOpen);

            Console.ReadKey(true);
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
