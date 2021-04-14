using System;
using System.Threading.Tasks;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Async tcp server event dispatcher class.
    /// </summary>
    public class AsyncTcpServerEventDispatcher : IAsyncTcpServerEventDispatcher
    {
        #region [ Callback ]
        /// <summary>
        /// On session data received callback.
        /// </summary>
        private Func<AsyncTcpServerSession, byte[], int, int, Task> _on_session_data_received;

        /// <summary>
        /// On session started callback.
        /// </summary>
        private Func<AsyncTcpServerSession, Task> _on_session_started;

        /// <summary>
        /// On session closed callback.
        /// </summary>
        private Func<AsyncTcpServerSession, Task> _on_session_closed;
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Construct a new <see cref="AsyncTcpServerEventDispatcher"></see> class's instance.
        /// </summary>
        public AsyncTcpServerEventDispatcher()
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncTcpServerEventDispatcher"></see> class's instance.
        /// </summary>
        /// <param name="on_session_data_received"></param>
        /// <param name="on_session_started"></param>
        /// <param name="on_session_closed"></param>
        public AsyncTcpServerEventDispatcher(
            Func<AsyncTcpServerSession, byte[], int, int, Task> on_session_data_received,
            Func<AsyncTcpServerSession, Task>                   on_session_started,
            Func<AsyncTcpServerSession, Task>                   on_session_closed)
            : this()
        {
            this._on_session_data_received = on_session_data_received;
            this._on_session_started       = on_session_started;
            this._on_session_closed        = on_session_closed;
        }
        #endregion

        #region [ Event ]
        /// <summary>
        /// On session started event.
        /// </summary>
        /// <param name="session">TCP session.</param>
        /// <returns>
        /// Session started opertion task.
        /// </returns>
        public async Task OnSessionStarted(AsyncTcpServerSession session)
        {
            if (null != this._on_session_started)
            {
                await this._on_session_started(session);
            }
        }

        /// <summary>
        /// On session data received event.
        /// </summary>
        /// <param name="session">TCP session.</param>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns>
        /// Session data received task.
        /// </returns>
        public async Task OnSessionDataReceived(AsyncTcpServerSession session, byte[] data, int offset, int count)
        {
            if (null != this._on_session_data_received)
            {
                await this._on_session_data_received(session, data, offset, count);
            }
        }

        /// <summary>
        /// On session closed event.
        /// </summary>
        /// <param name="session">TCP session.</param>
        /// <returns>
        /// Session closed opertion task.
        /// </returns>
        public async Task OnSessionClosed(AsyncTcpServerSession session)
        {
            if (null != this._on_session_closed)
            {
                await this._on_session_closed(session);
            }
        }
        #endregion
    }
}
