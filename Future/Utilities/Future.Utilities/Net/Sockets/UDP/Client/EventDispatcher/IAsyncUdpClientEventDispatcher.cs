using System.Threading.Tasks;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Async udp client event dispatcher interface.
    /// </summary>
    public interface IAsyncUdpClientEventDispatcher
    {
        #region [ Event ]
        /// <summary>
        /// On data received event.
        /// </summary>
        /// <param name="udp">Udp.</param>
        /// <param name="data">Received data.</param>
        /// <param name="offset">Received data's offset.</param>
        /// <param name="count">Received data's count.</param>
        /// <returns>
        /// Udp data received opertion task.
        /// </returns>
        Task OnDataReceived(AsyncUdpClient udp, byte[] data, int offset, int count);
        #endregion
    }
}
