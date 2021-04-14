using System;

namespace Future.Utilities.Net.Sockets
{
    public class TcpException : Exception
    {
        public TcpException(string message)
            : base(message)
        {

        }

        public TcpException(string message, Exception inner_exception)
            : base(message, inner_exception)
        {
            
        }
    }
}
