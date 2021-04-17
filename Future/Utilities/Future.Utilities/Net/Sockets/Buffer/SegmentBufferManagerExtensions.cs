using System;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Segment buffer manager extensions static class.
    /// </summary>
    public static class SegmentBufferManagerExtensions
    {
        #region [ Append ]
        /// <summary>
        /// Append buffer to <see cref="ISegmentBufferManager" />.
        /// </summary>
        /// <param name="buffer_manager">Segment buffer manager.</param>
        /// <param name="receive_buffer"></param>
        /// <param name="receive_count"></param>
        /// <param name="session_buffer"></param>
        /// <param name="session_buffer_count"></param>
        public static void AppendBuffer(this ISegmentBufferManager buffer_manager,
                                        ref  ArraySegment<byte>    receive_buffer,
                                             int                   receive_count,
                                        ref  ArraySegment<byte>    session_buffer,
                                        ref  int                   session_buffer_count)
        {
            if (session_buffer.Count < (session_buffer_count + receive_count))
            {
                ArraySegment<byte> buffer = buffer_manager.BorrowBuffer();

                if (buffer.Count < (session_buffer_count + receive_count) * 2)
                {
                    buffer_manager.ReturnBuffer(buffer);
                    buffer = new ArraySegment<byte>(new byte[(session_buffer_count + receive_count) * 2]);
                }

                Array.Copy(session_buffer.Array, session_buffer.Offset, buffer.Array, buffer.Offset, session_buffer_count);

                buffer_manager.ReturnBuffer(session_buffer);
                session_buffer = buffer;
            }

            Array.Copy(receive_buffer.Array, receive_buffer.Offset, session_buffer.Array, session_buffer.Offset + session_buffer_count, receive_count);
            session_buffer_count += receive_count;
        }
        #endregion

        #region [ Shift ]
        /// <summary>
        /// Shift buffer from <see cref="ISegmentBufferManager" />.
        /// </summary>
        /// <param name="buffer_manager">Segment buffer manager.</param>
        /// <param name="shift_start"></param>
        /// <param name="session_buffer"></param>
        /// <param name="session_buffer_count"></param>
        public static void ShiftBuffer(this ISegmentBufferManager buffer_manager,
                                            int                   shift_start,
                                       ref  ArraySegment<byte>    session_buffer,
                                       ref  int                   session_buffer_count)
        {
            if ((session_buffer_count - shift_start) < shift_start)
            {
                Array.Copy(session_buffer.Array, session_buffer.Offset + shift_start, session_buffer.Array, session_buffer.Offset, session_buffer_count - shift_start);
                session_buffer_count -= shift_start;
            }
            else
            {
                ArraySegment<byte> copy = buffer_manager.BorrowBuffer();
                if (copy.Count < (session_buffer_count - shift_start))
                {
                    buffer_manager.ReturnBuffer(copy);
                    copy = new ArraySegment<byte>(new byte[session_buffer_count - shift_start]);
                }

                Array.Copy(session_buffer.Array, session_buffer.Offset + shift_start, copy.Array, copy.Offset, session_buffer_count - shift_start);
                Array.Copy(copy.Array, copy.Offset, session_buffer.Array, session_buffer.Offset, session_buffer_count - shift_start);
                session_buffer_count -= shift_start;

                buffer_manager.ReturnBuffer(copy);
            }
        }
        #endregion

        #region [ Replace ]
        /// <summary>
        /// Replace buffer from <see cref="ISegmentBufferManager" />.
        /// </summary>
        /// <param name="buffer_manager">Segment buffer manager.</param>
        /// <param name="receive_buffer"></param>
        /// <param name="receive_buffer_offset"></param>
        /// <param name="receive_count"></param>
        public static void ReplaceBuffer(this ISegmentBufferManager buffer_manager,
                                         ref  ArraySegment<byte>    receive_buffer,
                                         ref  int                   receive_buffer_offset,
                                              int                   receive_count)
        {
            if ((receive_buffer_offset + receive_count) < receive_buffer.Count)
            {
                receive_buffer_offset += receive_count;
            }
            else
            {
                ArraySegment<byte> autoExpandedBuffer = buffer_manager.BorrowBuffer();
                if (autoExpandedBuffer.Count < (receive_buffer_offset + receive_count) * 2)
                {
                    buffer_manager.ReturnBuffer(autoExpandedBuffer);
                    autoExpandedBuffer = new ArraySegment<byte>(new byte[(receive_buffer_offset + receive_count) * 2]);
                }

                Array.Copy(receive_buffer.Array, receive_buffer.Offset, autoExpandedBuffer.Array, autoExpandedBuffer.Offset, receive_buffer_offset + receive_count);
                receive_buffer_offset += receive_count;

                buffer_manager.ReturnBuffer(receive_buffer);
                receive_buffer = autoExpandedBuffer;
            }
        }
        #endregion
    }
}
