using System;
using System.Collections.Generic;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Segment buffer manager class's Interface.
    /// </summary>
    public interface ISegmentBufferManager
    {
        #region [ Borrow ]
        /// <summary>
        /// Borrow Buffer.
        /// </summary>
        /// <returns>
        /// Buffer
        /// </returns>
        ArraySegment<byte> BorrowBuffer();

        /// <summary>
        /// Borrow Buffers.
        /// </summary>
        /// <param name="count">Buffer's count</param>
        /// <returns>
        /// Buffers
        /// </returns>
        IEnumerable<ArraySegment<byte>> BorrowBuffers(int count);
        #endregion

        #region [ Return ]
        /// <summary>
        /// Return Buffer.
        /// </summary>
        /// <param name="buffer">
        /// 
        /// </param>
        void ReturnBuffer(ArraySegment<byte> buffer);

        /// <summary>
        /// Return Buffers.
        /// </summary>
        /// <param name="buffers">
        /// 
        /// </param>
        void ReturnBuffers(IEnumerable<ArraySegment<byte>> buffers);

        /// <summary>
        /// Return Buffers.
        /// </summary>
        /// <param name="buffers">
        /// 
        /// </param>
        void ReturnBuffers(params ArraySegment<byte>[] buffers);
        #endregion
    }
}
