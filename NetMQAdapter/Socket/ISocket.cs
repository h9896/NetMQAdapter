
using static NetMQAdapter.Element;

namespace NetMQAdapter.Socket
{
    public interface ISocket
    {
        void SendData(byte[][] data);
        void Subscribe(string[] topic);
        string ZMQName { get; }
        event ReceiveHandler Received;
    }
}
