## NetMQAdapter
A tool based on NetMQ4
## Using/Documentation
Before using NetMQAdapter, make sure to read the [ZeroMQ Guide](http://zguide.zeromq.org/page:all).

Here is a simple example:

```csharp
using (var poller = new PollerAdapter()) // Create a poller to control all sockets.
{
    ISocket server = poller.AddSocket("router", "tcp://localhost:5556", true, "TestServer", Guid.NewGuid().ToString()); // Bind
    ISocket client = poller.AddSocket("Dealer", "tcp://localhost:5556", false, "TestClient",  Guid.NewGuid().ToString()); // connect
    
    // Receive event on server.
    server.Received += (s, e) =>
    {
        // Receive the message from the server socket
        byte[] identity = e.Item[0];
        string receiveData = "";
        for(int i = 1; i < e.Item.Length; i++)
        {
            receiveData += Encoding.UTF8.GetString(e.Item[i]);
        }
        Console.WriteLine("From Client: {0}", receiveData);
        
        // Send a response back from the server
        byte[][] sendMessage = { e.Item[0], Encoding.UTF8.GetBytes("Hi Back") };
        server.SendData(sendMessage);
    };
    
    // Receive event on client.
    client.Received += (s, e) =>
    {
        // Receive the response from the client socket
        string result = "";
        for(int i = 0; i < e.Item.Length; i++)
        {
            result += Encoding.UTF8.GetString(e.Item[i]);
        }
        
        Console.WriteLine("From Server: {0}", result);
    };
    poller.StopAll();
}

```