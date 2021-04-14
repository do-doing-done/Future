using System;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Segment Buffer Deflector.
    /// </summary>
    public class SegmentBufferDeflector
    {
        /// <summary>
        /// Append buffer to <see cref="ISegmentBufferManager" />
        /// </summary>
        /// <param name="buffer_manager"></param>
        /// <param name="receive_buffer"></param>
        /// <param name="receive_count"></param>
        /// <param name="session_buffer"></param>
        /// <param name="session_buffer_count"></param>
        public static void AppendBuffer(
                ISegmentBufferManager buffer_manager,
            ref ArraySegment<byte>    receive_buffer,
            int                       receive_count,
            ref ArraySegment<byte>    session_buffer,
            ref int                   session_buffer_count)
        {
            if (session_buffer.Count < (session_buffer_count + receive_count))
            {
                ArraySegment<byte> autoExpandedBuffer = buffer_manager.BorrowBuffer();
                if (autoExpandedBuffer.Count < (session_buffer_count + receive_count) * 2)
                {
                    buffer_manager.ReturnBuffer(autoExpandedBuffer);
                    autoExpandedBuffer = new ArraySegment<byte>(new byte[(session_buffer_count + receive_count) * 2]);
                }

                Array.Copy(session_buffer.Array, session_buffer.Offset, autoExpandedBuffer.Array, autoExpandedBuffer.Offset, session_buffer_count);

                var discardBuffer = session_buffer;
                session_buffer = autoExpandedBuffer;
                buffer_manager.ReturnBuffer(discardBuffer);
            }

            Array.Copy(receive_buffer.Array, receive_buffer.Offset, session_buffer.Array, session_buffer.Offset + session_buffer_count, receive_count);
            session_buffer_count += receive_count;
        }

        /// <summary>
        /// Shift buffer from <see cref="ISegmentBufferManager" />
        /// </summary>
        /// <param name="buffer_manager"></param>
        /// <param name="shift_start"></param>
        /// <param name="session_buffer"></param>
        /// <param name="session_buffer_count"></param>
        public static void ShiftBuffer(
                ISegmentBufferManager buffer_manager,
                int                   shift_start,
            ref ArraySegment<byte>    session_buffer,
            ref int                   session_buffer_count)
        {
            if ((session_buffer_count - shift_start) < shift_start)
            {
                Array.Copy(session_buffer.Array, session_buffer.Offset + shift_start, session_buffer.Array, session_buffer.Offset, session_buffer_count - shift_start);
                session_buffer_count = session_buffer_count - shift_start;
            }
            else
            {
                ArraySegment<byte> copyBuffer = buffer_manager.BorrowBuffer();
                if (copyBuffer.Count < (session_buffer_count - shift_start))
                {
                    buffer_manager.ReturnBuffer(copyBuffer);
                    copyBuffer = new ArraySegment<byte>(new byte[session_buffer_count - shift_start]);
                }

                Array.Copy(session_buffer.Array, session_buffer.Offset + shift_start, copyBuffer.Array, copyBuffer.Offset, session_buffer_count - shift_start);
                Array.Copy(copyBuffer.Array, copyBuffer.Offset, session_buffer.Array, session_buffer.Offset, session_buffer_count - shift_start);
                session_buffer_count = session_buffer_count - shift_start;

                buffer_manager.ReturnBuffer(copyBuffer);
            }
        }

        /// <summary>
        /// Replace buffer from <see cref="ISegmentBufferManager" />
        /// </summary>
        /// <param name="buffer_manager"></param>
        /// <param name="receive_buffer"></param>
        /// <param name="receive_buffer_offset"></param>
        /// <param name="receive_count"></param>
        public static void ReplaceBuffer(
                ISegmentBufferManager buffer_manager,
            ref ArraySegment<byte>    receive_buffer,
            ref int                   receive_buffer_offset,
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
                receive_buffer_offset = receive_buffer_offset + receive_count;

                var discardBuffer = receive_buffer;
                receive_buffer = autoExpandedBuffer;
                buffer_manager.ReturnBuffer(discardBuffer);
            }
        }
    }
}
