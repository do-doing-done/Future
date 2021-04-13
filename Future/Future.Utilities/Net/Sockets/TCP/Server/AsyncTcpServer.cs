using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Future.Utilities.Threading;

namespace Future.Utilities.Net.Sockets
{
    public class AsyncTcpServer
    {
        #region [ Constants ]
        /// <summary>
        /// Tcp connection state: Unknown State.
        /// </summary>
        private const int NONE = 0;
        /// <summary>
        /// Tcp connection state: Listening.
        /// </summary>
        private const int LISTENING = 1;
        /// <summary>
        /// Tcp connection state: Disposed.
        /// </summary>
        private const int DISPOSED = 5;
        #endregion

        #region [ Fields ]
        /* Server */
        private          TcpListener                                         _listener;
        private readonly ConcurrentDictionary<string, AsyncTcpServerSession> _sessions      = new ConcurrentDictionary<string, AsyncTcpServerSession>();
        private readonly IAsyncTcpServerEventDispatcher                      _dispatcher;
        private readonly AsyncTcpServerConfiguration                         _configuration;
        /* State */
        private          int                                                 _state;
        #endregion

        #region [ Constructor ]

        public AsyncTcpServer(int listenedPort, IAsyncTcpServerEventDispatcher dispatcher, AsyncTcpServerConfiguration configuration = null)
            : this(IPAddress.Any, listenedPort, dispatcher, configuration)
        {
        }

        public AsyncTcpServer(IPAddress listenedAddress, int listenedPort, IAsyncTcpServerEventDispatcher dispatcher, AsyncTcpServerConfiguration configuration = null)
            : this(new IPEndPoint(listenedAddress, listenedPort), dispatcher, configuration)
        {
        }

        public AsyncTcpServer(IPEndPoint listenedEndPoint, IAsyncTcpServerEventDispatcher dispatcher, AsyncTcpServerConfiguration configuration = null)
        {
            if (listenedEndPoint == null)
                throw new ArgumentNullException("listenedEndPoint");
            if (dispatcher == null)
                throw new ArgumentNullException("dispatcher");

            this.ListenedEndPoint = listenedEndPoint;
            _dispatcher = dispatcher;
            _configuration = configuration ?? new AsyncTcpServerConfiguration();

            if (_configuration.BufferManager == null)
                throw new InvalidProgramException("The buffer manager in configuration cannot be null.");
            if (_configuration.FrameBuilder == null)
                throw new InvalidProgramException("The frame handler in configuration cannot be null.");
        }

        public AsyncTcpServer(
            int listenedPort,
            Func<AsyncTcpServerSession, byte[], int, int, Task> onSessionDataReceived = null,
            Func<AsyncTcpServerSession, Task> onSessionStarted = null,
            Func<AsyncTcpServerSession, Task> onSessionClosed = null,
            AsyncTcpServerConfiguration configuration = null)
            : this(IPAddress.Any, listenedPort, onSessionDataReceived, onSessionStarted, onSessionClosed, configuration)
        {
        }

        public AsyncTcpServer(
            IPAddress listenedAddress, int listenedPort,
            Func<AsyncTcpServerSession, byte[], int, int, Task> onSessionDataReceived = null,
            Func<AsyncTcpServerSession, Task> onSessionStarted = null,
            Func<AsyncTcpServerSession, Task> onSessionClosed = null,
            AsyncTcpServerConfiguration configuration = null)
            : this(new IPEndPoint(listenedAddress, listenedPort), onSessionDataReceived, onSessionStarted, onSessionClosed, configuration)
        {
        }

        public AsyncTcpServer(
            IPEndPoint listenedEndPoint,
            Func<AsyncTcpServerSession, byte[], int, int, Task> onSessionDataReceived = null,
            Func<AsyncTcpServerSession, Task> onSessionStarted = null,
            Func<AsyncTcpServerSession, Task> onSessionClosed = null,
            AsyncTcpServerConfiguration configuration = null)
            : this(listenedEndPoint,
                  new AsyncTcpServerEventDispatcher(onSessionDataReceived, onSessionStarted, onSessionClosed),
                  configuration)
        {
        }

        #endregion

        #region Properties

        public IPEndPoint ListenedEndPoint { get; private set; }
        public bool IsListening { get { return _state == LISTENING; } }
        public int SessionCount { get { return _sessions.Count; } }

        #endregion

        #region Server

