
using static NetMQAdapter.Element;

namespace NetMQAdapter.Socket
{
    public interface ISocket
    {
        void SendData(byte[][] data);
        void Subscribe(string[] topic);
        void SetDataLength(int length = 4);
        string ZMQName { get; }
        event ReceiveHandler Received;
    }
}
