namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Raw buffer frame builder class.
    /// </summary>
    public sealed class RawBufferFrameBuilder : FrameBuilder
    {
        #region [ Constructor ]
        /// <summary>
        /// Construct a new <see cref="RawBufferFrameBuilder"></see> class's instance.
        /// </summary>
        public RawBufferFrameBuilder()
            : this(new RawBufferFrameEncoder(), new RawBufferFrameDecoder())
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="RawBufferFrameBuilder"></see> class's instance.
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="decoder"></param>
        private RawBufferFrameBuilder(RawBufferFrameEncoder encoder, RawBufferFrameDecoder decoder)
            : base(encoder, decoder)
        {
            // Do nothing.
        }
        #endregion

        #region [ Internal Class ]
        /// <summary>
        /// Raw buffer frame encoder object.
        /// </summary>
        public sealed class RawBufferFrameEncoder : IFrameEncoder
        {
            #region [ Constructor ]
            /// <summary>
            /// Construct a new <see cref="RawBufferFrameEncoder"></see> class's instance.
            /// </summary>
            public RawBufferFrameEncoder()
            {
                // Do nothing.
            }
            #endregion

            #region [ Encode ]
            /// <summary>
            /// Encode frame.
            /// </summary>
            /// <param name="payload"></param>
            /// <param name="offset"></param>
            /// <param name="count"></param>
            /// <param name="frame_buffer"></param>
            /// <param name="frame_buffer_offset"></param>
            /// <param name="frame_buffer_length"></param>
            public void EncodeFrame(byte[] payload, int offset, int count, out byte[] frame_buffer, out int frame_buffer_offset, out int frame_buffer_length)
            {
                frame_buffer        = payload;
                frame_buffer_offset = offset;
                frame_buffer_length = count;
            }
            #endregion
        }

        /// <summary>
        /// Raw buffer frame decoder object.
        /// </summary>
        public sealed class RawBufferFrameDecoder : IFrameDecoder
        {
            #region [ Constructor ]
            /// <summary>
            /// Construct a new <see cref="RawBufferFrameDecoder"></see> class's instance.
            /// </summary>
            public RawBufferFrameDecoder()
            {
                // Do nothing.
            }
            #endregion

            #region [ Decode ]
            /// <summary>
            /// Try decode frame.
            /// </summary>
            /// <param name="buffer"></param>
            /// <param name="offset"></param>
            /// <param name="count"></param>
            /// <param name="frame_length"></param>
            /// <param name="payload"></param>
            /// <param name="payload_offset"></param>
            /// <param name="payload_count"></param>
            /// <returns></returns>
            public bool TryDecodeFrame(byte[] buffer, int offset, int count, out int frame_length, out byte[] payload, out int payload_offset, out int payload_count)
            {
                frame_length   = 0;
                payload        = null;
                payload_offset = 0;
                payload_count  = 0;

                if (count > 0)
                {
                    frame_length   = count;
                    payload        = buffer;
                    payload_offset = offset;
                    payload_count  = count;

                    return true;
                }
                else
                {
                    return false;
                }
            }
            #endregion
        }
        #endregion
    }
}
