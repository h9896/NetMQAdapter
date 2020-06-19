using NetMQAdapter.Socket;
using NetMQAdapter.Timer;

namespace NetMQAdapter.Poller
{
    public interface IPoller
    {
        ISocket AddSocket(SocketType socketType, string endPoint, bool isBind, string name, string identity = "", int dataLength = 4);
        ITimer AddTimer(int interval, ISocket socket);
        void Start();
        void Stop(string name);
        void StopAll();
        void Dispose();
    }
}
