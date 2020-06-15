using NUnit.Framework;
using System;
using System.Text;
using System.Threading;
using NetMQAdapter.Poller;
using NetMQAdapter.Socket;
using NetMQAdapter.Timer;

namespace UnitTestNetMQAdapter
{
    [TestFixture]
    public class UnitTest
    {
        [Test, Category("RouterDealer")]
        public void TestRouterDealer()
        {
            using (var poller = new PollerAdapter())
            {
                int port1 = 55677;
                ISocket router = poller.AddSocket("router", $"tcp://127.0.0.1:{port1}", true, "TestServer", Guid.NewGuid().ToString());
                router.Received += (s, e) =>
                {
                    byte[] identity = e[0];
                    Assert.AreEqual("Hello", Encoding.UTF8.GetString(e[1]));
                    byte[][] router1Data = { e[0], Encoding.UTF8.GetBytes("Receive") };
                    router.SendData(router1Data);
                };
                ISocket client = poller.AddSocket("Dealer", $"tcp://127.0.0.1:{port1}", false, "TestClient",  Guid.NewGuid().ToString());
                poller.Start();
                string result = "";
                AutoResetEvent eventRaised = new AutoResetEvent(false);
                client.Received += (s, e) =>
                {
                    result = Encoding.UTF8.GetString(e[0]);
                    eventRaised.Set();
                };
                byte[][] client1Data = {Encoding.UTF8.GetBytes("Hello") };
                client.SendData(client1Data);
                Assert.AreEqual("TestClient", client.ZMQName);
                Assert.IsTrue(eventRaised.WaitOne(3000), "No Receive any event");
                Assert.AreEqual("Receive", result);
                poller.Stop();
            }
        }
        [Test, Category("Timer")]
        public void TestTimer()
        {
            using (var poller = new PollerAdapter())
            {
                int port1 = 55677;
                int count = 0;
                ISocket router = poller.AddSocket("router", $"tcp://127.0.0.1:{port1}", true, "TestServer", Guid.NewGuid().ToString());
                ISocket client = poller.AddSocket("Dealer", $"tcp://127.0.0.1:{port1}", false, "TestClient", Guid.NewGuid().ToString());
                ITimer heartBeat = poller.AddTimer(2, client);
                poller.Start();
                byte[][] heartBeatData = { Encoding.UTF8.GetBytes("ClientSend"), Encoding.UTF8.GetBytes("Alive") };
                heartBeat.SetTimerElapsed(heartBeatData);
                router.Received += (s, e) =>
                {
                    byte[] identity = e[0];
                    if (Encoding.UTF8.GetString(e[1]) == "ClientSend")
                    {
                        Assert.AreEqual("Alive", Encoding.UTF8.GetString(e[2]));
                        count += 1;
                        byte[][] router1Data = { e[0], Encoding.UTF8.GetBytes($"Receive HeartBeat {count}") };
                        router.SendData(router1Data);
                    }            
                };
                string result = "";
                AutoResetEvent eventRaised = new AutoResetEvent(false);
                client.Received += (s, e) =>
                {
                    result = Encoding.UTF8.GetString(e[0]);
                    eventRaised.Set();
                };
                Assert.IsTrue(eventRaised.WaitOne(2500), "No Receive any event");
                Assert.IsTrue(result.Contains("HeartBeat"), "No Receive any HeartBeat");
                poller.Stop();
            }
        }
    }
}
