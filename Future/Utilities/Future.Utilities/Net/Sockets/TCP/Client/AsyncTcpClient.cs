using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Future.Utilities.Threading;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Async tcp client class.
    /// </summary>
    public class AsyncTcpClient
    {
        #region [ Constants ]
        /// <summary>
        /// Tcp connection state: Unknown State.
        /// </summary>
        private const int NONE = 0;
        /// <summary>
        /// Tcp connection state: Connecting.
        /// </summary>
        private const int CONNECTING = 1;
        /// <summary>
        /// Tcp connection state: Connected.
        /// </summary>
        private const int CONNECTED = 2;
        /// <summary>
        /// Tcp connection state: Disposed.
        /// </summary>
        private const int DISPOSED = 5;
        #endregion

        #region [ Fields ]
        /* Parameters */
        private          TcpClient                      _tcp_client;
        private readonly IPEndPoint                     _remote_end_point;
        private readonly IPEndPoint                     _local_end_point;
        /* Client */
        private readonly IAsyncTcpClientEventDispatcher _dispatcher;
        private readonly AsyncTcpClientConfiguration    _configuration;
        /* Memory */
        private          Stream _stream;
        private          ArraySegment<byte>             _receive_buffer        = default;
        private          int                            _receive_buffer_offset = 0;
        /* State */
        private          int                             _state;
        #endregion

        #region [ Properties ]
        /* Parameters */
        public IPEndPoint RemoteEndPoint => (this.Connected ? (IPEndPoint)this._tcp_client.Client.RemoteEndPoint : this._remote_end_point);
        public IPEndPoint LocalEndPoint => (this.Connected ? (IPEndPoint)this._tcp_client.Client.LocalEndPoint : this._local_end_point);

        /* Client */
        private bool Connected => (true == this._tcp_client?.Client?.Connected);
        public TimeSpan ConnectTimeout => this._configuration.ConnectTimeout;

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

        #region [ Constructor ]
        /// <summary>
        /// Construct a new <see cref="AsyncTcpClient"></see> class's instance.
        /// </summary>
        /// <param name="remote_address">Remote IP address.</param>
        /// <param name="remote_port">Remote port.</param>
        /// <param name="local_address">Local IP address.</param>
        /// <param name="local_port">Local port.</param>
        /// <param name="dispatcher">Tcp client event dispatcher.</param>
        /// <param name="configuration">Tcp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncTcpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncTcpClient(IPAddress                      remote_address,
                              int                            remote_port,
                              IPAddress                      local_address,
                              int                            local_port,
                              IAsyncTcpClientEventDispatcher dispatcher,
                              AsyncTcpClientConfiguration    configuration = null)
            : this(new IPEndPoint(remote_address, remote_port), new IPEndPoint(local_address, local_port), dispatcher, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncTcpClient"></see> class's instance.
        /// </summary>
        /// <param name="remote_address">Remote IP address.</param>
        /// <param name="remote_port">Remote port.</param>
        /// <param name="local_end_point">Local end point.</param>
        /// <param name="dispatcher">Tcp client event dispatcher.</param>
        /// <param name="configuration">Tcp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncTcpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncTcpClient(IPAddress                      remote_address,
                              int                            remote_port,
                              IPEndPoint                     local_end_point,
                              IAsyncTcpClientEventDispatcher dispatcher,
                              AsyncTcpClientConfiguration    configuration = null)
            : this(new IPEndPoint(remote_address, remote_port), local_end_point, dispatcher, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncTcpClient"></see> class's instance.
        /// </summary>
        /// <param name="remote_address">Remote IP address.</param>
        /// <param name="remote_port">Remote port.</param>
        /// <param name="dispatcher">Tcp client event dispatcher.</param>
        /// <param name="configuration">Tcp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncTcpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncTcpClient(IPAddress                      remote_address,
                              int                            remote_port,
                              IAsyncTcpClientEventDispatcher dispatcher,
                              AsyncTcpClientConfiguration    configuration = null)
            : this(new IPEndPoint(remote_address, remote_port), dispatcher, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncTcpClient"></see> class's instance.
        /// </summary>
        /// <param name="remote_end_point">Remote end point.</param>
        /// <param name="dispatcher">Tcp client event dispatcher.</param>
        /// <param name="configuration">Tcp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncTcpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncTcpClient(IPEndPoint                     remote_end_point,
                              IAsyncTcpClientEventDispatcher dispatcher,
                              AsyncTcpClientConfiguration    configuration = null)
            : this(remote_end_point, null, dispatcher, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncTcpClient"></see> class's instance.
        /// </summary>
        /// <param name="remote_address">Remote IP address.</param>
        /// <param name="remote_port">Remote port.</param>
        /// <param name="local_address">Local IP address.</param>
        /// <param name="local_port">Local port.</param>
        /// <param name="on_server_data_received">On server data received event.</param>
        /// <param name="on_server_connected">On server connected event.</param>
        /// <param name="on_server_disconnected">On server disconnected event.</param>
        /// <param name="configuration">Tcp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncTcpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncTcpClient(IPAddress                                    remote_address,
                              int                                          remote_port,
                              IPAddress                                    local_address,
                              int                                          local_port,
                              Func<AsyncTcpClient, byte[], int, int, Task> on_server_data_received = null,
                              Func<AsyncTcpClient, Task>                   on_server_connected     = null,
                              Func<AsyncTcpClient, Task>                   on_server_disconnected  = null,
                              AsyncTcpClientConfiguration                  configuration           = null)
            : this(new IPEndPoint(remote_address, remote_port), new IPEndPoint(local_address, local_port), on_server_data_received, on_server_connected, on_server_disconnected, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncTcpClient"></see> class's instance.
        /// </summary>
        /// <param name="remote_address">Remote IP address.</param>
        /// <param name="remote_port">Remote port.</param>
        /// <param name="local_end_point">Local end point.</param>
        /// <param name="on_server_data_received">On server data received event.</param>
        /// <param name="on_server_connected">On server connected event.</param>
        /// <param name="on_server_disconnected">On server disconnected event.</param>
        /// <param name="configuration">Tcp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncTcpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncTcpClient(IPAddress                                    remote_address,
                              int                                          remote_port,
                              IPEndPoint                                   local_end_point,
                              Func<AsyncTcpClient, byte[], int, int, Task> on_server_data_received = null,
                              Func<AsyncTcpClient, Task>                   on_server_connected     = null,
                              Func<AsyncTcpClient, Task>                   on_server_disconnected  = null,
                              AsyncTcpClientConfiguration                  configuration           = null)
            : this(new IPEndPoint(remote_address, remote_port), local_end_point, on_server_data_received, on_server_connected, on_server_disconnected, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncTcpClient"></see> class's instance.
        /// </summary>
        /// <param name="remote_address">Remote IP address.</param>
        /// <param name="remote_port">Remote port.</param>
        /// <param name="on_server_data_received">On server data received event.</param>
        /// <param name="on_server_connected">On server connected event.</param>
        /// <param name="on_server_disconnected">On server disconnected event.</param>
        /// <param name="configuration">Tcp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncTcpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncTcpClient(IPAddress                                    remote_address,
                              int                                          remote_port,
                              Func<AsyncTcpClient, byte[], int, int, Task> on_server_data_received = null,
                              Func<AsyncTcpClient, Task>                   on_server_connected     = null,
                              Func<AsyncTcpClient, Task>                   on_server_disconnected  = null,
                              AsyncTcpClientConfiguration                  configuration           = null)
            : this(new IPEndPoint(remote_address, remote_port), on_server_data_received, on_server_connected, on_server_disconnected, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncTcpClient"></see> class's instance.
        /// </summary>
        /// <param name="remote_end_point">Remote end point.</param>
        /// <param name="on_server_data_received">On server data received event.</param>
        /// <param name="on_server_connected">On server connected event.</param>
        /// <param name="on_server_disconnected">On server disconnected event.</param>
        /// <param name="configuration">Tcp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncTcpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncTcpClient(IPEndPoint                                   remote_end_point,
                              Func<AsyncTcpClient, byte[], int, int, Task> on_server_data_received = null,
                              Func<AsyncTcpClient, Task>                   on_server_connected     = null,
                              Func<AsyncTcpClient, Task>                   on_server_disconnected  = null,
                              AsyncTcpClientConfiguration                  configuration           = null)
            : this(remote_end_point, null, on_server_data_received, on_server_connected, on_server_disconnected, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncTcpClient"></see> class's instance.
        /// </summary>
        /// <param name="remote_end_point">Remote end point.</param>
        /// <param name="local_end_point">Local end point.</param>
        /// <param name="on_server_data_received">On server data received event.</param>
        /// <param name="on_server_connected">On server connected event.</param>
        /// <param name="on_server_disconnected">On server disconnected event.</param>
        /// <param name="configuration">Tcp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncTcpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncTcpClient(IPEndPoint                                   remote_end_point,
                              IPEndPoint                                   local_end_point,
                              Func<AsyncTcpClient, byte[], int, int, Task> on_server_data_received = null,
                              Func<AsyncTcpClient, Task>                   on_server_connected     = null,
                              Func<AsyncTcpClient, Task>                   on_server_disconnected  = null,
                              AsyncTcpClientConfiguration                  configuration           = null)
            : this(remote_end_point, local_end_point, new AsyncTcpClientEventDispatcher(on_server_data_received, on_server_connected, on_server_disconnected), configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncTcpClient"></see> class's instance.
        /// </summary>
        /// <param name="remote_end_point">Remote end point.</param>
        /// <param name="local_end_point">Local end point.</param>
        /// <param name="dispatcher">Tcp client event dispatcher.</param>
        /// <param name="configuration">Tcp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncTcpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncTcpClient(IPEndPoint remote_end_point, IPEndPoint local_end_point, IAsyncTcpClientEventDispatcher dispatcher, AsyncTcpClientConfiguration configuration = null)
        {
            this._remote_end_point = remote_end_point ?? throw new ArgumentNullException($"{nameof(remote_end_point)}");
            this._local_end_point  = local_end_point;
            this._dispatcher       = dispatcher ?? throw new ArgumentNullException($"{nameof(dispatcher)}");
            this._configuration    = configuration ?? new AsyncTcpClientConfiguration();

            if (null == this._configuration.BufferManager)
            {
                throw new InvalidProgramException($"The buffer manager in configuration cannot be null.");
            }
            if (null == this._configuration.FrameBuilder)
            {
                throw new InvalidProgramException($"The frame handler in configuration cannot be null.");
            }
        }
        #endregion

        #region [ Connect ]
        /// <summary>
        /// Connect to tcp server.
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="TimeoutException"></exception>
        public async Task Connect()
        {
            int origin = Interlocked.Exchange(ref this._state, CONNECTING);
            if (!((NONE == origin) || (DISPOSED == origin)))
            {
                await this.Close(false);
                throw new InvalidOperationException("This tcp socket client is in invalid state when connecting.");
            }
            this.Clean();

            try
            {
                this._tcp_client = (null != this._local_end_point)
                                 ? new TcpClient(this._local_end_point)
                                 : new TcpClient(this._remote_end_point.Address.AddressFamily);
                this.SetSocketOptions();

                Task awaiter = _tcp_client.ConnectAsync(this._remote_end_point.Address, this._remote_end_point.Port);
                if (!awaiter.Wait(this.ConnectTimeout))
                {
                    await this.Close(false);
                    throw new TimeoutException($"Connect to [{this._remote_end_point}] timeout [{this.ConnectTimeout}].");
                }

                Task<Stream> negotiator = this.NegotiateStream(this._tcp_client.GetStream());
                if (!negotiator.Wait(this.ConnectTimeout))
                {
                    await this.Close(false);
                    throw new TimeoutException($"Negotiate SSL/TSL with remote [{this._remote_end_point}] timeout [{this.ConnectTimeout}].");
                }
                this._stream = negotiator.Result;

                if (default == this._receive_buffer)
                {
                    this._receive_buffer = this._configuration.BufferManager.BorrowBuffer();
                }
                this._receive_buffer_offset = 0;

                if (CONNECTING != Interlocked.CompareExchange(ref this._state, CONNECTED, CONNECTING))
                {
                    await this.Close(false);
                    throw new InvalidOperationException("This tcp socket client is in invalid state when connected.");
                }

                bool isErrorOccurredInUserSide = false;
                try
                {
                    await this._dispatcher.OnServerConnected(this);
                }
                catch (Exception exception)
                {
                    isErrorOccurredInUserSide = true;
                    await this.HandleUserSideError(exception);
                }

                if (!isErrorOccurredInUserSide)
                {
                    Task.Factory.StartNew(async () =>
                    {
                        await Process();
                    },
                    TaskCreationOptions.None).Forget();
                }
                else
                {
                    await this.Close(true);
                }
            }
            catch (Exception exception)
            {
                await this.Close(true);
                throw(exception);
            }
        }

        /// <summary>
        /// Process receive.
        /// </summary>
        /// <returns>
        /// Task.
        /// </returns>
        private async Task Process()
        {
            try
            {
                int    receive_count;
                int    consumed_length;
                int    frame_length;
                byte[] payload;
                int    payload_offset;
                int    payload_count;

                while (TcpConnectionState.Connected == this.State)
                {
                    receive_count = await _stream.ReadAsync(this._receive_buffer.Array,
                                                            this._receive_buffer.Offset + this._receive_buffer_offset,
                                                            this._receive_buffer.Count - this._receive_buffer_offset);
                    if (0 < receive_count)
                    {
                        SegmentBufferDeflector.ReplaceBuffer(this._configuration.BufferManager,
                                                     ref this._receive_buffer,
                                                     ref this._receive_buffer_offset,
                                                         receive_count);
                        consumed_length = 0;
                        while (true)
                        {
                            frame_length = 0;
                            payload = null;
                            payload_offset = 0;
                            payload_count = 0;

                            if (this._configuration.FrameBuilder.Decoder.TryDecodeFrame(this._receive_buffer.Array,
                                                                                        this._receive_buffer.Offset + consumed_length,
                                                                                        this._receive_buffer_offset - consumed_length,
                                                                                    out frame_length,
                                                                                    out payload,
                                                                                    out payload_offset,
                                                                                    out payload_count))
                            {
                                try
                                {
                                    await this._dispatcher.OnServerDataReceived(this, payload, payload_offset, payload_count);
                                }
                                catch (Exception exception)
                                {
                                    await this.HandleUserSideError(exception);
                                }
                                finally
                                {
                                    consumed_length += frame_length;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }

                        if ((null != this._receive_buffer) && (null != this._receive_buffer.Array))
                        {
                            SegmentBufferDeflector.ShiftBuffer(this._configuration.BufferManager,
                                                               consumed_length,
                                                           ref this._receive_buffer,
                                                           ref this._receive_buffer_offset);
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Do nothing.
            }
            catch (Exception exception)
            {
                await this.HandleReceiveOperationException(exception);
            }
            finally
            {
                await this.Close(true);
            }
        }

        /// <summary>
        /// Set socket options.
        /// </summary>
        private void SetSocketOptions()
        {
            this._tcp_client.ReceiveBufferSize = this._configuration.ReceiveBufferSize;
            this._tcp_client.SendBufferSize    = this._configuration.SendBufferSize;
            this._tcp_client.ReceiveTimeout    = (int)this._configuration.ReceiveTimeout.TotalMilliseconds;
            this._tcp_client.SendTimeout       = (int)this._configuration.SendTimeout.TotalMilliseconds;
            this._tcp_client.NoDelay           = this._configuration.NoDelay;
            this._tcp_client.LingerState       = this._configuration.LingerState;

            if (this._configuration.KeepAlive)
            {
                this._tcp_client.Client.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.KeepAlive, (int)this._configuration.KeepAliveInterval.TotalMilliseconds);
            }
            this._tcp_client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, this._configuration.ReuseAddress);
        }

        /// <summary>
        /// Get negotiate stream.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <returns>
        /// 
        /// </returns>
        private async Task<Stream> NegotiateStream(Stream stream)
        {
            if (this._configuration.SslEnabled)
            {
                var validate = new RemoteCertificateValidationCallback((object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) =>
                {
                    if ((SslPolicyErrors.None == errors) || (this._configuration.SslPolicyErrorsBypassed))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });

                SslStream ssl_stream = new SslStream(stream, false, validate, null, this._configuration.SslEncryptionPolicy);

                if ((null == this._configuration.SslClientCertificates) || (0 == this._configuration.SslClientCertificates.Count))
                {
                    await ssl_stream.AuthenticateAsClientAsync(this._configuration.SslTargetHost);
                }
                else
                {
                    await ssl_stream.AuthenticateAsClientAsync(this._configuration.SslTargetHost,
                                                               this._configuration.SslClientCertificates,
                                                               this._configuration.SslEnabledProtocols,
                                                               this._configuration.SslCheckCertificateRevocation);
                }
                return ssl_stream;
            }
            else
            {
                return stream;
            }
        }
        #endregion

        #region [ Close ]
        /// <summary>
        /// Close tcp client.
        /// </summary>
        /// <returns></returns>
        public async Task Close()
        {
            await this.Close(true);
        }

        /// <summary>
        /// Close tcp client whit notify to user.
        /// </summary>
        /// <param name="shall_notify_user_side">Shall notify user side.</param>
        /// <returns>
        /// 
        /// </returns>
        private async Task Close(bool shall_notify_user_side)
        {
            if (DISPOSED != Interlocked.Exchange(ref _state, DISPOSED))
            {
                this.Shutdown();

                if (shall_notify_user_side)
                {
                    try
                    {
                        await this._dispatcher.OnServerDisconnected(this);
                    }
                    catch (Exception ex)
                    {
                        await this.HandleUserSideError(ex);
                    }
                }

                this.Clean();
            }
        }

        /// <summary>
        /// Shutdown tcp client.
        /// </summary>
        public void Shutdown()
        {
            if (true == this._tcp_client?.Connected)
            {
                this._tcp_client.Client.Shutdown(SocketShutdown.Send);
            }
        }

        /// <summary>
        /// Close tcp connection.
        /// </summary>
        private void Clean()
        {
            try
            {
                try { this._stream?.Dispose(); } catch { }
                try { this._tcp_client?.Close(); } catch { }
            }
            catch
            {
                // Do nothing.
            }
            finally
            {
                this._stream     = null;
                this._tcp_client = null;
            }

            if (default != this._receive_buffer)
            {
                this._configuration.BufferManager.ReturnBuffer(this._receive_buffer);
            }
            this._receive_buffer        = default;
            this._receive_buffer_offset = 0;
        }
        #endregion

        #region [ Exception Handler ]
        /// <summary>
        /// Handle send operation exception.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <returns>
        /// 
        /// </returns>
        private async Task HandleSendOperationException(Exception exception)
        {
            if (this.IsSocketTimeOut(exception))
            {
                await this.CloseIfShould(exception);
                throw new TcpException(exception.Message, new TimeoutException(exception.Message, exception));
            }
            else
            {
                await this.CloseIfShould(exception);
                throw new TcpException(exception.Message, exception);
            }
        }

        /// <summary>
        /// Handle receive operation exception.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <returns>
        /// 
        /// </returns>
        private async Task HandleReceiveOperationException(Exception exception)
        {
            if (IsSocketTimeOut(exception))
            {
                await this.CloseIfShould(exception);
                throw new TcpException(exception.Message, new TimeoutException(exception.Message, exception));
            }
            else
            {
                await this.CloseIfShould(exception);
                throw new TcpException(exception.Message, exception);
            }
        }

        /// <summary>
        /// Is socket time out exception.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <returns>
        /// <see cref="true" />  Is socket time out exception.
        /// <see cref="false" /> Is not socket time out exception
        /// </returns>
        private bool IsSocketTimeOut(Exception exception)
        {
            return (exception is IOException) &&
                   (exception.InnerException != null) &&
                   (exception.InnerException is SocketException) &&
                   (SocketError.TimedOut == (exception.InnerException as SocketException).SocketErrorCode);
        }

        /// <summary>
        /// if exception happend, need close tcp client.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <returns>
        /// <see cref="true" />  Shoud close tcp client and already closed.
        /// <see cref="false" /> Shoud not close tcp client and keep alive.
        /// </returns>
        private async Task<bool> CloseIfShould(Exception exception)
        {
            if ((exception is ObjectDisposedException) ||
                (exception is InvalidOperationException) ||
                (exception is SocketException) ||
                (exception is IOException) ||
                (exception is NullReferenceException) ||
                (exception is ArgumentException))
            {
                await this.Close(false);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Handle user side error.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns>
        /// 
        /// </returns>
        private async Task HandleUserSideError(Exception exception)
        {
            await Task.CompletedTask;
        }
        #endregion

        #region [ Send ]
        /// <summary>
        /// Send data to tcp server.
        /// </summary>
        /// <param name="data">Sent data.</param>
        /// <returns>
        /// 
        /// </returns>
        public async Task SendAsync(byte[] data)
        {
            await this.SendAsync(data, 0, data.Length);
        }

        /// <summary>
        /// Send data to tcp server.
        /// </summary>
        /// <param name="data">Sent data.</param>
        /// <param name="offset">Sent data's offset.</param>
        /// <param name="count">Sent data's count.</param>
        /// <returns>
        /// 
        /// </returns>
        public async Task SendAsync(byte[] data, int offset, int count)
        {
            BufferValidator.ValidateBuffer(data, offset, count, nameof(data));

            if (TcpConnectionState.Connected != this.State)
            {
                throw new InvalidOperationException("This client has not connected to server.");
            }

            try
            {
                this._configuration.FrameBuilder
                                   .Encoder
                                   .EncodeFrame(data, offset, count, out byte[] frame_buffer, out int frame_buffer_offset, out int frame_buffer_length);
                await this._stream.WriteAsync(frame_buffer, frame_buffer_offset, frame_buffer_length);
            }
            catch (Exception exception)
            {
                await this.HandleSendOperationException(exception);
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
            return $"<< Remote End Point[{this.RemoteEndPoint}], Local End Point[{this.LocalEndPoint}] >>";
        }
        #endregion
    }
}
