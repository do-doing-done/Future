using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Future.Utilities.Net.Sockets
{
    public class AsyncTcpServerSession
    {
        #region [ Constants ]
        /// <summary>
        /// Tcp connection state: Unknown State.
        /// </summary>
        private const int NONE       = 0;
        /// <summary>
        /// Tcp connection state: Connecting.
        /// </summary>
        private const int CONNECTING = 1;
        /// <summary>
        /// Tcp connection state: Connected.
        /// </summary>
        private const int CONNECTED  = 2;
        /// <summary>
        /// Tcp connection state: Disposed.
        /// </summary>
        private const int DISPOSED   = 5;
        #endregion

        #region [ Fields ]
        /* Parameters */
        private          TcpClient                      _tcp_client;
        private          IPEndPoint                     _remote_end_point;
        private          IPEndPoint                     _local_end_point;
        /* Server */
        private readonly AsyncTcpServer                 _server;
        private readonly AsyncTcpServerConfiguration    _configuration;
        private readonly IAsyncTcpServerEventDispatcher _dispatcher;
        /* Client */
        private readonly string                         _session_key;
        private readonly DateTime                       _start_time;
        /* Memory */
        private readonly ISegmentBufferManager          _buffer_manager;
        private          Stream                         _stream;
        private          ArraySegment<byte>             _receive_buffer        = default;
        private          int                            _receive_buffer_offset = 0;
        /* State */
        private          int                            _state;
        #endregion

        #region [ Properties ]
        /* Parameters */
        public IPEndPoint RemoteEndPoint => (this.Connected ? (IPEndPoint)_tcp_client.Client.RemoteEndPoint : this._remote_end_point);
        public IPEndPoint LocalEndPoint => (this.Connected ? (IPEndPoint)_tcp_client.Client.LocalEndPoint : this._local_end_point);

        /* Server */
        public AsyncTcpServer Server => this._server;
        public TimeSpan ConnectTimeout => this._configuration.ConnectTimeout;

        /* Client */
        public Socket Socket => (this.Connected ? this._tcp_client.Client : null);
        public string SessionKey => this._session_key;
        public DateTime StartTime => this._start_time;
        private bool Connected => (true == this._tcp_client?.Client?.Connected);
        
        /* Memory */
        public Stream Stream => this._stream;
        
        /* State */
        public TcpConnectionState State
        {
            get
            {
                return _state switch
                {
                    NONE       => TcpConnectionState.None,
                    CONNECTING => TcpConnectionState.Connecting,
                    CONNECTED  => TcpConnectionState.Connected,
                    DISPOSED   => TcpConnectionState.Closed,
                    _          => TcpConnectionState.Closed,
                };
            }
        }
        #endregion

        #region [ Constructors ]
        /// <summary>
        /// Construct a new <see cref="AsyncTcpServerSession"></see> class's instance.
        /// </summary>
        /// <param name="tcp_client"></param>
        /// <param name="configuration"></param>
        /// <param name="buffer_manager"></param>
        /// <param name="dispatcher"></param>
        /// <param name="server"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public AsyncTcpServerSession(
            TcpClient                      tcp_client,
            AsyncTcpServerConfiguration    configuration,
            ISegmentBufferManager          buffer_manager,
            IAsyncTcpServerEventDispatcher dispatcher,
            AsyncTcpServer                 server)
        {
            this._tcp_client     = tcp_client     ?? throw new ArgumentNullException($"{nameof(tcp_client)}");
            this._configuration  = configuration  ?? throw new ArgumentNullException($"{nameof(configuration)}");
            this._buffer_manager = buffer_manager ?? throw new ArgumentNullException($"{nameof(buffer_manager)}");
            this._dispatcher     = dispatcher     ?? throw new ArgumentNullException($"{nameof(dispatcher)}");
            this._server         = server         ?? throw new ArgumentNullException($"{nameof(server)}");

            this._session_key    = Guid.NewGuid().ToString();
            this._start_time     = DateTime.UtcNow;

            this.SetSocketOptions();

            this._remote_end_point = this.RemoteEndPoint;
            this._local_end_point  = this.LocalEndPoint;
        }
        #endregion

        #region [ Process ]
        /// <summary>
        /// Start session.
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        internal async Task Start()
        {
            int origin = Interlocked.CompareExchange(ref this._state, CONNECTING, NONE);
            if (origin == DISPOSED)
            {
                throw new ObjectDisposedException("This tcp socket session has been disposed when connecting.");
            }
            else if (origin != NONE)
            {
                throw new InvalidOperationException("This tcp socket session is in invalid state when connecting.");
            }

            try
            {
                Task<Stream> negotiator = this.NegotiateStream(this._tcp_client.GetStream());
                if (!negotiator.Wait(this.ConnectTimeout))
                {
                    await this.Close(false);
                    throw new TimeoutException($"Negotiate SSL/TSL with remote [{this.RemoteEndPoint}] timeout [{this.ConnectTimeout}].");
                }
                this._stream = negotiator.Result;

                if (default == this._receive_buffer) this._receive_buffer = this._buffer_manager.BorrowBuffer();
                this._receive_buffer_offset = 0;

                if (Interlocked.CompareExchange(ref this._state, CONNECTED, CONNECTING) != CONNECTING)
                {
                    await this.Close(false);
                    throw new ObjectDisposedException("This tcp socket session has been disposed after connected.");
                }

                bool isErrorOccurredInUserSide = false;
                try
                {
                    await this._dispatcher.OnSessionStarted(this);
                }
                catch (Exception ex)
                {
                    isErrorOccurredInUserSide = true;
                    await HandleUserSideError(ex);
                }

                if (!isErrorOccurredInUserSide)
                {
                    await this.Process();
                }
                else
                {
                    await this.Close(true);
                }
            }
            catch
            {
                await this.Close(true);
                throw;
            }
        }

        private async Task Process()
        {
            try
            {
                int frameLength;
                byte[] payload;
                int payloadOffset;
                int payloadCount;
                int consumedLength = 0;

                while (State == TcpConnectionState.Connected)
                {
                    int receiveCount = await _stream.ReadAsync(
                        _receive_buffer.Array,
                        _receive_buffer.Offset + _receive_buffer_offset,
                        _receive_buffer.Count - _receive_buffer_offset);
                    if (receiveCount == 0)
                        break;

                    SegmentBufferDeflector.ReplaceBuffer(_buffer_manager, ref _receive_buffer, ref _receive_buffer_offset, receiveCount);
                    consumedLength = 0;

                    while (true)
                    {
                        frameLength = 0;
                        payload = null;
                        payloadOffset = 0;
                        payloadCount = 0;

                        if (_configuration.FrameBuilder.Decoder.TryDecodeFrame(
                            _receive_buffer.Array,
                            _receive_buffer.Offset + consumedLength,
                            _receive_buffer_offset - consumedLength,
                            out frameLength, out payload, out payloadOffset, out payloadCount))
                        {
                            try
                            {
                                await _dispatcher.OnSessionDataReceived(this, payload, payloadOffset, payloadCount);
                            }
                            catch (Exception ex) // catch all exceptions from out-side
                            {
                                await HandleUserSideError(ex);
                            }
                            finally
                            {
                                consumedLength += frameLength;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (_receive_buffer != null && _receive_buffer.Array != null)
                    {
                        SegmentBufferDeflector.ShiftBuffer(_buffer_manager, consumedLength, ref _receive_buffer, ref _receive_buffer_offset);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // looking forward to a graceful quit from the ReadAsync but the inside EndRead will raise the ObjectDisposedException,
                // so a gracefully close for the socket should be a Shutdown, but we cannot avoid the Close triggers this happen.
            }
            catch (Exception ex)
            {
                await HandleReceiveOperationException(ex);
            }
            finally
            {
                await Close(true); // read async buffer returned, remote notifies closed
            }
        }

        private void SetSocketOptions()
        {
            _tcp_client.ReceiveBufferSize = _configuration.ReceiveBufferSize;
            _tcp_client.SendBufferSize = _configuration.SendBufferSize;
            _tcp_client.ReceiveTimeout = (int)_configuration.ReceiveTimeout.TotalMilliseconds;
            _tcp_client.SendTimeout = (int)_configuration.SendTimeout.TotalMilliseconds;
            _tcp_client.NoDelay = _configuration.NoDelay;
            _tcp_client.LingerState = _configuration.LingerState;

            if (_configuration.KeepAlive)
            {
                _tcp_client.Client.SetSocketOption(
                    SocketOptionLevel.Socket,
                    SocketOptionName.KeepAlive,
                    (int)_configuration.KeepAliveInterval.TotalMilliseconds);
            }

            _tcp_client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, _configuration.ReuseAddress);
        }

        private async Task<Stream> NegotiateStream(Stream stream)
        {
            if (!_configuration.SslEnabled)
                return stream;

            var validateRemoteCertificate = new RemoteCertificateValidationCallback(
                (object sender,
                X509Certificate certificate,
                X509Chain chain,
                SslPolicyErrors sslPolicyErrors)
                =>
                {
                    if (sslPolicyErrors == SslPolicyErrors.None)
                        return true;

                    if (_configuration.SslPolicyErrorsBypassed)
                        return true;
                    else
                        

                    return false;
                });

            var sslStream = new SslStream(
                stream,
                false,
                validateRemoteCertificate,
                null,
                _configuration.SslEncryptionPolicy);

            if (!_configuration.SslClientCertificateRequired)
            {
                await sslStream.AuthenticateAsServerAsync(
                    _configuration.SslServerCertificate); // The X509Certificate used to authenticate the server.
            }
            else
            {
                await sslStream.AuthenticateAsServerAsync(
                    _configuration.SslServerCertificate, // The X509Certificate used to authenticate the server.
                    _configuration.SslClientCertificateRequired, // A Boolean value that specifies whether the client must supply a certificate for authentication.
                    _configuration.SslEnabledProtocols, // The SslProtocols value that represents the protocol used for authentication.
                    _configuration.SslCheckCertificateRevocation); // A Boolean value that specifies whether the certificate revocation list is checked during authentication.
            }

            return sslStream;
        }

        #endregion

        #region Close

        public async Task Close()
        {
            await Close(true); // close by external
        }

        private async Task Close(bool shallNotifyUserSide)
        {
            if (Interlocked.Exchange(ref _state, DISPOSED) == DISPOSED)
            {
                return;
            }

            Shutdown();

            if (shallNotifyUserSide)
            {
                try
                {
                    await _dispatcher.OnSessionClosed(this);
                }
                catch (Exception ex) // catch all exceptions from out-side
                {
                    await HandleUserSideError(ex);
                }
            }

            Clean();
        }

        public void Shutdown()
        {
            // The correct way to shut down the connection (especially if you are in a full-duplex conversation) 
            // is to call socket.Shutdown(SocketShutdown.Send) and give the remote party some time to close 
            // their send channel. This ensures that you receive any pending data instead of slamming the 
            // connection shut. ObjectDisposedException should never be part of the normal application flow.
            if (_tcp_client != null && _tcp_client.Connected)
            {
                _tcp_client.Client.Shutdown(SocketShutdown.Send);
            }
        }

        private void Clean()
        {
            try
            {
                try
                {
                    if (_stream != null)
                    {
                        _stream.Dispose();
                    }
                }
                catch { }
                try
                {
                    if (_tcp_client != null)
                    {
                        _tcp_client.Close();
                    }
                }
                catch { }
            }
            catch { }
            finally
            {
                _stream = null;
                _tcp_client = null;
            }

            if (_receive_buffer != default(ArraySegment<byte>))
                _configuration.BufferManager.ReturnBuffer(_receive_buffer);
            _receive_buffer = default(ArraySegment<byte>);
            _receive_buffer_offset = 0;
        }

        #endregion

        #region Exception Handler

        private async Task HandleSendOperationException(Exception ex)
        {
            if (IsSocketTimeOut(ex))
            {
                await CloseIfShould(ex);
                throw new TcpException(ex.Message, new TimeoutException(ex.Message, ex));
            }

            await CloseIfShould(ex);
            throw new TcpException(ex.Message, ex);
        }

        private async Task HandleReceiveOperationException(Exception ex)
        {
            if (IsSocketTimeOut(ex))
            {
                await CloseIfShould(ex);
                throw new TcpException(ex.Message, new TimeoutException(ex.Message, ex));
            }

            await CloseIfShould(ex);
            throw new TcpException(ex.Message, ex);
        }

        private bool IsSocketTimeOut(Exception ex)
        {
            return ex is IOException
                && ex.InnerException != null
                && ex.InnerException is SocketException
                && (ex.InnerException as SocketException).SocketErrorCode == SocketError.TimedOut;
        }

        private async Task<bool> CloseIfShould(Exception ex)
        {
            if (ex is ObjectDisposedException
                || ex is InvalidOperationException
                || ex is SocketException
                || ex is IOException
                || ex is NullReferenceException // buffer array operation
                || ex is ArgumentException      // buffer array operation
                )
            {
                await Close(true); // catch specified exception then intend to close the session

                return true;
            }

            return false;
        }

        private async Task HandleUserSideError(Exception ex)
        {
            await Task.CompletedTask;
        }

        #endregion

        #region Send

        public async Task SendAsync(byte[] data)
        {
            await SendAsync(data, 0, data.Length);
        }

        public async Task SendAsync(byte[] data, int offset, int count)
        {
            BufferValidator.ValidateBuffer(data, offset, count, "data");

            if (State != TcpConnectionState.Connected)
            {
                throw new InvalidOperationException("This session has not connected.");
            }

            try
            {
                byte[] frameBuffer;
                int frameBufferOffset;
                int frameBufferLength;
                _configuration.FrameBuilder.Encoder.EncodeFrame(data, offset, count, out frameBuffer, out frameBufferOffset, out frameBufferLength);

                await _stream.WriteAsync(frameBuffer, frameBufferOffset, frameBufferLength);
            }
            catch (Exception ex)
            {
                await HandleSendOperationException(ex);
            }
        }

        #endregion

        #region [ To String ]
        /// <summary>
        /// Instance to string.
        /// </summary>
        /// <returns>
        /// Instance's string.
        /// </returns>
        public override string ToString()
        {
            return $"Session Key[{this.SessionKey}], " +
                   $"Remote End Point[{this.RemoteEndPoint}], " +
                   $"Local End Point[{this.LocalEndPoint}]";
        }
        #endregion
    }
}
