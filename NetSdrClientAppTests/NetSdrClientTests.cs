using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NUnit.Framework;
using System.Threading.Tasks;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class NetSdrClientTests
    {
        private NetSdrClient _client;
        private Mock<ITcpClient> _tcpMock;
        private Mock<IUdpClient> _udpMock;

        [SetUp]
        public void Setup()
        {
            _tcpMock = new Mock<ITcpClient>();

            // Поведение для Connect/Disconnect
            _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
            {
                _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
            });

            _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
            {
                _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
            });

            // Симуляция отправки сообщений
            _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
                .Returns(Task.CompletedTask)
                .Callback<byte[]>((bytes) =>
                {
                    _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
                });

            _udpMock = new Mock<IUdpClient>();
            _udpMock.Setup(udp => udp.StartListeningAsync()).Returns(Task.CompletedTask);

            _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
        }

        [Test]
        public async Task ConnectAsyncTest()
        {
            // act
            await _client.ConnectAsync();

            // assert
            _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
        }

        [Test]
        public void DisconnectWithNoConnectionTest()
        {
            // act
            _client.Disconect();

            // assert
            _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
        }

        [Test]
        public async Task DisconnectTest()
        {
            // Arrange 
            await _client.ConnectAsync();

            // act
            _client.Disconect();

            // assert
            _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
        }

        [Test]
        public async Task StartIQNoConnectionTest()
        {
            // act
            await _client.StartIQAsync();

            // assert
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
            _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
        }

        [Test]
        public async Task StartIQTest()
        {
            // Arrange 
            await _client.ConnectAsync();

            // act
            await _client.StartIQAsync();

            // assert
            _udpMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
            Assert.That(_client.IQStarted, Is.True);
        }

        [Test]
        public async Task StopIQTest()
        {
            // Arrange 
            await _client.ConnectAsync();

            // act
            await _client.StopIQAsync();

            // assert
            _udpMock.Verify(udp => udp.StopListening(), Times.Once);
            Assert.That(_client.IQStarted, Is.False);
        }

        [Test]
        public async Task ChangeFrequencyAsync_ShouldSendMessage_WhenConnected()
        {
            // Arrange
            await _client.ConnectAsync();

            // Act
            await _client.ChangeFrequencyAsync(145_000_000, 1);

            // Assert
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.AtLeastOnce);
        }

        [Test]
        public void TcpClient_MessageReceived_ShouldHandleEvent()
        {
            // Arrange
            var bytes = new byte[] { 0xAA, 0xBB, 0xCC };

            // Act
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);

            // Assert
            Assert.Pass("Handled TCP message without exception");
        }

        [Test]
        public void UdpClient_MessageReceived_ShouldHandleEvent()
        {
            // Arrange
            var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            // Act
            _udpMock.Raise(udp => udp.MessageReceived += null, _udpMock.Object, bytes);

            // Assert
            Assert.Pass("Handled UDP message without exception");
        }
    }
}
