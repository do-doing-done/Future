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
    /// Async udp client class.
    /// </summary>
    public class AsyncUdpClient
    {
        #region [ Constants ]
        /// <summary>
        /// Udp client state: Unknown State.
        /// </summary>
        private const int NONE = 0;
        /// <summary>
        /// Udp client state: Initializing.
        /// </summary>
        private const int INITIALIZING = 1;
        /// <summary>
        /// Udp client state: Running.
        /// </summary>
        private const int RUNNING = 2;
        /// <summary>
        /// Udp client state: Disposed.
        /// </summary>
        private const int DISPOSED = 5;
        #endregion

        #region [ Fields ]
        /* Parameters */
        private          UdpClient                      _udp_client;
        private readonly IPEndPoint                     _remote_end_point;
        private readonly IPEndPoint                     _local_end_point;
        /* Client */
        private readonly IAsyncUdpClientEventDispatcher _dispatcher;
        private readonly AsyncUdpClientConfiguration    _configuration;
        /* Memory */
        private          ArraySegment<byte>             _receive_buffer        = default;
        private          int                            _receive_buffer_offset = 0;
        /* State */
        private          int                            _state;
        #endregion

        #region [ Properties ]
        /* Parameters */
        public IPEndPoint RemoteEndPoint => (this.Connected ? (IPEndPoint)this._udp_client.Client.RemoteEndPoint : this._remote_end_point);
        public IPEndPoint LocalEndPoint => (this.Connected ? (IPEndPoint)this._udp_client.Client.LocalEndPoint : this._local_end_point);

        /* Client */
        private bool IsRunning => (true == this._udp_client?.Client?.IsBound);
        private bool Connected => (true == this._udp_client?.Client?.Connected);
        public TimeSpan ConnectTimeout => this._configuration.ConnectTimeout;

        /* State */
        public UdpConnectionState State
        {
            get
            {
                return _state switch
                {
                    NONE         => UdpConnectionState.None,
                    INITIALIZING => UdpConnectionState.Initializing,
                    RUNNING      => UdpConnectionState.Running,
                    DISPOSED     => UdpConnectionState.Closed,
                    _            => UdpConnectionState.Closed,
                };
            }
        }
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Construct a new <see cref="AsyncUdpClient"></see> class's instance.
        /// </summary>
        /// <param name="dispatcher">Udp client event dispatcher.</param>
        /// <param name="configuration">Udp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncUdpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncUdpClient(IAsyncUdpClientEventDispatcher dispatcher,
                              AsyncUdpClientConfiguration    configuration = null)
            : this(null, null, dispatcher, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncUdpClient"></see> class's instance.
        /// </summary>
        /// <param name="on_server_data_received">On server data received event.</param>
        /// <param name="configuration">Udp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncUdpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncUdpClient(Func<AsyncUdpClient, byte[], int, int, Task> on_server_data_received = null,
                              AsyncUdpClientConfiguration                  configuration           = null)
            : this(null, null, on_server_data_received, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncUdpClient"></see> class's instance.
        /// </summary>
        /// <param name="local_address">Local IP address.</param>
        /// <param name="local_port">Local port.</param>
        /// <param name="remote_address">Remote IP address.</param>
        /// <param name="remote_port">Remote port.</param>
        /// <param name="dispatcher">Udp client event dispatcher.</param>
        /// <param name="configuration">Udp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncUdpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncUdpClient(IPAddress                      local_address,
                              int                            local_port,
                              IPAddress                      remote_address,
                              int                            remote_port,
                              IAsyncUdpClientEventDispatcher dispatcher,
                              AsyncUdpClientConfiguration    configuration = null)
            : this(new IPEndPoint(local_address, local_port), new IPEndPoint(remote_address, remote_port), dispatcher, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncUdpClient"></see> class's instance.
        /// </summary>
        /// <param name="local_end_point">Local end point.</param>
        /// <param name="remote_address">Remote IP address.</param>
        /// <param name="remote_port">Remote port.</param>
        /// <param name="dispatcher">Udp client event dispatcher.</param>
        /// <param name="configuration">Udp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncUdpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncUdpClient(IPEndPoint                     local_end_point,
                              IPAddress                      remote_address,
                              int                            remote_port,
                              IAsyncUdpClientEventDispatcher dispatcher,
                              AsyncUdpClientConfiguration    configuration = null)
            : this(local_end_point, new IPEndPoint(remote_address, remote_port), dispatcher, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncUdpClient"></see> class's instance.
        /// </summary>
        /// <param name="local_address">Local IP address.</param>
        /// <param name="local_port">Local port.</param>
        /// <param name="dispatcher">Udp client event dispatcher.</param>
        /// <param name="configuration">Udp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncUdpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncUdpClient(IPAddress                      local_address,
                              int                            local_port,
                              IAsyncUdpClientEventDispatcher dispatcher,
                              AsyncUdpClientConfiguration    configuration = null)
            : this(new IPEndPoint(local_address, local_port), dispatcher, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncUdpClient"></see> class's instance.
        /// </summary>
        /// <param name="local_end_point">Local end point.</param>
        /// <param name="dispatcher">Udp client event dispatcher.</param>
        /// <param name="configuration">Udp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncUdpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncUdpClient(IPEndPoint                     local_end_point,
                              IAsyncUdpClientEventDispatcher dispatcher,
                              AsyncUdpClientConfiguration    configuration = null)
            : this(local_end_point, null, dispatcher, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncUdpClient"></see> class's instance.
        /// </summary>
        /// <param name="local_address">Local IP address.</param>
        /// <param name="local_port">Local port.</param>
        /// <param name="remote_address">Remote IP address.</param>
        /// <param name="remote_port">Remote port.</param>
        /// <param name="on_server_data_received">On server data received event.</param>
        /// <param name="configuration">Udp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncUdpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncUdpClient(IPAddress                                    local_address,
                              int                                          local_port,
                              IPAddress                                    remote_address,
                              int                                          remote_port,
                              Func<AsyncUdpClient, byte[], int, int, Task> on_server_data_received = null,
                              AsyncUdpClientConfiguration                  configuration           = null)
            : this(new IPEndPoint(local_address, local_port), new IPEndPoint(remote_address, remote_port), on_server_data_received, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncUdpClient"></see> class's instance.
        /// </summary>
        /// <param name="local_end_point">Local end point.</param>
        /// <param name="remote_address">Remote IP address.</param>
        /// <param name="remote_port">Remote port.</param>
        /// <param name="on_server_data_received">On server data received event.</param>
        /// <param name="configuration">Udp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncUdpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncUdpClient(IPEndPoint                                   local_end_point,
                              IPAddress                                    remote_address,
                              int                                          remote_port,
                              Func<AsyncUdpClient, byte[], int, int, Task> on_server_data_received = null,
                              AsyncUdpClientConfiguration                  configuration           = null)
            : this(local_end_point, new IPEndPoint(remote_address, remote_port), on_server_data_received, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncUdpClient"></see> class's instance.
        /// </summary>
        /// <param name="local_address">Local IP address.</param>
        /// <param name="local_port">Local port.</param>
        /// <param name="on_server_data_received">On server data received event.</param>
        /// <param name="configuration">Udp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncUdpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncUdpClient(IPAddress                                    local_address,
                              int                                          local_port,
                              Func<AsyncUdpClient, byte[], int, int, Task> on_server_data_received = null,
                              AsyncUdpClientConfiguration                  configuration           = null)
            : this(new IPEndPoint(local_address, local_port), on_server_data_received, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncUdpClient"></see> class's instance.
        /// </summary>
        /// <param name="local_end_point">Local end point.</param>
        /// <param name="on_server_data_received">On server data received event.</param>
        /// <param name="configuration">Udp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncUdpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncUdpClient(IPEndPoint                                   local_end_point,
                              Func<AsyncUdpClient, byte[], int, int, Task> on_server_data_received = null,
                              AsyncUdpClientConfiguration                  configuration           = null)
            : this(local_end_point, null, on_server_data_received, configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncUdpClient"></see> class's instance.
        /// </summary>
        /// <param name="local_end_point">Local end point.</param>
        /// <param name="remote_end_point">Remote end point.</param>
        /// <param name="on_server_data_received">On server data received event.</param>
        /// <param name="configuration">Udp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncUdpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncUdpClient(IPEndPoint                                   local_end_point,
                              IPEndPoint                                   remote_end_point,
                              Func<AsyncUdpClient, byte[], int, int, Task> on_server_data_received = null,
                              AsyncUdpClientConfiguration                  configuration           = null)
            : this(local_end_point, remote_end_point, new AsyncUdpClientEventDispatcher(on_server_data_received), configuration)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncUdpClient"></see> class's instance.
        /// </summary>
        /// <param name="local_end_point">Local end point.</param>
        /// <param name="remote_end_point">Remote end point.</param>
        /// <param name="dispatcher">Udp client event dispatcher.</param>
        /// <param name="configuration">Udp client configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="AsyncUdpClientConfiguration"></exception>
        /// <exception cref="InvalidProgramException"></exception>
        public AsyncUdpClient(IPEndPoint local_end_point, IPEndPoint remote_end_point, IAsyncUdpClientEventDispatcher dispatcher, AsyncUdpClientConfiguration configuration = null)
        {
            this._local_end_point  = local_end_point;
            this._remote_end_point = remote_end_point;
            this._dispatcher       = dispatcher ?? throw new ArgumentNullException($"{nameof(dispatcher)}");
            this._configuration    = configuration ?? new AsyncUdpClientConfiguration();

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

        #region [ Start & Process ]
        /// <summary>
        /// Connect to Udp server.
        /// </summary>
        /// <returns>
        /// 
        /// </returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task Start()
        {
            int origin = Interlocked.Exchange(ref this._state, INITIALIZING);
            if (!((NONE == origin) || (DISPOSED == origin)))
            {
                await this.Close();
                throw new InvalidOperationException("This Udp socket client is in invalid state when initializing.");
            }
            this.Clean();

            try
            {
                if (null != this._local_end_point)
                {
                    this._udp_client = new UdpClient(this._local_end_point);
                }
                else if (null != this._remote_end_point)
                {
                    this._udp_client = new UdpClient(this._remote_end_point.Address.AddressFamily);
                    Task.Run(() =>
                    {
                        this._udp_client.Connect(this._remote_end_point);
                    }).
                    Wait(this.ConnectTimeout);
                }
                else
                {
                    this._udp_client = new UdpClient();
                }
                this.SetSocketOptions();

                if (default == this._receive_buffer)
                {
                    this._receive_buffer = this._configuration.BufferManager.BorrowBuffer();
                }
                this._receive_buffer_offset = 0;

                if (INITIALIZING != Interlocked.CompareExchange(ref this._state, RUNNING, INITIALIZING))
                {
                    await this.Close();
                    throw new InvalidOperationException("This Udp socket client is in invalid state when running.");
                }

                Task.Factory.StartNew(async () =>
                {
                    await Process();
                },
                TaskCreationOptions.None).Forget();
            }
            catch (Exception exception)
            {
                await this.Close();
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

                while (UdpConnectionState.Running == this.State)
                {
                    receive_count = 0;
                    await Task.Run(() =>
                    {
                        receive_count = this._udp_client.Client.Receive(this._receive_buffer.Array,
                                                                        this._receive_buffer.Offset + this._receive_buffer_offset,
                                                                        this._receive_buffer.Count - this._receive_buffer_offset,
                                                                        SocketFlags.None);
                    });

                    if (0 < receive_count)
                    {
                        this._configuration.BufferManager.ReplaceBuffer(ref this._receive_buffer, ref this._receive_buffer_offset, receive_count);

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
                                    await this._dispatcher.OnDataReceived(this, payload, payload_offset, payload_count);
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
                            this._configuration.BufferManager.ShiftBuffer(consumed_length, ref this._receive_buffer, ref this._receive_buffer_offset);
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
                await this.Close();
            }
        }

        /// <summary>
        /// Set socket options.
        /// </summary>
        private void SetSocketOptions()
        {
            this._udp_client.Client.ReceiveBufferSize = this._configuration.ReceiveBufferSize;
            this._udp_client.Client.SendBufferSize    = this._configuration.SendBufferSize;
            this._udp_client.Client.ReceiveTimeout    = (int)this._configuration.ReceiveTimeout.TotalMilliseconds;
            this._udp_client.Client.SendTimeout       = (int)this._configuration.SendTimeout.TotalMilliseconds;
            this._udp_client.EnableBroadcast          = this._configuration.EnableBroadcast;
            this._udp_client.DontFragment             = this._configuration.DontFragment;

            if (this._configuration.KeepAlive)
            {
                this._udp_client.Client.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.KeepAlive, (int)this._configuration.KeepAliveInterval.TotalMilliseconds);
            }
            this._udp_client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, this._configuration.ReuseAddress);
        }
        #endregion

        #region [ Close ]
        /// <summary>
        /// Close Udp client.
        /// </summary>
        /// <returns></returns>
        public async Task Close()
        {
            if (DISPOSED != Interlocked.Exchange(ref this._state, DISPOSED))
            {
                await Task.Run(() =>
                {
                    this.Shutdown();
                    this.Clean();
                });
            }
        }

        /// <summary>
        /// Shutdown Udp client.
        /// </summary>
        public void Shutdown()
        {
            if (true == this._udp_client?.Client?.IsBound)
            {
                this._udp_client.Client.Shutdown(SocketShutdown.Send);
            }
        }

        /// <summary>
        /// Close Udp connection.
        /// </summary>
        private void Clean()
        {
            try
            {
                try { this._udp_client?.Close(); } catch { }
            }
            catch
            {
                // Do nothing.
            }
            finally
            {
                this._udp_client = null;
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
                throw new UdpException(exception.Message, new TimeoutException(exception.Message, exception));
            }
            else
            {
                await this.CloseIfShould(exception);
                throw new UdpException(exception.Message, exception);
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
                throw new UdpException(exception.Message, new TimeoutException(exception.Message, exception));
            }
            else
            {
                await this.CloseIfShould(exception);
                throw new UdpException(exception.Message, exception);
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
        /// if exception happend, need close Udp client.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <returns>
        /// <see cref="true" />  Shoud close Udp client and already closed.
        /// <see cref="false" /> Shoud not close Udp client and keep alive.
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
                await this.Close();
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
        /// Send data to Udp server.
        /// </summary>
        /// <param name="data">Sent data.</param>
        /// <returns>
        /// 
        /// </returns>
        public async Task SendAsync(byte[] data, IPEndPoint remote = null)
        {
            await this.SendAsync(data, 0, data.Length, remote);
        }

        /// <summary>
        /// Send data to Udp server.
        /// </summary>
        /// <param name="data">Sent data.</param>
        /// <param name="offset">Sent data's offset.</param>
        /// <param name="count">Sent data's count.</param>
        /// <returns>
        /// 
        /// </returns>
        public async Task SendAsync(byte[] data, int offset, int count, IPEndPoint remote = null)
        {
            BufferValidator.ValidateBuffer(data, offset, count, nameof(data));

            if (UdpConnectionState.Running != this.State)
            {
                throw new InvalidOperationException("This client has not connected to server.");
            }

            try
            {
                this._configuration.FrameBuilder
                                   .Encoder
                                   .EncodeFrame(data, offset, count, out byte[] frame_buffer, out int frame_buffer_offset, out int frame_buffer_length);
                await Task.Run(() =>
                {
                    if (null != remote)
                    {
                        this._udp_client.Client.SendTo(frame_buffer, frame_buffer_offset, frame_buffer_length, SocketFlags.None, remote);
                    }
                    else if (this.Connected)
                    {
                        this._udp_client.Client.Send(frame_buffer, frame_buffer_offset, frame_buffer_length, SocketFlags.None);
                    }
                });
            }
            catch (Exception exception)
            {
                await this.HandleSendOperationException(exception);
            }
        }
        #endregion

        #region [ Broadcast ]

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
