using System;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Fixed length frame builder class.
    /// </summary>
    public class FixedLengthFrameBuilder : FrameBuilder
    {
        #region [ Constructor ]
        /// <summary>
        /// Construct a new <see cref="FixedLengthFrameBuilder"></see> class's instance.
        /// </summary>
        /// <param name="fixed_frame_length">Fixed frame length.</param>
        public FixedLengthFrameBuilder(int fixed_frame_length)
            : this(new FixedLengthFrameEncoder(fixed_frame_length), new FixedLengthFrameDecoder(fixed_frame_length))
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="FixedLengthFrameBuilder"></see> class's instance.
        /// </summary>
        /// <param name="encoder">Fixed length frame encoder.</param>
        /// <param name="decoder">Fixed length frame decoder.</param>
        private FixedLengthFrameBuilder(FixedLengthFrameEncoder encoder, FixedLengthFrameDecoder decoder)
            : base(encoder, decoder)
        {
            // Do nothing.
        }
        #endregion

        #region [ Internal Class ]
        /// <summary>
        /// Fixed length frame encoder class.
        /// </summary>
        private sealed class FixedLengthFrameEncoder : IFrameEncoder
        {
            #region [ Fields ]
            /// <summary>
            /// Fixed frame length.
            /// </summary>
            private readonly int _fixed_frame_length;
            #endregion

            #region [ Properties ]
            /// <summary>
            /// Fixed frame length.
            /// </summary>
            public int FixedFrameLength => this._fixed_frame_length;
            #endregion

            #region [ Constructor ]
            /// <summary>
            /// Construct a new <see cref="FixedLengthFrameEncoder"></see> class's instance.
            /// </summary>
            /// <param name="fixed_frame_length">Fixed frame length.</param>
            /// <exception cref="ArgumentOutOfRangeException"></exception>
            public FixedLengthFrameEncoder(int fixed_frame_length)
            {
                if (fixed_frame_length <= 0) throw new ArgumentOutOfRangeException($"{nameof(fixed_frame_length)}");

                this._fixed_frame_length = fixed_frame_length;
            }
            #endregion

            #region [ Encode ]
            /// <summary>
            /// 
            /// </summary>
            /// <param name="payload"></param>
            /// <param name="offset"></param>
            /// <param name="count"></param>
            /// <param name="frame_buffer"></param>
            /// <param name="frame_buffer_offset"></param>
            /// <param name="frame_buffer_length"></param>
            public void EncodeFrame(byte[] payload, int offset, int count, out byte[] frame_buffer, out int frame_buffer_offset, out int frame_buffer_length)
            {
                if (count == this._fixed_frame_length)
                {
                    frame_buffer        = payload;
                    frame_buffer_offset = offset;
                    frame_buffer_length = count;
                }
                else
                {
                    byte[] buffer = new byte[this._fixed_frame_length];
                    if (count >= this._fixed_frame_length)
                    {
                        Array.Copy(payload, offset, buffer, 0, this._fixed_frame_length);
                    }
                    else
                    {
                        Array.Copy(payload, offset, buffer, 0, count);
                        for (int index = 0; index < this._fixed_frame_length - count; index++)
                        {
                            buffer[count + index] = (byte)'\n';
                        }
                    }

                    frame_buffer        = buffer;
                    frame_buffer_offset = 0;
                    frame_buffer_length = buffer.Length;
                }
            }
            #endregion
        }

        /// <summary>
        /// Fixed length frame decoder class.
        /// </summary>
        private sealed class FixedLengthFrameDecoder : IFrameDecoder
        {
            #region [ Fields ]
            /// <summary>
            /// Fixed frame length.
            /// </summary>
            private readonly int _fixed_frame_length;
            #endregion

            #region [ Properties ]
            /// <summary>
            /// Fixed frame length.
            /// </summary>
            public int FixedFrameLength => this._fixed_frame_length;
            #endregion

            #region [ Constructor ]
            /// <summary>
            /// Construct a new <see cref="FixedLengthFrameDecoder"></see> class's instance.
            /// </summary>
            /// <param name="fixed_frame_length">Fixed frame length.</param>
            /// <exception cref="ArgumentOutOfRangeException"></exception>
            public FixedLengthFrameDecoder(int fixed_frame_length)
            {
                if (fixed_frame_length <= 0) throw new ArgumentOutOfRangeException($"{nameof(fixed_frame_length)}");

                this._fixed_frame_length = fixed_frame_length;
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

                if (count >= this._fixed_frame_length)
                {
                    frame_length   = this._fixed_frame_length;
                    payload        = buffer;
                    payload_offset = offset;
                    payload_count  = this._fixed_frame_length;

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
