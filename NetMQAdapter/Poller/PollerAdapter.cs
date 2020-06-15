using System;
using System.Collections.Generic;
using NetMQAdapter.Socket;
using NetMQAdapter.Timer;
using NetMQ;
using System.Collections.Concurrent;

namespace NetMQAdapter.Poller
{
    public class PollerAdapter : IDisposable, IPoller
    {
        private NetMQPoller _poller { get; set; }
        private ConcurrentDictionary<string, SocketAdapter> _socketDictionary { get; set; }
        public PollerAdapter()
        {
            _poller = new NetMQPoller();
            _socketDictionary = new ConcurrentDictionary<string, SocketAdapter>();
        }
        public ISocket AddSocket(string socketType, string endPoint, bool isBind, string name, string identity = "")
        {
            try
            {
                SocketAdapter socket = new SocketAdapter(GetSocketType(socketType), endPoint, isBind, identity);
                socket.SetZmqname(name);
                _poller.Add(socket._socket);
                if (_socketDictionary.ContainsKey(name)) { _socketDictionary.TryUpdate(name, socket, socket); }
                else { _socketDictionary.TryAdd(name, socket); }
                return socket;
            }
            catch (Exception ex)
            {
                throw new NotImplementedException($"AddSocket: EndPoint: {endPoint} ---> {ex.Message}\r\n{ex.StackTrace}\r\n{ex.Source}");
            }
        }

        public ITimer AddTimer(int interval, ISocket socket)
        {
            try
            {
                TimerAdapter timer = new TimerAdapter(interval, socket);
                _poller.Add(timer.Timer);
                return timer;
            }
            catch (Exception ex)
            {
                throw new NotImplementedException($"AddTimer: ISocket-Name: {socket.ZMQName} ---> {ex.Message}\r\n{ex.StackTrace}\r\n{ex.Source}");
            }
        }

        public void Dispose()
        {
            Stop();
            _poller.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            try
            {
                _poller.RunAsync();
            }
            catch (Exception ex)
            {
                throw new NotImplementedException($"Start ---> {ex.Message}\r\n{ex.StackTrace}\r\n{ex.Source}");
            }
        }

        public void Stop()
        {
            try
            {
                if (_poller.IsRunning)
                {
                    foreach(KeyValuePair<string, SocketAdapter> item in _socketDictionary)
                    {
                        item.Value.UnSubscribe();
                        item.Value.UnBindOrDisconnect();
                        _poller.RemoveAndDispose(item.Value._socket);
                    }
                    _poller.Stop();
                }
            }
            catch (Exception ex) { throw new NotImplementedException($"Stop ---> {ex.Message}\r\n{ex.StackTrace}\r\n{ex.Source}"); }
        }
        private ZmqSocketType GetSocketType(string socketType)
        {
            switch (socketType.ToUpper())
            {
                case "PUB":
                    return ZmqSocketType.Pub;
                case "SUB":
                    return ZmqSocketType.Sub;
                case "ROUTER":
                    return ZmqSocketType.Router;
                case "DEALER":
                    return ZmqSocketType.Dealer;
                default:
                    throw new ArgumentOutOfRangeException($"SocketType:({socketType}) out of range!");
            }
        }
    }
}
