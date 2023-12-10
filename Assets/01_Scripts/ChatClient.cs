using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class ChatClient {
  private readonly Socket clientSocket;
  private readonly Thread thReceive;
  private ushort playerNumber = 0;
  private readonly IPAddress serverIP;

  internal event EventHandler ChatSentEvent;
  internal event EventHandler<ChatContent> ChatReceivedEvent;
  internal event EventHandler<ushort> PlayerIdentifiedEvent;

  public ChatClient(string serverIP = null) {
    this.serverIP = serverIP != null ? IPAddress.Parse(serverIP) : IPAddress.Loopback;

    clientSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    clientSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

    thReceive = new(Receive);
  }

  ~ChatClient() {
    Close();
  }

  public void Connect() {
    if(clientSocket != null && !clientSocket.Connected) {
      clientSocket.Connect(new IPEndPoint(serverIP, ChatConstants.SERVER_PORT));

      if(clientSocket.Connected) {
        Debug.Log("[Chat Client] Connected to the server, sending hello message...");

        /* Client Hello */
        ChatPacket helloPacket = new() {
          PacketType = PacketType.ClientManagementRequest,
          ClientManagementRequestType = ClientManagementRequestType.Hello,
        };
        clientSocket.Send(helloPacket.ToBytes(), SocketFlags.None);

        /* Player identification after hello */
        Debug.Log("[Chat Client] Wait for server reply and player identification...");
        byte[] buffer = new byte[ChatConstants.BUFFER_SIZE];
        clientSocket.Receive(buffer);
        ChatPacket packet = ChatPacket.FromBytes(buffer);

        if(packet.ServerManagementResponseType == ServerManagementResponseType.PlayerIdentify) {
          Debug.Log("[Chat Client] Player identified, starting receive thread.");
          playerNumber = ushort.Parse(packet.ContentString);
          thReceive.Start(clientSocket);
        } else {
          Debug.LogError("[Chat Client] Not received server reply during player identification!");
        }
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
      };
      clientSocket.Send(byePacket.ToBytes(), SocketFlags.None);
    }

    clientSocket?.Close();

    Debug.Log("[Chat Client] Client socket closed.");
  }

  public void SendChat(ChatType type, string content) {
    if(playerNumber <= 0) throw new InvalidOperationException("Player not identified (invalid player number)");

    ChatPacket packet = new() {
      PacketType = PacketType.Chat,
      Content = (new ChatContent {
        ChatType = type,
        PlayerNumber = playerNumber,
        Content = content,
      }).ToBytes(),
    };

    try {
      clientSocket.Send(packet.ToBytes(), SocketFlags.None);
      ChatSentEvent.Invoke(this, EventArgs.Empty);
    } catch(Exception ex) {
      Debug.LogException(ex);
    }
  }

  private void Receive(object obj) {
    if(obj == null || obj is not Socket) throw new ArgumentNullException();

    Socket clientSocket = (Socket)obj;
    while(true) {
      ChatUtils.ReceiveAll(clientSocket, out byte[] buffer);

      ProcessPacket(ChatPacket.FromBytes(buffer));
    }
  }

  private void ProcessPacket(ChatPacket packet) {
    if(packet == null) throw new ArgumentNullException();

    switch(packet.PacketType) {
      case PacketType.Chat:
        ChatContent chat = ChatContent.FromBytes(packet.Content);

        Debug.Log($"[Chat Client] Chat received, type = {chat.ChatType}, content = {packet.ContentString}");

        ChatReceivedEvent.Invoke(this, chat);

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
        Debug.Log($"[Chat Client] Unknown packet type from server, got {packet.PacketType}.");
        break;
    }
  }
}
