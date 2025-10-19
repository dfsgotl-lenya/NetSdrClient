using System;
using System.Net;
using System.Threading.Tasks;
using NetSdrClientApp;
using NUnit.Framework;

namespace NetSdrClientAppTests
{
    public class UdpClientWrapperTests
    {
        private UdpClientWrapper _client;
        private IPEndPoint _localEndPoint;

        [SetUp]
        public void Setup()
        {
            _localEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            _client = new UdpClientWrapper(_localEndPoint);
        }

        [TearDown]
        public void Cleanup()
        {
            _client?.Disconnect();
        }

        [Test]
        public void Connect_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => _client.Connect(new IPEndPoint(IPAddress.Loopback, 9000)));
        }

        [Test]
        public void Disconnect_AfterConnect_ShouldNotThrow()
        {
            _client.Connect(new IPEndPoint(IPAddress.Loopback, 9000));
            Assert.DoesNotThrow(() => _client.Disconnect());
        }

        [Test]
        public async Task SendMessageAsync_WithData_ShouldNotThrow()
        {
            _client.Connect(new IPEndPoint(IPAddress.Loopback, 9000));
            var data = new byte[] { 1, 2, 3, 4 };
            Assert.DoesNotThrowAsync(async () => await _client.SendMessageAsync(data));
        }

        [Test]
        public void Disconnect_WithoutConnect_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => _client.Disconnect());
        }

        [Test]
        public void Connect_InvalidEndpoint_ShouldThrow()
        {
            var invalidEndPoint = new IPEndPoint(IPAddress.None, 0);
            Assert.Throws<Exception>(() => _client.Connect(invalidEndPoint));
        }
    }
}
