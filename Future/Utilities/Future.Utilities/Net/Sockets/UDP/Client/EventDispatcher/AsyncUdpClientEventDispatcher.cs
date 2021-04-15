using System;
using System.Threading.Tasks;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Async udp client event dispatcher class.
    /// </summary>
    public class AsyncUdpClientEventDispatcher : IAsyncUdpClientEventDispatcher
    {
        #region [ Callback ]
        /// <summary>
        /// On udp data received callback.
        /// </summary>
        private Func<AsyncUdpClient, byte[], int, int, Task> _on_server_data_received;
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Construct a new <see cref="AsyncUdpClientEventDispatcher"></see> class's instance.
        /// </summary>
        public AsyncUdpClientEventDispatcher()
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="AsyncUdpClientEventDispatcher"></see> class's instance.
        /// </summary>
        /// <param name="on_server_data_received"> On server data received callback. </param>
        public AsyncUdpClientEventDispatcher(Func<AsyncUdpClient, byte[], int, int, Task> on_server_data_received)
            : this()
        {
            this._on_server_data_received = on_server_data_received;
        }
        #endregion

        #region [ Event ]
        /// <summary>
        /// On udp received event.
        /// </summary>
        /// <param name="udp">Tcp client.</param>
        /// <param name="data">Received data.</param>
        /// <param name="offset">Received data's offset.</param>
        /// <param name="count">Received data's count.</param>
        /// <returns>
        /// Udp data received opertion task.
        /// </returns>
        public async Task OnDataReceived(AsyncUdpClient udp, byte[] data, int offset, int count)
        {
            if (null != this._on_server_data_received)
            {
                await this._on_server_data_received(udp, data, offset, count);
            }
        }
        #endregion
    }
}
