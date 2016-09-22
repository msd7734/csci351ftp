using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Csci351ftp
{
    // for the record this is a dumb idea to make this recursion, so just allow it to take one prompt or multiple and hold all the responses(?)
    public class UserPrompt
    {
        public String Message { get; private set; }

        public UserPrompt Next { get; private set; }

        public UserPrompt(params String[] msg)
        {
            if (msg.Length == 0)
            {
                Message = String.Empty;
                Next = null;
            }
            else
            {
                Message = msg[0];
                Next = new UserPrompt(msg.SkipWhile((x, index) => index == 0).ToArray());
            }
        }
    }
}
