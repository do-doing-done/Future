using System;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Segment buffer manager class.
    /// </summary>
    public class FrameBuilder : IFrameBuilder
    {
        #region [ Properties ]
        /// <summary>
        /// Encoder.
        /// </summary>
        public IFrameEncoder Encoder { get; private set; }
        /// <summary>
        /// Decoder.
        /// </summary>
        public IFrameDecoder Decoder { get; private set; }
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Construct a new <see cref="FrameBuilder"></see> class's instance.
        /// </summary>
        /// <param name="encoder">Frame encoder.</param>
        /// <param name="decoder">Frame decoder.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public FrameBuilder(IFrameEncoder encoder, IFrameDecoder decoder)
        {
            this.Encoder = encoder ?? throw new ArgumentNullException($"{nameof(encoder)}");
            this.Decoder = decoder ?? throw new ArgumentNullException($"{nameof(decoder)}");
        }
        #endregion
    }
}