        public void Listen()
        {
            int origin = Interlocked.CompareExchange(ref _state, LISTENING, NONE);
            if (origin == DISPOSED)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            else if (origin != NONE)
            {
                throw new InvalidOperationException("This tcp server has already started.");
            }

            try
            {
                _listener = new TcpListener(this.ListenedEndPoint);
                SetSocketOptions();

                _listener.Start(_configuration.PendingConnectionBacklog);

                Task.Factory.StartNew(async () =>
                {
                    await Accept();
                },
                TaskCreationOptions.LongRunning)
                .Forget();
            }
            catch (Exception ex) when (!ShouldThrow(ex)) { }
        }

        public void Shutdown()
        {
            if (Interlocked.Exchange(ref _state, DISPOSED) == DISPOSED)
            {
                return;
            }

            try
            {
                _listener.Stop();
                _listener = null;

                Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        foreach (var session in _sessions.Values)
                        {
                            await session.Close(); // parent server close session when shutdown
                        }
                    }
                    catch (Exception ex) when (!ShouldThrow(ex)) { }
                },
                TaskCreationOptions.PreferFairness)
                .Wait();
            }
            catch (Exception ex) when (!ShouldThrow(ex)) { }
        }

        private void SetSocketOptions()
        {
            _listener.AllowNatTraversal(_configuration.AllowNatTraversal);
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, _configuration.ReuseAddress);
        }

        public bool Pending()
        {
            if (!IsListening)
                throw new InvalidOperationException("The tcp server is not active.");

            // determine if there are pending connection requests.
            return _listener.Pending();
        }

        private async Task Accept()
        {
            try
            {
                while (IsListening)
                {
                    var tcpClient = await _listener.AcceptTcpClientAsync();
                    Task.Factory.StartNew(async () =>
                    {
                        await Process(tcpClient);
                    },
                    TaskCreationOptions.None).Forget();
                }
            }
            catch (Exception ex) when (!ShouldThrow(ex)) { }
            catch (Exception ex)
            {
                
            }
        }

        private async Task Process(TcpClient acceptedTcpClient)
        {
            var session = new AsyncTcpServerSession(acceptedTcpClient, _configuration, _configuration.BufferManager, _dispatcher, this);

            if (_sessions.TryAdd(session.SessionKey, session))
            {
                try
                {
                    await session.Start();
                }
                catch (TimeoutException ex)
                {
                    
                }
                finally
                {
                    AsyncTcpServerSession throwAway;
                    if (_sessions.TryRemove(session.SessionKey, out throwAway))
                    {
                        
                    }
                }
            }
        }

        private bool ShouldThrow(Exception ex)
        {
            if (ex is ObjectDisposedException
                || ex is InvalidOperationException
                || ex is SocketException
                || ex is IOException)
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Send

        public async Task SendToAsync(string sessionKey, byte[] data)
        {
            await SendToAsync(sessionKey, data, 0, data.Length);
        }

        public async Task SendToAsync(string sessionKey, byte[] data, int offset, int count)
        {
            AsyncTcpServerSession sessionFound;
            if (_sessions.TryGetValue(sessionKey, out sessionFound))
            {
                await sessionFound.SendAsync(data, offset, count);
            }
            else
            {
                
            }
        }

        public async Task SendToAsync(AsyncTcpServerSession session, byte[] data)
        {
            await SendToAsync(session, data, 0, data.Length);
        }

        public async Task SendToAsync(AsyncTcpServerSession session, byte[] data, int offset, int count)
        {
            AsyncTcpServerSession sessionFound;
            if (_sessions.TryGetValue(session.SessionKey, out sessionFound))
            {
                await sessionFound.SendAsync(data, offset, count);
            }
            else
            {
                
            }
        }

        public async Task BroadcastAsync(byte[] data)
        {
            await BroadcastAsync(data, 0, data.Length);
        }

        public async Task BroadcastAsync(byte[] data, int offset, int count)
        {
            foreach (var session in _sessions.Values)
            {
                await session.SendAsync(data, offset, count);
            }
        }

        #endregion

        #region Session

        public bool HasSession(string sessionKey)
        {
            return _sessions.ContainsKey(sessionKey);
        }

        public AsyncTcpServerSession GetSession(string sessionKey)
        {
            AsyncTcpServerSession session = null;
            _sessions.TryGetValue(sessionKey, out session);
            return session;
        }

        public async Task CloseSession(string sessionKey)
        {
            AsyncTcpServerSession session = null;
            if (_sessions.TryGetValue(sessionKey, out session))
            {
                await session.Close(); // parent server close session by session-key
            }
        }

        #endregion
    }
}
