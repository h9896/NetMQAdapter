using NetMQ;

namespace NetMQAdapter.Timer
{
    public interface ITimer
    {
        NetMQTimer Timer { get; }
        void SetTimerElapsed(byte[][] data);
    }
}
