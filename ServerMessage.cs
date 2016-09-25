using System;
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

        /// <summary>
        /// Indicates whether the given line is a terminal line based on a ServerMessage's criteria.
        /// </summary>
        /// <param name="line">The line to parse.</param>
        /// <returns>Whether the line is the end of a message on the server command connection.</returns>
        public static bool IsLastMessageLine(String line)
        {
            Match m = Regex.Match(line, codePattern);
            return m.Success;
        }

        public String PreText { get; private set; }

        public int Code { get; private set; }

        public String Text { get; private set; }

        public ServerMessage()
        {
            PreText = String.Empty;
            Code = -1;
            Text = String.Empty;
        }

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
            if (s.Length < 3)
            {
                PreText = String.Empty;
                Code = -1;
                Text = String.Empty;
            }
            else
            {
                _initialize(s);
            }
            
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

        public bool IsEmpty()
        {
            return String.IsNullOrWhiteSpace(PreText) &&
                String.IsNullOrWhiteSpace(Text) &&
                Code < 100;
        }

        public override string ToString()
        {
            if (!IsEmpty())
                return String.Format("{0}{1}{2}", PreText, Code, Text);
            else
                return String.Empty;
        }
    }
}
