using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;


namespace NetSdrClient.Transport
{
    public class UdpClientWrapper : IUdpClient, System.IDisposable
    {
    private readonly UdpClient _client;


    public UdpClientWrapper(IPEndPoint local)
    {
        _client = new UdpClient(local);
    }


    public int Send(byte[] data)
    {
        return _client.Send(data, data.Length);
    }


    public async Task<int> SendAsync(byte[] data, System.Threading.CancellationToken ct = default)
    {
        using var _ = ct.Register(() => _client.Close());
        var result = await _client.SendAsync(data, data.Length);
        return result;
    }


    public byte[] Receive()
    {
        var remote = new IPEndPoint(0,0);
        var data = _client.Receive(ref remote);
        return data;
    }


    public async Task<byte[]> ReceiveAsync(System.Threading.CancellationToken ct = default)
    {
        var res = await _client.ReceiveAsync(ct);
        return res.Buffer;
    }


    public void Close() => _client.Close();


    public void Dispose() => _client.Dispose();
    }
}
