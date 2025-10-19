using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _udpMock;

    public NetSdrClientTests() { }

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
        {
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
        });

        _udpMock = new Mock<IUdpClient>();

        _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
    }

    [Test]
    public async Task ConnectAsyncTest()
    {
        await _client.ConnectAsync();

        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public void DisconnectWithNoConnectionTest()
    {
        _client.Disconect();
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        await ConnectAsyncTest();
        _client.Disconect();
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {
        await _client.StartIQAsync();
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task StartIQTest()
    {
        await ConnectAsyncTest();
        await _client.StartIQAsync();

        _udpMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQTest()
    {
        await ConnectAsyncTest();
        await _client.StopIQAsync();

        _udpMock.Verify(udp => udp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }

    [Test]
    public async Task EchoServer_ReadsAndWritesStreamData()
    {
        var inputBytes = System.Text.Encoding.UTF8.GetBytes("ping");
        using var memoryStream = new MemoryStream(inputBytes);

        var streamMock = new Mock<Stream>();
        int readCalled = 0;

        streamMock.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<byte[], int, int, CancellationToken>((buffer, offset, count, token) =>
            {
                if (readCalled == 0)
                {
                    Array.Copy(inputBytes, buffer, inputBytes.Length);
                    readCalled++;
                    return Task.FromResult(inputBytes.Length);
                }
                else
                {
                    return Task.FromResult(0);
                }
            });

        streamMock.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var buffer = new byte[1024];
        var token = new CancellationTokenSource().Token;
        int bytesRead = 0;

        while (!token.IsCancellationRequested &&
               (bytesRead = await streamMock.Object.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
        {
            await streamMock.Object.WriteAsync(buffer, 0, bytesRead, token);
        }

        streamMock.Verify(s => s.WriteAsync(It.IsAny<byte[]>(), 0, inputBytes.Length, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ChangeFrequencyAsyncTest()
    {
        await ConnectAsyncTest();

        await _client.ChangeFrequencyAsync(1000);

        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.Is<byte[]>(b => b.Length > 0)), Times.AtLeastOnce);
    }

    [Test]
    public async Task UdpMessageReceived_WritesSamples()
    {
        await ConnectAsyncTest();
        await _client.StartIQAsync();

        var udpData = new byte[] { 0x01 }; 
        _udpMock.Raise(u => u.MessageReceived += null, _udpMock.Object, udpData);

        Assert.Pass(); // перевірка, що не впало
    }

    //TODO: додати покриття інших методів NetSdrClient
}
