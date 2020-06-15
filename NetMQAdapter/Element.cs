namespace NetMQAdapter
{
    public class Element
    {
        public delegate void ReceiveHandler(string name, byte[][] value);
    }
}
