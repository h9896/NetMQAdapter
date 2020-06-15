using System;
using NetMQ;
using NetMQAdapter.Socket;

namespace NetMQAdapter.Timer
{
    internal class TimerAdapter : ITimer
    {
        public NetMQTimer Timer { get; private set; }
        private ISocket _socket { get; set; }
        internal TimerAdapter(int interval, ISocket socket)
        {
            Timer = GetNetMQTimer(interval);
            _socket = socket;
        }
        public void SetTimerElapsed(byte[][] data)
        {
            Timer.Elapsed += (s, e) => Timer_Elapsed(s, e, data);
        }
        private void Timer_Elapsed(object sender, NetMQTimerEventArgs e, byte[][] data)
        {
            try
            {
                _socket.SendData(data);
            }
            catch (Exception ex) { throw new NotImplementedException($"Timer_Elapsed: ZMQName {_socket.ZMQName} ---> {ex.Message}\r\n{ex.StackTrace}\r\n{ex.Source}"); }
        }
        private NetMQTimer GetNetMQTimer(int seconds)
        {
            return new NetMQTimer(TimeSpan.FromSeconds(seconds));
        }
    }
}
