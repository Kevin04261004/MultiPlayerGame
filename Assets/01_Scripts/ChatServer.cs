using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

using Peer = System.Tuple<ushort, System.Net.Sockets.Socket, System.Threading.Thread>;

public class ChatServer {
  private readonly List<Peer> peers;
  private readonly Socket serverSocket;

  private readonly Thread thAccept;

  public ChatServer() {
    peers = new(ChatConstants.CLIENT_LIMIT);

    serverSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) {
      ReceiveBufferSize = (int)ChatConstants.BUFFER_SIZE,
      SendBufferSize = (int)ChatConstants.BUFFER_SIZE,
      NoDelay = true, /* serverSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true); */
    };
    serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

    thAccept = new(AcceptClient);
  }

  ~ChatServer() {
    Close();
  }

  public void StartListen() {
    if(serverSocket != null && !serverSocket.IsBound) {
      serverSocket.Bind(new IPEndPoint(IPAddress.Any, ChatConstants.SERVER_PORT));
      serverSocket.Listen(ChatConstants.CLIENT_LIMIT * 2);
      thAccept.Start(serverSocket);

      Debug.Log("[Chat Server] Server socket start listening.");
    } else {
      Debug.LogError("[Chat Server] Server socket is already bound.");
    }
  }

  public void Close() {
    thAccept.Abort();

    foreach(var (_, socket, thread) in peers) {
      socket.Close();
      thread.Abort();
    }
    peers.Clear();

    serverSocket?.Close();

    Debug.Log("[Chat Server] Server socket closed.");
  }

  private void AcceptClient(object obj) {
    if(obj == null || obj is not Socket) throw new ArgumentNullException();

    Socket serverSocket = (Socket)obj;

    while(true) {
      Socket clientSocket = serverSocket.Accept();

      Debug.Log($"[Chat Server] Client connected from {clientSocket.RemoteEndPoint}.");

      if(peers.Count < ChatConstants.CLIENT_LIMIT) {
        Debug.Log("[Chat Server] Waiting for client hello request...");

        /* Wait for client hello */
      byte[] buffer = new byte[ChatConstants.BUFFER_SIZE];
        clientSocket.Receive(buffer);

        ChatPacket packet = ChatPacket.FromBytes(buffer);
        if(!(packet.PacketType == PacketType.ClientManagementRequest
          && packet.ClientManagementRequestType == ClientManagementRequestType.Hello)) {
          Debug.LogWarning($"[Chat Server] Client {clientSocket.RemoteEndPoint} rejected, due to invalid hello request");
          clientSocket.Close();
          continue;
        }

        Debug.Log($"[Chat Server] Client {clientSocket.RemoteEndPoint} accepted");

        /* Send player identification after hello */
        ushort newPlayerNumber = GetAvailablePlayerNumber();
        Debug.Log($"[Chat Server] Client {clientSocket.RemoteEndPoint} will be assigned as player number: #{newPlayerNumber}, send identification");
        clientSocket.Send((new ChatPacket() {
          PacketType = PacketType.ServerManagementResponse,
          ServerManagementResponseType = ServerManagementResponseType.PlayerIdentify,
          ContentString = newPlayerNumber.ToString(),
        }).ToBytes());

        /* Start peer chat process */
        Thread thChatProcess = new(ChatProcess);
        Peer peer = new(newPlayerNumber, clientSocket, thChatProcess);
        peers.Add(peer);
        thChatProcess.Start(peer);
      } else {
        Debug.LogWarning($"[Chat Server] Client {clientSocket.RemoteEndPoint} rejected, due to client count limit");
        clientSocket.Close();
      }
    }
  }

  private ushort GetAvailablePlayerNumber() {
    for(ushort i = 1; i <= ChatConstants.CLIENT_LIMIT; i++) {
      if(!peers.Exists(peer => peer.Item1 == i)) {
        return i;
      }
    }

    return 0;
  }

  private ushort GetPlayerNumber(Socket socket) {
    return peers.Find(peer => peer.Item2 == socket).Item1;
  }

  private void ChatProcess(object obj) {
    if(obj == null || obj is not Peer) throw new ArgumentNullException();

    var (playerNumber, clientSocket, _) = (Peer)obj;

    while(true) {
      int recv = ChatUtils.ReceiveAll(clientSocket, out byte[] buffer);

      if(recv == 0) {
        ClosePlayer(playerNumber);
        break;
      }

      ChatPacket packet = ChatPacket.FromBytes(buffer);
      if(packet != null) {
        Debug.Log($"[Chat Server] Received valid packet form player #{playerNumber} ({clientSocket.RemoteEndPoint})");

        ProcessPacket((Peer)obj, packet);
      } else {
        Debug.LogWarning($"[Chat Server] Received invalid packet form player #{playerNumber} ({clientSocket.RemoteEndPoint}). Ignoring.");
      }
    }
  }

  private void ClosePlayer(ushort playerNumber) {
    var (_, playerSocket, playerChatThread) = peers.Find(peer => peer.Item1 == playerNumber);

    Debug.Log($"[Chat Server] Closing Player #{playerNumber} ({playerSocket.RemoteEndPoint}) connection.");

    if(playerSocket != null) {
      playerSocket.Close();
      peers.RemoveAll(peer => peer.Item2 == playerSocket);
    }

    playerChatThread?.Abort();
  } 

  private void ProcessPacket(Peer peer, ChatPacket packet) {
    if(packet == null) throw new ArgumentNullException();

    var (playerNumber, playerSocket, _) = peer;

    switch(packet.PacketType) {
      case PacketType.ClientManagementRequest:
        if(packet.ClientManagementRequestType == ClientManagementRequestType.Bye) {
          ClosePlayer(playerNumber);
        }
        break;

      case PacketType.Chat:
        ChatContent chat = ChatContent.FromBytes(packet.Content);

        if(chat.ChatType == ChatType.Text) {
          Debug.Log($"[Chat Server] Player #{playerNumber} ({playerSocket.RemoteEndPoint}) sent: {chat.Content}");

          foreach(var p in peers) {
            p.Item2.Send(packet.ToBytes(), SocketFlags.None);
          }
        } else if(chat.ChatType == ChatType.Sticker) {
          // TODO
        }
        break;

      default:
        Debug.Log($"[Chat Server] Unknown packet type from Player #{playerNumber}, got {packet.PacketType}.");
        break;
    }
  }
}
