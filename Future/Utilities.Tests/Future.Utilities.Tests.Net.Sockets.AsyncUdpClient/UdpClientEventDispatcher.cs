using Future.Utilities.Net.Sockets;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Future.Utilities.Tests.Net.Sockets
{
    public class UdpClientEventDispatcher : IAsyncUdpClientEventDispatcher
    {
        #region [ Constructor ]
        /// <summary>
        /// Construct a new <see cref="UdpClientEventDispatcher"></see> class's instance.
        /// </summary>
        public UdpClientEventDispatcher()
        {
            // Do nothing.
        }
        #endregion

        #region [ Event ]
        /// <summary>
        /// On udp data received event.
        /// </summary>
        /// <param name="client">Udp client.</param>
        /// <param name="data">Received data.</param>
        /// <param name="offset">Received data's offset.</param>
        /// <param name="count">Received data's count.</param>
        /// <returns>
        /// Server data received opertion task.
        /// </returns>
        public async Task OnDataReceived(Utilities.Net.Sockets.AsyncUdpClient client, byte[] data, int offset, int count)
        {
            Console.WriteLine($"Client : {client.RemoteEndPoint} --> ");
            if (count < 1024 * 1024 * 1)
            {
                Console.WriteLine(Encoding.UTF8.GetString(data, offset, count));
            }
            else
            {
                Console.WriteLine("{0} Bytes", count);
            }
            Console.WriteLine();
            await Task.CompletedTask;
        }
        #endregion
    }
}
