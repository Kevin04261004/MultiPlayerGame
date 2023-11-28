using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public enum PacketType {
  ClientManagementRequest  = 1,
  ServerManagementResponse = 2,
  Chat = 3
}

public enum ClientManagementRequestType {
  NotSpecified = 0,
  Hello = 1,
  Bye = 2
}

public enum ChatType {
  NotSpecified = 0,
  Text = 1,
  Sticker = 2
}

[Serializable]
public class ChatPacket {
  public PacketType PacketType { get; set; }
  public ClientManagementRequestType ClientManagementRequestType { get; set; } = ClientManagementRequestType.NotSpecified;
  public ChatType ChatType { get; set; } = ChatType.NotSpecified;
  public short SenderPlayerNumber { get; set; }
  public string Content { get; set; }

  public byte[] ToBytes() {
    var binFormatter = new BinaryFormatter();
    using var stream = new MemoryStream();
    binFormatter.Serialize(stream, this);
    return stream.ToArray();
  }

  public static ChatPacket FromBytes(byte[] bytes) {
    var binFormatter = new BinaryFormatter();
    using var stream = new MemoryStream(bytes);
    return (ChatPacket)binFormatter.Deserialize(stream);
  }
}
