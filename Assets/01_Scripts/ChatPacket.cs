using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

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

public enum ServerManagementResponseType {
  NotSpecified = 0,
  PlayerIdentify = 1
}

public enum ChatType {
  NotSpecified = 0,
  Text = 1,
  Sticker = 2
}

[Serializable]
internal class SerializableClass {
  protected SerializableClass() { }

  internal byte[] ToBytes() {
    var binFormatter = new BinaryFormatter();
    using var stream = new MemoryStream();
    
    try {
      binFormatter.Serialize(stream, this);
      return stream.ToArray();
    } catch(Exception ex) {
      Debug.LogException(ex);
      return null;
    }
  }
  internal string ToJson() {
    return JsonUtility.ToJson(this);
  }

  internal static SerializableClass FromBytes(byte[] bytes) {
    var binFormatter = new BinaryFormatter();
    using var stream = new MemoryStream(bytes);

    try {
      return binFormatter.Deserialize(stream) as SerializableClass;
    } catch(Exception ex) {
      Debug.LogException(ex);
      return null;
    }
  }
}

[Serializable]
internal class ChatPacket: SerializableClass {
  public PacketType PacketType { get; set; }
  public ClientManagementRequestType ClientManagementRequestType { get; set; } = ClientManagementRequestType.NotSpecified;
  public ServerManagementResponseType ServerManagementResponseType { get; set; } = ServerManagementResponseType.NotSpecified;
  public byte[] Content { get; set; }

  internal string ContentString {
    get => Encoding.Default.GetString(Content);
    set => Content = Encoding.Default.GetBytes(value);
  }

  internal static new ChatPacket FromBytes(byte[] bytes) {
    return SerializableClass.FromBytes(bytes) as ChatPacket;
  }
}

[Serializable]
internal class ChatContent: SerializableClass {
  public ushort PlayerNumber { get; set; }
  public ChatType ChatType { get; set; }
  public string Content { get; set; }

  internal static new ChatContent FromBytes(byte[] bytes) {
    return SerializableClass.FromBytes(bytes) as ChatContent;
  }
}
