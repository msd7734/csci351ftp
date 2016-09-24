﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Csci351ftp
{
    /// <summary>
    /// Parse and store messages sent from the server to the client.
    /// </summary>
    public class ServerMessage
    {

        /// <summary>
        /// Pattern to find the true last line and numeric code of a server response message.
        /// </summary>
        private static String codePattern = "^\\d{3}(?=\\s)";

        public String PreText { get; private set; }

        public int Code { get; private set; }

        public String Text { get; private set; }

        public ServerMessage(byte[] b)
        {
            StringBuilder msg = new StringBuilder(b.Length);
            for (int i = 0; i < b.Length; ++i)
            {
                msg.Append(Convert.ToChar(b[i]));
            }
            _initialize(msg.ToString());
        }

        public ServerMessage(String s)
        {
            _initialize(s);
        }

        private void _initialize(String b)
        {
            Match m = Regex.Match(b, codePattern, RegexOptions.Multiline);
            if (!m.Success)
            {
                throw new ArgumentException("The given server message did not have a 3 digit message code.");
            }
            Code = Int32.Parse(m.Value);

            String[] parts = Regex.Split(b, codePattern, RegexOptions.Multiline);

            if (String.IsNullOrWhiteSpace(parts[0]))
                PreText = String.Empty;
            else
                PreText = parts[0];

            if (String.IsNullOrWhiteSpace(parts[1]))
                Text = String.Empty;
            else
                Text = parts[1];
        }

        public override string ToString()
        {
            return String.Format("{0}{1}{2}", PreText, Code, Text);
        }
    }
}