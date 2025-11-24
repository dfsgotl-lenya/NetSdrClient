namespace NetSdrClient.Transport
{
    public interface IUdpClient
    {
        int Send(byte[] data);
        System.Threading.Tasks.Task<int> SendAsync(byte[] data, System.Threading.CancellationToken ct = default);
        byte[] Receive();
        System.Threading.Tasks.Task<byte[]> ReceiveAsync(System.Threading.CancellationToken ct = default);
        void Close();
    }
}
