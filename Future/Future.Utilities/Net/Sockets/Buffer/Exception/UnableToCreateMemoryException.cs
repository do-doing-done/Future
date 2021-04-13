using System;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Unable to create memory exception.
    /// </summary>
    class UnableToCreateMemoryException : Exception
    {
        /// <summary>
        /// Constructors
        /// </summary>
        public UnableToCreateMemoryException()
            : base($"All buffers were in use and acquiring more memory has been disabled.")
        {
            /* Do nothing */
        }
    }
}
