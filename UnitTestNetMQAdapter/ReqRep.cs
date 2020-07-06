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
    public class ReqRep
    {
        [Test, Category("ReqRep")]
        public void TestReqRep()
        {
            int port1 = 55677;
            AutoResetEvent eventRaised = new AutoResetEvent(false);
            using (var poller = new PollerAdapter())
            {
                ISocket rep = poller.AddSocket(SocketType.Response, $"tcp://127.0.0.1:{port1}", true, "TestServer");
                ISocket req = poller.AddSocket(SocketType.Request, $"tcp://127.0.0.1:{port1}", false, "TestClient");
                byte[][] Response = { Encoding.UTF8.GetBytes("Topic A"), Encoding.UTF8.GetBytes("Message Conetxt") };
                byte[][] Request = { Encoding.UTF8.GetBytes("Topic A"), Encoding.UTF8.GetBytes("Message Request") };
                req.Received += (s, e) =>
                {
                    List<string> receiveData = ReqReceive(e.Item);
                    for (int i = 0; i < receiveData.Count; i++)
                    {
                        Assert.AreEqual(Encoding.UTF8.GetString(Response[i]), receiveData[i]);
                    }
                    eventRaised.Set();
                };
                rep.Received += (s, e) =>
                {
                    List<string> receiveData = ReqReceive(e.Item);
                    for (int i = 0; i < receiveData.Count; i++)
                    {
                        Assert.AreEqual(Encoding.UTF8.GetString(Request[i]), receiveData[i]);
                    }
                    rep.SendData(Response);
                };
                poller.Start();

                Thread.Sleep(1000);
                req.SendData(Request);
                Assert.IsTrue(eventRaised.WaitOne(2000), "No Receive any event");
                poller.StopAll();
            }
        }
        private List<string> ReqReceive(byte[][] item)
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
