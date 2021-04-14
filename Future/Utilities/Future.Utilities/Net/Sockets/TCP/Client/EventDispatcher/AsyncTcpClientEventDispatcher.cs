using System;
using System.Threading.Tasks;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Async tcp client event dispatcher class.
    /// </summary>
    public class AsyncTcpClientEventDispatcher : IAsyncTcpClientEventDispatcher
    {
        #region [ Callback ]
        /// <summary>
        /// On server connected callback.
        /// </summary>
        private Func<AsyncTcpClient, Task> _on_server_connected;

        /// <summary>
        /// On server data received callback.
        /// </summary>
        private Func<AsyncTcpClient, byte[], int, int, Task> _on_server_data_received;

        /// <summary>
        /// On server disconnected callback.
        /// </summary>
        private Func<AsyncTcpClient, Task> _on_server_disconnected;
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Construct a new <see cref="AsyncTcpClientEventDispatcher"></see> class's instance.
        /// </summary>
        public AsyncTcpClientEventDispatcher()
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncTcpClientEventDispatcher"></see> class's instance.
        /// </summary>
        /// <param name="on_server_data_received"> On server data received callback. </param>
        /// <param name="on_server_connected">     On server connected callback.     </param>
        /// <param name="on_server_disconnected">  On server disconnected callback.  </param>
        public AsyncTcpClientEventDispatcher(
            Func<AsyncTcpClient, byte[], int, int, Task> on_server_data_received,
            Func<AsyncTcpClient, Task>                   on_server_connected,
            Func<AsyncTcpClient, Task>                   on_server_disconnected)
            : this()
        {
            this._on_server_data_received = on_server_data_received;
            this._on_server_connected     = on_server_connected;
            this._on_server_disconnected  = on_server_disconnected;
        }
        #endregion

        #region [ Event ]
        /// <summary>
        /// On server connected event.
        /// </summary>
        /// <param name="client">Tcp client.</param>
        /// <returns>
        ///  Server connected opertion task.
        /// </returns>
        public async Task OnServerConnected(AsyncTcpClient client)
        {
            if (null != this._on_server_connected)
            {
                await this._on_server_connected(client);
            }
        }

        /// <summary>
        /// On server data received event.
        /// </summary>
        /// <param name="client">Tcp client.</param>
        /// <param name="data">Received data.</param>
        /// <param name="offset">Received data's offset.</param>
        /// <param name="count">Received data's count.</param>
        /// <returns>
        /// Server data received opertion task.
        /// </returns>
        public async Task OnServerDataReceived(AsyncTcpClient client, byte[] data, int offset, int count)
        {
            if (null != this._on_server_data_received)
            {
                await this._on_server_data_received(client, data, offset, count);
            }
        }

        /// <summary>
        /// On server disconnected event.
        /// </summary>
        /// <param name="client">Tcp client.</param>
        /// <returns>
        /// Server disconnected opertion task.
        /// </returns>
        public async Task OnServerDisconnected(AsyncTcpClient client)
        {
            if (null != this._on_server_disconnected)
            {
                await this._on_server_disconnected(client);
            }
        }
        #endregion
    }
}
