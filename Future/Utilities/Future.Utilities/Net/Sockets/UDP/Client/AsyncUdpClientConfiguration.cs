using System;
using System.Net.Sockets;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Async udp client configuration class.
    /// </summary>
    public class AsyncUdpClientConfiguration
    {
        #region [ Properties ]
        public ISegmentBufferManager BufferManager { get; set; }

        public int ReceiveBufferSize { get; set; }
        public int SendBufferSize { get; set; }
        public TimeSpan ReceiveTimeout { get; set; }
        public TimeSpan SendTimeout { get; set; }
        public bool KeepAlive { get; set; }
        public TimeSpan KeepAliveInterval { get; set; }
        public bool ReuseAddress { get; set; }

        public bool EnableBroadcast { get; set; }
        public bool DontFragment { get; set; }

        public TimeSpan ConnectTimeout { get; set; }
        public IFrameBuilder FrameBuilder { get; set; }
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Construct a new <see cref="AsyncUdpClientConfiguration"></see> class's instance.
        /// </summary>
        public AsyncUdpClientConfiguration()
            : this(new SegmentBufferManager(1024, 8192, 1, true))
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncUdpClientConfiguration"></see> class's instance.
        /// </summary>
        /// <param name="buffer_manager">Segment buffer manager.</param>
        public AsyncUdpClientConfiguration(ISegmentBufferManager buffer_manager)
        {
            this.BufferManager     = buffer_manager;

            this.ReceiveBufferSize = 8192;
            this.SendBufferSize    = 8192;
            this.ReceiveTimeout    = TimeSpan.Zero;
            this.SendTimeout       = TimeSpan.Zero;
            this.KeepAlive         = false;
            this.KeepAliveInterval = TimeSpan.FromSeconds(5);
            this.ReuseAddress      = false;

            this.EnableBroadcast   = true;
            this.DontFragment      = true;

            this.ConnectTimeout    = TimeSpan.FromSeconds(15);
            this.FrameBuilder      = new LengthPrefixedFrameBuilder();
        }
        #endregion
    }
}
