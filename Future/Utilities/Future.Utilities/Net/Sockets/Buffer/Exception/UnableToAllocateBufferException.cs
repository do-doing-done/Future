using System;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Unable to allocate buffer exception class.
    /// </summary>
    public class UnableToAllocateBufferException : Exception
    {
        #region [ Constants ]
        /// <summary>
        /// Exception message.
        /// </summary>
        private const string EXCEPTION_MESSAGE = "Cannot allocate buffer after few trials.";
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Construct a new <see cref="UnableToAllocateBufferException"></see> class's instance.
        /// </summary>
        public UnableToAllocateBufferException()
            : base(EXCEPTION_MESSAGE)
        {
            /* Do nothing */
        }
        #endregion
    }
}
