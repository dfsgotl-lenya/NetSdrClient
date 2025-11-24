// 1. Создаем интерфейс
public interface IDataReceiver
{
    Task<byte[]> ReceiveAsync(CancellationToken token);
}

// 2. Реализация для реальной сети
public class UdpDataReceiver : IDataReceiver
{
    private readonly UdpClient _client;

    public UdpDataReceiver(int port)
    {
        _client = new UdpClient(port);
    }

    public async Task<byte[]> ReceiveAsync(CancellationToken token)
    {
        var result = await _client.ReceiveAsync(token); // Используем асинхронность
        return result.Buffer;
    }
}

// 3. Основной класс теперь принимает интерфейс
public class SdrClient
{
    private readonly IDataReceiver _receiver;

    public SdrClient(IDataReceiver receiver)
    {
        _receiver = receiver; // Инъекция зависимости
    }
}
