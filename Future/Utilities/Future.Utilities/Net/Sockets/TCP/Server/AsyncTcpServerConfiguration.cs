using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Async tcp server configuration class.
    /// </summary>
    public class AsyncTcpServerConfiguration
    {
        #region [ Properties ]
        public ISegmentBufferManager BufferManager { get; set; }

        public int ReceiveBufferSize { get; set; }
        public int SendBufferSize { get; set; }
        public TimeSpan ReceiveTimeout { get; set; }
        public TimeSpan SendTimeout { get; set; }
        public bool NoDelay { get; set; }
        public LingerOption LingerState { get; set; }
        public bool KeepAlive { get; set; }
        public TimeSpan KeepAliveInterval { get; set; }
        public bool ReuseAddress { get; set; }

        public int PendingConnectionBacklog { get; set; }
        public bool AllowNatTraversal { get; set; }

        public bool SslEnabled { get; set; }
        public X509Certificate2 SslServerCertificate { get; set; }
        public EncryptionPolicy SslEncryptionPolicy { get; set; }
        public SslProtocols SslEnabledProtocols { get; set; }
        public bool SslClientCertificateRequired { get; set; }
        public bool SslCheckCertificateRevocation { get; set; }
        public bool SslPolicyErrorsBypassed { get; set; }

        public TimeSpan ConnectTimeout { get; set; }
        public IFrameBuilder FrameBuilder { get; set; }
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Construct a new <see cref="AsyncTcpServerConfiguration"></see> class's instance.
        /// </summary>
        public AsyncTcpServerConfiguration()
            : this(new SegmentBufferManager(1024, 8192, 1, true))
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncTcpServerConfiguration"></see> class's instance.
        /// </summary>
        /// <param name="buffer_manager">Segment buffer manager.</param>
        public AsyncTcpServerConfiguration(ISegmentBufferManager buffer_manager)
        {
            this.BufferManager                 = buffer_manager;

            this.ReceiveBufferSize             = 8192;
            this.SendBufferSize                = 8192;
            this.ReceiveTimeout                = TimeSpan.Zero;
            this.SendTimeout                   = TimeSpan.Zero;
            this.NoDelay                       = true;
            this.LingerState                   = new LingerOption(false, 0);
            this.KeepAlive                     = false;
            this.KeepAliveInterval             = TimeSpan.FromSeconds(5);
            this.ReuseAddress                  = false;

            this.PendingConnectionBacklog      = 200;
            this.AllowNatTraversal             = true;

            this.SslEnabled                    = false;
            this.SslServerCertificate          = null;
            this.SslEncryptionPolicy           = EncryptionPolicy.RequireEncryption;
            this.SslEnabledProtocols           = SslProtocols.None;
            this.SslClientCertificateRequired  = true;
            this.SslCheckCertificateRevocation = false;
            this.SslPolicyErrorsBypassed       = false;

            this.ConnectTimeout                = TimeSpan.FromSeconds(15);
            this.FrameBuilder                  = new LengthPrefixedFrameBuilder();
        }
        #endregion
    }
}
