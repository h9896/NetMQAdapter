using NetMQAdapter.Socket;
using NetMQAdapter.Timer;

namespace NetMQAdapter.Poller
{
    public interface IPoller
    {
        ISocket AddSocket(string socketType, string endPoint, bool isBind, string name, string identity = "");
        ITimer AddTimer(int interval, ISocket socket);
        void Start();
        void Stop(string name);
        void StopAll();
        void Dispose();
    }
}
