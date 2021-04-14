using System;
using System.Text;
using System.Threading.Tasks;

using Future.Utilities.Net.Sockets;

namespace Future.Utilities.Tests.Net.Sockets.AsyncTcpServer
{
    public class TcpServerEventDispatcher : IAsyncTcpServerEventDispatcher
    {
        public async Task OnSessionStarted(AsyncTcpServerSession session)
        {
            Console.WriteLine($"TCP session {session.RemoteEndPoint} has connected {session}.");
            await Task.CompletedTask;
        }

        public async Task OnSessionDataReceived(AsyncTcpServerSession session, byte[] data, int offset, int count)
        {
            var text = Encoding.UTF8.GetString(data, offset, count);
            Console.Write($"Client : {session.RemoteEndPoint} --> ");
            if (count < 1024 * 1024 * 1)
            {
                Console.WriteLine(text);
            }
            else
            {
                Console.WriteLine("{0} Bytes", count);
            }

            await session.SendAsync(Encoding.UTF8.GetBytes(text));
        }

        public async Task OnSessionClosed(AsyncTcpServerSession session)
        {
            Console.WriteLine($"TCP session {session} has disconnected.");
            await Task.CompletedTask;
        }
    }
}
