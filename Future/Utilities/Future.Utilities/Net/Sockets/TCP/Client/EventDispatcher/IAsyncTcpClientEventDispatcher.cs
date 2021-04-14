using System.Threading.Tasks;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Async tcp client event dispatcher interface.
    /// </summary>
    public interface IAsyncTcpClientEventDispatcher
    {
        #region [ Event ]
        /// <summary>
        /// On server connected event.
        /// </summary>
        /// <param name="client">Tcp client.</param>
        /// <returns>
        ///  Server connected opertion task.
        /// </returns>
        Task OnServerConnected(AsyncTcpClient client);

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
        Task OnServerDataReceived(AsyncTcpClient client, byte[] data, int offset, int count);

        /// <summary>
        /// On server disconnected event.
        /// </summary>
        /// <param name="client">Tcp client.</param>
        /// <returns>
        /// Server disconnected opertion task.
        /// </returns>
        Task OnServerDisconnected(AsyncTcpClient client);
        #endregion


    }
}
