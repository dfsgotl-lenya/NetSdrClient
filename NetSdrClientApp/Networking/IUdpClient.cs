using System.Diagnostics.CodeAnalysis;

namespace NetSdrClientApp.Networking
{
    public interface IUdpClient
    {
        [ExcludeFromCodeCoverage]
        event EventHandler<byte[]>? MessageReceived;

        Task StartListeningAsync();

        void StopListening();
        void Exit();
    }
}
