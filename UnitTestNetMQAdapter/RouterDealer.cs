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
    public class RouterDealer
    {
        [Test, Category("RouterDealer")]
        public void TestRouterDealer()
        {
            int port1 = 55677;
            AutoResetEvent eventRaised = new AutoResetEvent(false);
            using (var poller = new PollerAdapter())
            {
                ISocket router = poller.AddSocket(SocketType.Router, $"tcp://127.0.0.1:{port1}", true, "TestServer", Guid.NewGuid().ToString());
                ISocket client = poller.AddSocket(SocketType.Dealer, $"tcp://127.0.0.1:{port1}", false, "TestClient", Guid.NewGuid().ToString());
                byte[][] clientData = { Encoding.UTF8.GetBytes("Hello") };
                string replay = "";
                router.Received += (s, e) =>
                {
                    byte[] identity = e.Item[0];
                    List<string> receiveData = RouterReceive(e.Item);
                    replay = "";
                    for (int i = 0; i < receiveData.Count; i++)
                    {
                        Assert.AreEqual(Encoding.UTF8.GetString(clientData[i]), receiveData[i]);
                        replay += receiveData[i];
                    }
                    byte[][] routerData = { e.Item[0], Encoding.UTF8.GetBytes("Receive->"), Encoding.UTF8.GetBytes(replay) };
                    router.SendData(routerData);
                };
                List<string> result = new List<string>();
                client.Received += (s, e) =>
                {
                    result = ClientReceive(e.Item);
                    eventRaised.Set();
                };
                poller.Start();

                client.SendData(clientData);

                Assert.AreEqual("TestClient", client.ZMQName);
                Assert.IsTrue(eventRaised.WaitOne(3000), "No Receive any event");
                Assert.AreEqual("Receive->", result[0]);
                Assert.AreEqual(replay, result[1]);
                poller.StopAll();
            }
        }
        [Test, Category("Timer")]
        public void TestRouterDealerTimer()
        {
            int port1 = 55677;
            int count = 0;
            AutoResetEvent eventRaised = new AutoResetEvent(false);
            using (var poller = new PollerAdapter())
            {
                ISocket router = poller.AddSocket(SocketType.Router, $"tcp://127.0.0.1:{port1}", true, "TestServer", Guid.NewGuid().ToString());
                ISocket client = poller.AddSocket(SocketType.Dealer, $"tcp://127.0.0.1:{port1}", false, "TestClient", Guid.NewGuid().ToString());
                ITimer heartBeat = poller.AddTimer(2, client);
                poller.Start();
                byte[][] heartBeatData = { Encoding.UTF8.GetBytes("ClientSend"), Encoding.UTF8.GetBytes("Alive") };
                heartBeat.SetTimerElapsed(heartBeatData);
                router.Received += (s, e) =>
                {
                    byte[] identity = e.Item[0];
                    List<string> receiveData = RouterReceive(e.Item);
                    Assert.AreEqual("ClientSend", receiveData[0]);
                    Assert.AreEqual("Alive", receiveData[1]);
                    count += 1;
                    byte[][] router1Data = { e.Item[0], Encoding.UTF8.GetBytes("Receive HeartBeat"), Encoding.UTF8.GetBytes($"{count}") };
                    router.SendData(router1Data);
                };
                List<string> result = new List<string>();
                client.Received += (s, e) =>
                {
                    result = ClientReceive(e.Item);
                    if (count == 2) eventRaised.Set();
                };
                Assert.IsTrue(eventRaised.WaitOne(5000), "No Receive any event");
                Assert.IsTrue(result[0].Contains("HeartBeat"), "No Receive any HeartBeat");
                Assert.AreEqual(count.ToString(), result[1]);
                poller.StopAll();
            }
        }
        private List<string> RouterReceive(byte[][] item)
        {
            List<string> result = new List<string>();
            for (int i = 1; i < item.Length; i++)
            {
                if (item[i] == null) { break; }
                else { result.Add(Encoding.UTF8.GetString(item[i])); }
            }
            return result;
        }
        private List<string> ClientReceive(byte[][] item)
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
