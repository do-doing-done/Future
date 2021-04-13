using System;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Length field based frame builder class.
    /// </summary>
    public class LengthFieldBasedFrameBuilder : FrameBuilder
    {
        #region [ Contacts ]
        /// <summary>
        /// Length field Enumerate.
        /// </summary>
        public enum LengthField
        {
            OneByte    = 1,
            TwoBytes   = 2,
            FourBytes  = 4,
            EigthBytes = 8
        }
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Construct a new <see cref="LengthFieldBasedFrameBuilder"></see> class's instance.
        /// </summary>
        public LengthFieldBasedFrameBuilder()
            : this(LengthField.FourBytes)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="LengthFieldBasedFrameBuilder"></see> class's instance.
        /// </summary>
        /// <param name="length_field">Length field.</param>
        public LengthFieldBasedFrameBuilder(LengthField length_field)
            : this(new LengthFieldBasedFrameEncoder(length_field), new LengthFieldBasedFrameDecoder(length_field))
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="LengthFieldBasedFrameBuilder"></see> class's instance.
        /// </summary>
        /// <param name="encoder">Length field based frame encoder.</param>
        /// <param name="decoder">Length field based frame decoder.</param>
        private LengthFieldBasedFrameBuilder(LengthFieldBasedFrameEncoder encoder, LengthFieldBasedFrameDecoder decoder)
            : base(encoder, decoder)
        {
            // Do nothing.
        }
        #endregion

        #region [ Internal Class ]
        /// <summary>
        /// Length field based frame encoder class.
        /// </summary>
        private sealed class LengthFieldBasedFrameEncoder : IFrameEncoder
        {
            #region [ Fields ]
            /// <summary>
            /// Length field.
            /// </summary>
            private LengthField _length_field;
            #endregion

            #region [ Properties ]
            /// <summary>
            /// Length field.
            /// </summary>
            public LengthField LengthField => this._length_field;
            #endregion

            #region [ Constructor ]
            /// <summary>
            /// Construct a new <see cref="LengthFieldBasedFrameEncoder"></see> class's instance.
            /// </summary>
            /// <param name="length_field"></param>
            public LengthFieldBasedFrameEncoder(LengthField length_field)
            {
                this._length_field = length_field;
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
            /// <exception cref="ArgumentOutOfRangeException"></exception>
            /// <exception cref="NotSupportedException"></exception>
            public void EncodeFrame(byte[] payload, int offset, int count, out byte[] frame_buffer, out int frame_buffer_offset, out int frame_buffer_length)
            {
                byte[] buffer = null;

                switch (this._length_field)
                {
                    case LengthField.OneByte:
                        if (count <= byte.MaxValue)
                        {
                            buffer    = new byte[1 + count];
                            buffer[0] = (byte)count;
                            Array.Copy(payload, offset, buffer, 1, count);
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException($"{nameof(count)}");
                        }
                        break;

                    case LengthField.TwoBytes:
                        if (count <= short.MaxValue)
                        {
                            buffer    = new byte[2 + count];
                            buffer[0] = (byte)((ushort)count >> 8);
                            buffer[1] = (byte)count;
                            Array.Copy(payload, offset, buffer, 2, count);
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException($"{nameof(count)}");
                        }
                        break;

                    case LengthField.FourBytes:
                        buffer    = new byte[4 + count];
                        buffer[0] = (byte)(((uint)count) >> 24);
                        buffer[1] = (byte)(((uint)count) >> 16);
                        buffer[2] = (byte)(((uint)count) >> 8);
                        buffer[3] = (byte)((uint)count);
                        Array.Copy(payload, offset, buffer, 4, count);
                        break;

                    case LengthField.EigthBytes:
                        buffer    = new byte[8 + count];
                        buffer[0] = (byte)(((ulong)count) >> 56);
                        buffer[1] = (byte)(((ulong)count) >> 48);
                        buffer[2] = (byte)(((ulong)count) >> 40);
                        buffer[3] = (byte)(((ulong)count) >> 32);
                        buffer[4] = (byte)(((ulong)count) >> 24);
                        buffer[5] = (byte)(((ulong)count) >> 16);
                        buffer[6] = (byte)(((ulong)count) >> 8);
                        buffer[7] = (byte)((ulong)count);
                        Array.Copy(payload, offset, buffer, 8, count);
                        break;

                    default:
                        throw new NotSupportedException("Specified length field is not supported.");
                }

                frame_buffer        = buffer;
                frame_buffer_offset = 0;
                frame_buffer_length = buffer.Length;
            }
            #endregion
        }

        /// <summary>
        /// Length field based frame decoder class.
        /// </summary>
        private sealed class LengthFieldBasedFrameDecoder : IFrameDecoder
        {
            #region [ Fields ]
            /// <summary>
            /// Length field.
            /// </summary>
            private LengthField _length_field;
            #endregion

            #region [ Properties ]
            /// <summary>
            /// Length field.
            /// </summary>
            public LengthField LengthField => this._length_field;
            #endregion

            #region [ Constructor ]
            /// <summary>
            /// Construct a new <see cref="LengthFieldBasedFrameDecoder"></see> class's instance.
            /// </summary>
            /// <param name="length_field">Length field.</param>
            public LengthFieldBasedFrameDecoder(LengthField length_field)
            {
                this._length_field = length_field;
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
            /// <exception cref="NotSupportedException"></exception>
            public bool TryDecodeFrame(byte[] buffer, int offset, int count, out int frame_length, out byte[] payload, out int payload_offset, out int payload_count)
            {
                frame_length   = 0;
                payload        = null;
                payload_offset = 0;
                payload_count  = 0;

                byte[] output = null;
                long   length = 0;

                switch (this._length_field)
                {
                    case LengthField.OneByte:
                        length = buffer[offset];
                        if ((1 <= count) && (length <= (count - 1)))
                        {
                            output = new byte[length];
                            Array.Copy(buffer, offset + 1, output, 0, length);
                        }
                        else
                        {
                            return false;
                        }
                        break;

                    case LengthField.TwoBytes:
                        length = (buffer[offset + 0] << (8 * 1)) |
                                 (buffer[offset + 1] << (8 * 0));
                        if ((2 <= count) && (length <= (count - 2)))
                        {
                            output = new byte[length];
                            Array.Copy(buffer, offset + 2, output, 0, length);
                        }
                        else
                        {
                            return false;
                        }
                        break;

                    case LengthField.FourBytes:
                        length = (buffer[offset + 0] << (8 * 3)) |
                                 (buffer[offset + 1] << (8 * 2)) |
                                 (buffer[offset + 2] << (8 * 1)) |
                                 (buffer[offset + 3] << (8 * 0));
                        if ((4 <= count) && (length <= (count - 4)))
                        {
                            output = new byte[length];
                            Array.Copy(buffer, offset + 4, output, 0, length);
                        }
                        else
                        {
                            return false;
                        }
                        break;

                    case LengthField.EigthBytes:
                        length = (buffer[offset + 0] << (8 * 7)) |
                                 (buffer[offset + 1] << (8 * 6)) |
                                 (buffer[offset + 2] << (8 * 5)) |
                                 (buffer[offset + 3] << (8 * 4)) |
                                 (buffer[offset + 4] << (8 * 3)) |
                                 (buffer[offset + 5] << (8 * 2)) |
                                 (buffer[offset + 6] << (8 * 1)) |
                                 (buffer[offset + 7] << (8 * 0));
                        if ((8 <= count) && (length <= (count - 8)))
                        {
                            output = new byte[length];
                            Array.Copy(buffer, offset + 8, output, 0, length);
                        }
                        else
                        {
                            return false;
                        }
                        break;

                    default:
                        throw new NotSupportedException("Specified length field is not supported.");
                }

                payload        = output;
                payload_offset = 0;
                payload_count  = output.Length;
                frame_length   = (int)this._length_field + output.Length;

                return true;
            }
            #endregion
        }
        #endregion
    }
}
