using System.IO;
using System.Net.Sockets;
using UnityEngine;

public static class ChatConstants {
  public const uint BUFFER_SIZE = 512;
  public const ushort SERVER_PORT = 9001;
  public const ushort CLIENT_LIMIT = 4;
}

public static class ChatUtils {
  public static int ReceiveAll(Socket socket, out byte[] data) {
    using var ms = new MemoryStream();
    byte[] buffer = new byte[socket.ReceiveBufferSize];

    // Receive until empty
    int received = 0;
    do {
      received = socket.Receive(buffer, socket.ReceiveBufferSize, SocketFlags.None);
      ms.Write(buffer, 0, received);
    } while(socket.Available > 0);

    data = ms.ToArray();
    return received;
  }
}
