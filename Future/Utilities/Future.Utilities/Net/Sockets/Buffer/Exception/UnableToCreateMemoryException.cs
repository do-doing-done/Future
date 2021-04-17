using System;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Unable to create memory exception class.
    /// </summary>
    class UnableToCreateMemoryException : Exception
    {
        #region [ Constants ]
        /// <summary>
        /// Exception message.
        /// </summary>
        private const string EXCEPTION_MESSAGE = "All buffers were in use and acquiring more memory has been disabled.";
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Construct a new <see cref="UnableToCreateMemoryException"></see> class's instance.
        /// </summary>
        public UnableToCreateMemoryException()
            : base(EXCEPTION_MESSAGE)
        {
            /* Do nothing */
        }
        #endregion
    }
}
