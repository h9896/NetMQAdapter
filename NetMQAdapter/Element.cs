using System;
namespace NetMQAdapter
{
    public class Element
    {
        public delegate void ReceiveHandler(object sender, ReceiveEventArgs e);
    }
    public class ReceiveEventArgs: EventArgs
    {
        public byte[][] Item { get; internal set; }
        public string ZMQName { get; internal set; }
    }
    public enum SocketType
    {
        Router,
        Dealer,
        Pub,
        Sub,
        Request,
        Response,
    }
}
