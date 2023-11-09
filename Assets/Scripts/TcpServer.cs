using System.Net.Sockets;

public class ServerClient
{
    public TcpClient _client;
    public string _clientName;

    public ServerClient(TcpClient client)
    {
        _clientName = "";
        _client = client;
    }
}
