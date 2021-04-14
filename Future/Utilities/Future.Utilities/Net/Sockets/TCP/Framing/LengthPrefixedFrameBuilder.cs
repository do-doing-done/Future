using System;
using System.Collections.Generic;
using System.Text;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Length prefixed frame builder class.
    /// </summary>
    public class LengthPrefixedFrameBuilder : FrameBuilder
    {
        #region [ Constructor ]
        /// <summary>
        /// Construct a new <see cref="LengthPrefixedFrameBuilder"></see> class's instance.
        /// </summary>
        /// <param name="is_masked"></param>
        public LengthPrefixedFrameBuilder(bool is_masked = false)
            : this(new LengthPrefixedFrameEncoder(is_masked), new LengthPrefixedFrameDecoder(is_masked))
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="LengthPrefixedFrameBuilder"></see> class's instance.
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="decoder"></param>
        private LengthPrefixedFrameBuilder(LengthPrefixedFrameEncoder encoder, LengthPrefixedFrameDecoder decoder)
            : base(encoder, decoder)
        {
            // Do nothing.
        }
        #endregion

        #region [ Internal Class ]
        /// <summary>
        /// Length prefixed frame encoder class.
        /// </summary>
        private sealed class LengthPrefixedFrameEncoder : IFrameEncoder
        {
            #region [ Constants ]
            /// <summary>
            /// Masking key length.
            /// </summary>
            private static readonly int MASKING_KEY_LENGTH = 4;
            #endregion

            #region [ Fields ]
            /// <summary>
            /// Is masked?
            /// </summary>
            private bool _is_masked;

            /// <summary>
            /// Random.
            /// </summary>
            private static readonly Random _random = new Random(DateTime.UtcNow.Millisecond);
            #endregion

            #region [ Properties ]
            /// <summary>
            /// Is masked?
            /// </summary>
            public bool IsMasked => _is_masked;
            #endregion

            #region [ Constructor ]
            /// <summary>
            /// Construct a new <see cref="LengthPrefixedFrameEncoder"></see> class's instance.
            /// </summary>
            /// <param name="is_masked">Is masked?</param>
            public LengthPrefixedFrameEncoder(bool is_masked = false)
            {
                this._is_masked = is_masked;
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
                frame_buffer        = Encode(payload, offset, count, this._is_masked);
                frame_buffer_offset = 0;
                frame_buffer_length = frame_buffer.Length;
            }

            /// <summary>
            /// Encode.
            /// </summary>
            /// <param name="payload"></param>
            /// <param name="offset"></param>
            /// <param name="count"></param>
            /// <param name="is_masked"></param>
            /// <returns></returns>
            private static byte[] Encode(byte[] payload, int offset, int count, bool is_masked = false)
            {
                byte[] fragment;

                // Payload length:  7 bits, 7+16 bits, or 7+64 bits.
                // The length of the "Payload data", in bytes: 
                // if 0-125, that is the payload length.  
                // If 126, the following 2 bytes interpreted as a 16-bit unsigned integer are the payload length.  
                // If 127, the following 8 bytes interpreted as a 64-bit unsigned integer are the payload length.
                if (count < 126)
                {
                    fragment    = new byte[1 + (is_masked ? MASKING_KEY_LENGTH : 0) + count];
                    fragment[0] = (byte)count;
                }
                else if (count < 65536)
                {
                    fragment    = new byte[1 + 2 + (is_masked ? MASKING_KEY_LENGTH : 0) + count];
                    fragment[0] = (byte)126;
                    fragment[1] = (byte)(count / 256);
                    fragment[2] = (byte)(count % 256);
                }
                else
                {
                    fragment = new byte[1 + 8 + (is_masked ? MASKING_KEY_LENGTH : 0) + count];
                    fragment[0] = (byte)127;

                    int left = count;
                    for (int i = 8; i > 0; i--)
                    {
                        fragment[i] = (byte)(left % 256);
                        left /= 256;

                        if (left == 0)
                            break;
                    }
                }

                // Mask:  1 bit
                // Defines whether the "Payload data" is masked.
                if (is_masked)
                    fragment[0] = (byte)(fragment[0] | 0x80);

                // Masking-key:  0 or 4 bytes
                // The masking key is a 32-bit value chosen at random by the client.
                if (is_masked)
                {
                    int maskingKeyIndex = fragment.Length - (MASKING_KEY_LENGTH + count);
                    for (var i = maskingKeyIndex; i < maskingKeyIndex + MASKING_KEY_LENGTH; i++)
                    {
                        fragment[i] = (byte)_random.Next(0, 255);
                    }
                    if (count > 0)
                    {
                        int payloadIndex = fragment.Length - count;
                        for (var i = 0; i < count; i++)
                        {
                            fragment[payloadIndex + i] = (byte)(payload[offset + i] ^ fragment[maskingKeyIndex + i % MASKING_KEY_LENGTH]);
                        }
                    }
                }
                else
                {
                    if (count > 0)
                    {
                        int payloadIndex = fragment.Length - count;
                        Array.Copy(payload, offset, fragment, payloadIndex, count);
                    }
                }

                return fragment;
            }
            #endregion
        }

        /// <summary>
        /// Length prefixed frame decoder class.
        /// </summary>
        private sealed class LengthPrefixedFrameDecoder : IFrameDecoder
        {
            private static readonly int MaskingKeyLength = 4;

            public LengthPrefixedFrameDecoder(bool isMasked = false)
            {
                IsMasked = isMasked;
            }

            public bool IsMasked { get; private set; }

            public bool TryDecodeFrame(byte[] buffer, int offset, int count, out int frameLength, out byte[] payload, out int payloadOffset, out int payloadCount)
            {
                frameLength = 0;
                payload = null;
                payloadOffset = 0;
                payloadCount = 0;

                var frameHeader = DecodeHeader(buffer, offset, count);
                if (frameHeader != null && frameHeader.Length + frameHeader.PayloadLength <= count)
                {
                    if (IsMasked)
                    {
                        payload = DecodeMaskedPayload(buffer, offset, frameHeader.MaskingKeyOffset, frameHeader.Length, frameHeader.PayloadLength);
                        payloadOffset = 0;
                        payloadCount = payload.Length;
                    }
                    else
                    {
                        payload = buffer;
                        payloadOffset = offset + frameHeader.Length;
                        payloadCount = frameHeader.PayloadLength;
                    }

                    frameLength = frameHeader.Length + frameHeader.PayloadLength;

                    return true;
                }

                return false;
            }

            internal sealed class Header
            {
                public bool IsMasked { get; set; }
                public int PayloadLength { get; set; }
                public int MaskingKeyOffset { get; set; }
                public int Length { get; set; }

                public override string ToString()
                {
                    return string.Format("IsMasked[{0}], PayloadLength[{1}], MaskingKeyOffset[{2}], Length[{3}]",
                        IsMasked, PayloadLength, MaskingKeyOffset, Length);
                }
            }

            private static Header DecodeHeader(byte[] buffer, int offset, int count)
            {
                if (count < 1)
                    return null;

                // parse fixed header
                var header = new Header()
                {
                    IsMasked = ((buffer[offset + 0] & 0x80) == 0x80),
                    PayloadLength = (buffer[offset + 0] & 0x7f),
                    Length = 1,
                };

                // parse extended payload length
                if (header.PayloadLength >= 126)
                {
                    if (header.PayloadLength == 126)
                        header.Length += 2;
                    else
                        header.Length += 8;

                    if (count < header.Length)
                        return null;

                    if (header.PayloadLength == 126)
                    {
                        header.PayloadLength = buffer[offset + 1] * 256 + buffer[offset + 2];
                    }
                    else
                    {
                        int totalLength = 0;
                        int level = 1;

                        for (int i = 7; i >= 0; i--)
                        {
                            totalLength += buffer[offset + i + 1] * level;
                            level *= 256;
                        }

                        header.PayloadLength = totalLength;
                    }
                }

                // parse masking key
                if (header.IsMasked)
                {
                    if (count < header.Length + MaskingKeyLength)
                        return null;

                    header.MaskingKeyOffset = header.Length;
                    header.Length += MaskingKeyLength;
                }

                return header;
            }

            private static byte[] DecodeMaskedPayload(byte[] buffer, int offset, int maskingKeyOffset, int payloadOffset, int payloadCount)
            {
                var payload = new byte[payloadCount];

                for (var i = 0; i < payloadCount; i++)
                {
                    payload[i] = (byte)(buffer[offset + payloadOffset + i] ^ buffer[offset + maskingKeyOffset + i % MaskingKeyLength]);
                }

                return payload;
            }
        }
        #endregion
    }
}
