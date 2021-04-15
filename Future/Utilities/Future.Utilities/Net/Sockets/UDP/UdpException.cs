using System;

namespace Future.Utilities.Net.Sockets
{
    public class UdpException : Exception
    {
        public UdpException(string message)
            : base(message)
        {
            // Do nothing.
        }

        public UdpException(string message, Exception inner_exception)
            : base(message, inner_exception)
        {
            // Do nothing.
        }
    }
}
