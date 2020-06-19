using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using static NetMQAdapter.Element;

namespace NetMQAdapter.Socket
{
    internal class SocketAdapter : ISocket
    {
        public string ZMQName { get { return _zmqName; } }

        public event ReceiveHandler Received { add { _receive += value; } remove { _receive -= value; } }
        internal NetMQSocket _socket { get; set; }
        private ReceiveHandler _receive { get; set; }
        private string _zmqName { get; set; }
        private string _endPoint { get; set; }
        private ZmqSocketType _socketType { get; set; }
        private List<string> _topic { get; set; }
        private int _dataMaxLength { get; set; }
        private bool _isBind { get; set; }
        public void SendData(byte[][] data)
        {
            try
            {
                if (data.Length > _dataMaxLength) { throw new NotImplementedException($"SendData fail: data length out of range-> {_dataMaxLength}, please use SetDataLength to change"); }
                NetMQMessage msg = new NetMQMessage();
                for (int i = 0; i < data.Length; i++)
                {
                    msg.Append(data[i]);
                }
                _socket.TrySendMultipartMessage(msg);
            }
            catch (Exception ex) { throw new NotImplementedException($"SendData fail ZMQName: {_zmqName}, IP: {_endPoint} ---> {ex.Message}\r\n{ex.StackTrace}\r\n{ex.Source}"); }
        }
        public void Subscribe(string[] topic)
        {
            try
            {
                if (_socketType == ZmqSocketType.Sub)
                {
                    foreach (string str in topic)
                    {
                        (_socket as SubscriberSocket).Subscribe(str);
                        _topic.Add(str);
                    }
                }
            }
            catch (Exception ex) { throw new NotImplementedException($"Subscribe fail ZMQName: {_zmqName}, IP: {_endPoint} ---> {ex.Message}\r\n{ex.StackTrace}\r\n{ex.Source}"); }
        }
        internal SocketAdapter(ZmqSocketType socketType, string endPoint, bool isBind, string identity, int dataLength)
        {
            _endPoint = endPoint;
            _socketType = socketType;
            _isBind = isBind;
            _topic = new List<string>();
            SetDataLength(dataLength);
            _socket = InitSocket(socketType, endPoint, isBind, identity);
        }
        internal void SetZmqname(string name) { _zmqName = name; }

        internal void UnBindOrDisconnect()
        {
            if (_isBind) { _socket.Unbind(_endPoint); }
            else { _socket.Disconnect(_endPoint); }
        }
        internal void UnSubscribe()
        {
            if(_socketType == ZmqSocketType.Sub && _topic != null)
            {
                foreach(string str in _topic) { (_socket as SubscriberSocket).Unsubscribe(str); }
            }
        }
        private NetMQSocket InitSocket(ZmqSocketType socketType, string endPoint, bool isBind, string identity)
        {
            NetMQSocket socket = GetSocket(socketType);
            if (identity != null && identity != "") socket.Options.Identity = Encoding.UTF8.GetBytes(identity);
            socket.Options.SendHighWatermark = 10000;
            socket.Options.ReceiveHighWatermark = 10000;
            socket.Options.Linger = TimeSpan.Zero;
            socket.Options.TcpKeepalive = true;
            socket.Options.TcpKeepaliveIdle = TimeSpan.FromMinutes(1);
            socket.Options.TcpKeepaliveInterval = TimeSpan.FromSeconds(10);
            if (isBind) { socket.Bind(endPoint); }
            else { socket.Connect(endPoint); }
            SetReceive(socketType, socket);
            return socket;
        }
        private NetMQSocket GetSocket(ZmqSocketType socketType)
        {
            switch (socketType)
            {
                case ZmqSocketType.Pub:
                    return new NetMQ.Sockets.PublisherSocket();
                case ZmqSocketType.Sub:
                    return new NetMQ.Sockets.SubscriberSocket();
                case ZmqSocketType.Router:
                    return new NetMQ.Sockets.RouterSocket();
                case ZmqSocketType.Dealer:
                    return new NetMQ.Sockets.DealerSocket();
                default:
                    throw new ArgumentOutOfRangeException($"SocketType:({socketType}) out of range!");
            }
        }
        private void SetReceive(ZmqSocketType socketType, NetMQSocket socket)
        {
            switch (socketType)
            {
                case ZmqSocketType.Sub:
                case ZmqSocketType.Dealer:
                case ZmqSocketType.Router:
                    socket.ReceiveReady += Socket_ReceiveReady;
                    break;
            }
        }
        private void Socket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            try
            {
                byte[][] item = new byte[_dataMaxLength][];
                item[0] = e.Socket.ReceiveFrameBytes();
                int i = 0;
                while (e.Socket.Options.ReceiveMore)
                {
                    i++;
                    e.Socket.TryReceiveFrameBytes(out item[i]);
                }
                ReceiveEventArgs eventArgs = new ReceiveEventArgs
                {
                    Item = item,
                    ZMQName = _zmqName
                };
                _receive?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                throw new NotImplementedException($"Socket_ReceiveReady fail ZMQName: {_zmqName}, IP: {_endPoint} ---> {ex.Message}\r\n{ex.StackTrace}\r\n{ex.Source}");
            }
        }
        private void SetDataLength(int length)
        {
            _dataMaxLength = length;
        }
    }
}
