using NUnit.Framework;
using System;
using System.Text;
using System.Threading;
using NetMQAdapter.Poller;
using NetMQAdapter.Socket;
using NetMQAdapter.Timer;
using NetMQAdapter;
using System.Collections.Generic;

namespace UnitTestNetMQAdapter
{
    [TestFixture]
    public class PubSub
    {
        [Test, Category("PubSub")]
        public void TestPubSub()
        {
            int port1 = 55677;
            AutoResetEvent eventRaised = new AutoResetEvent(false);
            using (var poller = new PollerAdapter())
            {
                ISocket pub = poller.AddSocket(SocketType.Pub, $"tcp://127.0.0.1:{port1}", true, "TestServer");
                ISocket sub = poller.AddSocket(SocketType.Sub, $"tcp://127.0.0.1:{port1}", false, "TestClient");
                byte[][] pubAData = { Encoding.UTF8.GetBytes("Topic A"), Encoding.UTF8.GetBytes("Message Conetxt") };
                string[] topic = { "Topic A" };
                sub.Subscribe(topic);
                sub.Received += (s, e) =>
                {
                    List<string> receiveData = SubReceive(e.Item);
                    for (int i = 0; i < receiveData.Count; i++)
                    {
                        Assert.AreEqual(Encoding.UTF8.GetString(pubAData[i]), receiveData[i]);
                    }
                    eventRaised.Set();
                };
                poller.Start();

                Thread.Sleep(1000);
                pub.SendData(pubAData);
                Assert.IsTrue(eventRaised.WaitOne(2000), "No Receive any event");
                eventRaised.Reset();
                byte[][] pubBData = { Encoding.UTF8.GetBytes("Topic B"), Encoding.UTF8.GetBytes("Message Conetxt") };
                pub.SendData(pubBData);
                Assert.IsFalse(eventRaised.WaitOne(2000), "Subscribe has problem");
                poller.StopAll();
            }
        }
        [Test, Category("PubSubTimer")]
        public void TestPubSubTimer()
        {
            int port1 = 55677;
            AutoResetEvent eventRaised = new AutoResetEvent(false);
            using (var poller = new PollerAdapter())
            {
                ISocket pub = poller.AddSocket(SocketType.Pub, $"tcp://127.0.0.1:{port1}", true, "TestServer");
                ISocket sub = poller.AddSocket(SocketType.Sub, $"tcp://127.0.0.1:{port1}", false, "TestClient");
                ITimer heartBeat = poller.AddTimer(2, pub);
                byte[][] heartBeatData = { Encoding.UTF8.GetBytes("HeartBeat"), Encoding.UTF8.GetBytes("ClientSend"), Encoding.UTF8.GetBytes("Alive") };
                heartBeat.SetTimerElapsed(heartBeatData);
                string[] topic = { "HeartBeat" };
                sub.Subscribe(topic);
                sub.Received += (s, e) =>
                {
                    List<string> receiveData = SubReceive(e.Item);
                    for (int i = 0; i < receiveData.Count; i++)
                    {
                        Assert.AreEqual(Encoding.UTF8.GetString(heartBeatData[i]), receiveData[i]);
                    }
                    eventRaised.Set();
                };
                poller.Start();
                Thread.Sleep(1000);
                Assert.IsTrue(eventRaised.WaitOne(2000), "No Receive any event");
                poller.StopAll();
            }
        }
        private List<string> SubReceive(byte[][] item)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < item.Length; i++)
            {
                if (item[i] == null) { break; }
                else { result.Add(Encoding.UTF8.GetString(item[i])); }
            }
            return result;
        }
    }
}
