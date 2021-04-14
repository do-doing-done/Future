using System;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Buffer Validator.
    /// </summary>
    public static class BufferValidator
    {
        #region [ Buffer ]
        /// <summary>
        /// Validate Buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="buffer_name"><see cref="buffer"/> parameter name</param>
        /// <param name="offset_name"><see cref="offset"/> parameter name</param>
        /// <param name="count_name"><see cref="count"/> parameter name</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void ValidateBuffer(byte[] buffer, int offset, int count, string buffer_name = null, string offset_name = null, string count_name = null)
        {
            /* buffer */
            if (null == buffer)
            {
                throw new ArgumentNullException(!string.IsNullOrWhiteSpace(buffer_name) ? buffer_name : nameof(buffer));
            }
            /* offset */
            if ((offset < 0) || (buffer.Length < offset))
            {
                throw new ArgumentOutOfRangeException(!string.IsNullOrWhiteSpace(offset_name) ? offset_name : nameof(offset));
            }
            /* count */
            if (count < 0 || count > (buffer.Length - offset))
            {
                throw new ArgumentOutOfRangeException(!string.IsNullOrWhiteSpace(count_name) ? count_name : nameof(count));
            }
        }
        #endregion

        #region [ Buffers ]
        /// <summary>
        /// Validate Array Segment.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array_segment"></param>
        /// <param name="array_segment_name"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void ValidateArraySegment<T>(ArraySegment<T> array_segment, string array_segment_name = null)
        {
            /* array */
            if (null == array_segment.Array)
            {
                throw new ArgumentNullException((!string.IsNullOrWhiteSpace(array_segment_name) ? array_segment_name : nameof(array_segment)) + ".Array");
            }
            /* offset */
            if (array_segment.Offset < 0 || array_segment.Offset > array_segment.Array.Length)
            {
                throw new ArgumentOutOfRangeException((!string.IsNullOrWhiteSpace(array_segment_name) ? array_segment_name : nameof(array_segment)) + ".Offset");
            }
            /* count */
            if (array_segment.Count < 0 || array_segment.Count > (array_segment.Array.Length - array_segment.Offset))
            {
                throw new ArgumentOutOfRangeException((!string.IsNullOrWhiteSpace(array_segment_name) ? array_segment_name : nameof(array_segment)) + ".Count");
            }
        }
        #endregion
    }
}
