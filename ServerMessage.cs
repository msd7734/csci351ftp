/*
 * By: Matthew Dennis (msd7734)
 */

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
        
        /// <summary>
        /// The text held at the head of a message, before the final line.
        /// </summary>
        public String PreText { get; private set; }

        /// <summary>
        /// The 3 digit code that represents the type of server message.
        /// </summary>
        public int Code { get; private set; }

        /// <summary>
        /// The text found after the 3 digit code on the last line.
        /// Split up this way since this text is sometimes needed while any "PreText"
        ///     can generally be discarded.
        /// </summary>
        public String Text { get; private set; }

        /// <summary>
        /// Create an empty/dummy server message.
        /// </summary>
        public ServerMessage()
        {
            PreText = String.Empty;
            Code = -1;
            Text = String.Empty;
        }

        /// <summary>
        /// Create a server message from the given raw bytes.
        /// </summary>
        /// <param name="b">The bytes to interpret</param>
        public ServerMessage(byte[] b)
        {
            StringBuilder msg = new StringBuilder(b.Length);
            for (int i = 0; i < b.Length; ++i)
            {
                msg.Append(Convert.ToChar(b[i]));
            }
            _initialize(msg.ToString());
        }

        /// <summary>
        /// Create a server message from the given string.
        /// </summary>
        /// <param name="s">String to parse</param>
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

        /// <summary>
        /// Parse the given initialization data and populate the members.
        /// </summary>
        /// <param name="b"></param>
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

        /// <summary>
        /// Whether the message can be considered empty.
        /// </summary>
        /// <returns>True if there is no pretext, no main message text, and an invalid message code.</returns>
        public bool IsEmpty()
        {
            return String.IsNullOrWhiteSpace(PreText) &&
                String.IsNullOrWhiteSpace(Text) &&
                Code < 100;
        }

        /// <summary>
        /// Get the original message from the server as received.
        /// </summary>
        /// <returns>A potentially multiline string that was received from the server.</returns>
        public override string ToString()
        {
            if (!IsEmpty())
                return String.Format("{0}{1}{2}", PreText, Code, Text);
            else
                return String.Empty;
        }
    }
}
