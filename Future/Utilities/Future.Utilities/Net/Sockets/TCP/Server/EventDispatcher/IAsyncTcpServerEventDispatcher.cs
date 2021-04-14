using System.Threading.Tasks;

namespace Future.Utilities.Net.Sockets
{
    public interface IAsyncTcpServerEventDispatcher
    {
        Task OnSessionStarted(AsyncTcpServerSession session);
        Task OnSessionDataReceived(AsyncTcpServerSession session, byte[] data, int offset, int count);
        Task OnSessionClosed(AsyncTcpServerSession session);
    }
}
