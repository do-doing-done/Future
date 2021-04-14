using System;
using System.Text;
using System.Threading.Tasks;

using Future.Utilities.Net.Sockets;

namespace Future.Utilities.Tests.Net.Sockets.AsyncTcpClient
{
    public class TcpClientEventDispatcher : IAsyncTcpClientEventDispatcher
    {
        #region [ Constructor ]
        /// <summary>
        /// Construct a new <see cref="TcpClientEventDispatcher"></see> class's instance.
        /// </summary>
        public TcpClientEventDispatcher()
        {
            // Do nothing.
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
        public async Task OnServerConnected(Utilities.Net.Sockets.AsyncTcpClient client)
        {
            Console.WriteLine($"TCP server {client.RemoteEndPoint} has connected.");
            await Task.CompletedTask;
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
        public async Task OnServerDataReceived(Utilities.Net.Sockets.AsyncTcpClient client, byte[] data, int offset, int count)
        {
            Console.Write($"Server : {client.RemoteEndPoint} --> ");
            if (count < 1024 * 1024 * 1)
            {
                Console.WriteLine(Encoding.UTF8.GetString(data, offset, count));
            }
            else
            {
                Console.WriteLine("{0} Bytes", count);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// On server disconnected event.
        /// </summary>
        /// <param name="client">Tcp client.</param>
        /// <returns>
        /// Server disconnected opertion task.
        /// </returns>
        public async Task OnServerDisconnected(Utilities.Net.Sockets.AsyncTcpClient client)
        {
            Console.WriteLine($"TCP server {client.RemoteEndPoint} has disconnected.");
            await Task.CompletedTask;
        }
        #endregion
    }
}
