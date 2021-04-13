using System;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Unable to allocate buffer exception.
    /// </summary>
    public class UnableToAllocateBufferException : Exception
    {
        /// <summary>
        /// Constructors.
        /// </summary>
        public UnableToAllocateBufferException()
            : base($"Cannot allocate buffer after few trials.")
        {
            /* Do nothing */
        }
    }
}
