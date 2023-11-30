using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class ChatClient {
  private readonly Socket clientSocket;
  private readonly Thread thReceive;
  private readonly ushort playerNumber;

  public event EventHandler<ChatPacket> ChatReceivedEvent;

  public ChatClient(ushort playerNumber) {
    this.playerNumber = playerNumber;

    clientSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    clientSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
    clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

    thReceive = new(Receive);
  }

  ~ChatClient() {
    Close();
  }

  public void Connect() {
    if(clientSocket != null && !clientSocket.Connected) {
      clientSocket.Connect(new IPEndPoint(IPAddress.Loopback, ChatConstants.SERVER_PORT));

      if(clientSocket.Connected) {
        Debug.Log("[Chat Client] Connected to the server, sending hello message...");

        ChatPacket helloPacket = new() {
          PacketType = PacketType.ClientManagementRequest,
          ClientManagementRequestType = ClientManagementRequestType.Hello,
          SenderPlayerNumber = playerNumber,
        };
        clientSocket.Send(helloPacket.ToBytes(), SocketFlags.None);
        thReceive.Start(clientSocket);
      } else {
        Debug.LogWarning("[Chat Client] Failed to connect to the server.");
      }

    } else {
      Debug.LogError("[Chat Client] Client is already connected to the server.");
    }
  }

  public void Close() {
    if(clientSocket.Connected) {
      thReceive.Abort();

      Debug.Log("[Chat Client] Sending bye message...");

      ChatPacket byePacket = new() {
        PacketType = PacketType.ClientManagementRequest,
        ClientManagementRequestType = ClientManagementRequestType.Bye,
        SenderPlayerNumber = playerNumber,
      };
      clientSocket.Send(byePacket.ToBytes(), SocketFlags.None);
    }

    clientSocket?.Close();

    Debug.Log("[Chat Client] Client socket closed.");
  }

  public void SendChat(ChatType type, string content) {
    ChatPacket packet = new() {
      PacketType = PacketType.Chat,
      SenderPlayerNumber = playerNumber,
      ChatType = type,
      Content = content,
    };

    clientSocket.Send(packet.ToBytes(), SocketFlags.None);
  }

  private void Receive(object obj) {
    if(obj == null || obj is not Socket) throw new ArgumentNullException();

    Socket clientSocket = (Socket)obj;
    byte[] buffer = new byte[ChatConstants.BUFFER_SIZE];
    while(true) {
      clientSocket.Receive(buffer);

      ProcessPacket(ChatPacket.FromBytes(buffer));
    }
  }

  private void ProcessPacket(ChatPacket packet) {
    if(packet == null) throw new ArgumentNullException();

    switch(packet.PacketType) {
      case PacketType.ServerManagementResponse:
        // TODO
        break;

      case PacketType.Chat:
        Debug.Log($"[Chat Client] Chat received, type = {packet.ChatType}, content = {packet.Content}");

        ChatReceivedEvent.Invoke(this, packet);

        /* if(packet.ChatType == ChatType.Text) {
          Debug.Log($"[Chat Client] Player #{packet.SenderPlayerNumber} ({playerSocket.RemoteEndPoint}) sent: {packet.Content}");

          foreach(var peer in peers) {
            if(peer.Item2 != playerSocket) {
              peer.Item2.Send(packet.ToBytes(), SocketFlags.None);
            }
          }
        } else if(packet.ChatType == ChatType.Sticker) {
          // TODO
        } */
        break;

      default:
        Debug.Log($"[Chat Client] Unknown packet type from Player #{packet.SenderPlayerNumber}, got {packet.PacketType}.");
        break;
    }
  }

  internal void OnChatReceived(Action<ChatPacket> value) {
    throw new NotImplementedException();
  }
}
