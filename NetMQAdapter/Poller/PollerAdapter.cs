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
        private ConcurrentDictionary<string, ITimer> _timerDictionary { get; set; }
        public PollerAdapter()
        {
            _poller = new NetMQPoller();
            _socketDictionary = new ConcurrentDictionary<string, SocketAdapter>();
            _timerDictionary = new ConcurrentDictionary<string, ITimer>();
        }
        public ISocket AddSocket(SocketType socketType, string endPoint, bool isBind, string name, string identity = "", int dataLength = 4)
        {
            try
            {
                SocketAdapter socket = new SocketAdapter(GetSocketType(socketType), endPoint, isBind, identity, dataLength);
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
                if (_timerDictionary.ContainsKey(socket.ZMQName)) { _timerDictionary.TryUpdate(socket.ZMQName, timer, timer); }
                else { _timerDictionary.TryAdd(socket.ZMQName, timer); }
                return timer;
            }
            catch (Exception ex)
            {
                throw new NotImplementedException($"AddTimer: ISocket-Name: {socket.ZMQName} ---> {ex.Message}\r\n{ex.StackTrace}\r\n{ex.Source}");
            }
        }
        ~PollerAdapter() { Dispose(false); }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (_poller != null)
                {
                    StopAll();
                    _poller.Dispose();
                    _poller = null;
                }
            }
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
        public void Stop(string name)
        {
            try
            {
                if (_poller.IsRunning)
                {
                    if (_socketDictionary.ContainsKey(name))
                    {
                        _socketDictionary.TryRemove(name, out SocketAdapter socket);
                        socket.UnSubscribe();
                        socket.UnBindOrDisconnect();
                        _poller.RemoveAndDispose(socket._socket);
                    }
                    if (_timerDictionary.ContainsKey(name))
                    {
                        _timerDictionary.TryRemove(name, out ITimer timer);
                        _poller.Remove(timer.Timer);
                    }
                }
            }
            catch (Exception ex) { throw new NotImplementedException($"Stop ---> {ex.Message}\r\n{ex.StackTrace}\r\n{ex.Source}"); }
        }

        public void StopAll()
        {
            try
            {
                if (_poller.IsRunning)
                {
                    foreach (KeyValuePair<string, ITimer> item in _timerDictionary)
                    {
                        _poller.Remove(item.Value.Timer);
                    }
                    _timerDictionary.Clear();
                    foreach (KeyValuePair<string, SocketAdapter> item in _socketDictionary)
                    {
                        item.Value.UnSubscribe();
                        item.Value.UnBindOrDisconnect();
                        _poller.RemoveAndDispose(item.Value._socket);
                    }
                    _socketDictionary.Clear();
                    _poller.Stop();
                }
            }
            catch (Exception ex) { throw new NotImplementedException($"StopAll ---> {ex.Message}\r\n{ex.StackTrace}\r\n{ex.Source}"); }
        }
        private ZmqSocketType GetSocketType(SocketType socketType)
        {
            switch (socketType)
            {
                case SocketType.Router:
                    return ZmqSocketType.Router;
                case SocketType.Dealer:
                    return ZmqSocketType.Dealer;
                case SocketType.Pub:
                    return ZmqSocketType.Pub;
                case SocketType.Sub:
                    return ZmqSocketType.Sub;
                case SocketType.Request:
                    return ZmqSocketType.Req;
                case SocketType.Response:
                    return ZmqSocketType.Rep;
                default:
                    throw new ArgumentOutOfRangeException($"SocketType:({socketType}) out of range!");
            }
        }
    }
}
