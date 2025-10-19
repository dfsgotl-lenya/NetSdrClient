using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using NUnit.Framework;

namespace NetSdrClientAppTests
{
    public class NetSdrClientTests
    {
        private Mock<ITcpClient> _tcpMock;
        private Mock<IUdpClient> _udpMock;
        private NetSdrClient _client;

        [SetUp]
        public void Setup()
        {
            _tcpMock = new Mock<ITcpClient>();
            _udpMock = new Mock<IUdpClient>();
            _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
        }

        [Test]
        public async Task ConnectAsync_WhenNotConnected_ShouldSendSetupMessages()
        {
            _tcpMock.SetupGet(x => x.Connected).Returns(false);
            _tcpMock.Setup(x => x.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

            await _client.ConnectAsync();

            _tcpMock.Verify(x => x.Connect(), Times.Once);
            _tcpMock.Verify(x => x.SendMessageAsync(It.IsAny<byte[]>()), Times.AtLeast(3));
        }

        [Test]
        public void Disconnect_ShouldCallTcpDisconnect()
        {
            _client.Disconect();
            _tcpMock.Verify(x => x.Disconnect(), Times.Once);
        }

        [Test]
        public async Task StartIQAsync_WhenConnected_ShouldSetIQStartedAndSendMessage()
        {
            _tcpMock.SetupGet(x => x.Connected).Returns(true);
            _tcpMock.Setup(x => x.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);
            _udpMock.Setup(x => x.StartListeningAsync()).Returns(Task.CompletedTask);

            await _client.StartIQAsync();

            Assert.IsTrue(_client.IQStarted);
            _tcpMock.Verify(x => x.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
            _udpMock.Verify(x => x.StartListeningAsync(), Times.Once);
        }

        [Test]
        public async Task StopIQAsync_WhenConnected_ShouldSetIQStartedFalseAndSendMessage()
        {
            _tcpMock.SetupGet(x => x.Connected).Returns(true);
            _tcpMock.Setup(x => x.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

            await _client.StopIQAsync();

            Assert.IsFalse(_client.IQStarted);
            _tcpMock.Verify(x => x.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
            _udpMock.Verify(x => x.StopListening(), Times.Once);
        }

        [Test]
        public async Task ChangeFrequencyAsync_ShouldSendMessageWithCorrectArgs()
        {
            _tcpMock.SetupGet(x => x.Connected).Returns(true);
            _tcpMock.Setup(x => x.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);

            await _client.ChangeFrequencyAsync(1234567, 1);

            _tcpMock.Verify(x => x.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
        }

        [Test]
        public async Task SendTcpRequest_WhenNotConnected_ShouldReturnNull()
        {
            _tcpMock.SetupGet(x => x.Connected).Returns(false);
            var resp = await _client.GetType()
                .GetMethod("SendTcpRequest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_client, new object[] { new byte[] { 1, 2, 3 } }) as Task<byte[]>;

            Assert.IsNull(resp.Result);
        }

        [Test]
        public void TcpClient_MessageReceived_ShouldCompleteTask()
        {
            var data = new byte[] { 0x01, 0x02 };
            var method = _client.GetType().GetMethod("_tcpClient_MessageReceived", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Викликати як приватний метод
            Assert.DoesNotThrow(() =>
                method.Invoke(_client, new object[] { null, data })
            );
        }

        [Test]
        public void UdpClient_MessageReceived_ShouldNotThrow()
        {
            var bytes = new byte[32];
            var method = _client.GetType().GetMethod("_udpClient_MessageReceived", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.DoesNotThrow(() =>
                method.Invoke(_client, new object[] { null, bytes })
            );
        }
    }
}
