using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class ChatServer {
  private const int SERVER_PORT = 9001;
  private const int CLIENT_LIMIT = 4;
  private const int BUFFER_SIZE = 512;

  private readonly List<Tuple<short, Socket, Thread>> peers;
  private readonly Socket serverSocket;

  private readonly Thread thAccept;

  public ChatServer() {
    peers = new(CLIENT_LIMIT);

    serverSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    serverSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
    serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

    thAccept = new(AcceptClient);
  }

  ~ChatServer() {
    Close();
  }

  public void StartListen() {
    if(serverSocket != null && !serverSocket.IsBound) {
      serverSocket.Bind(new IPEndPoint(IPAddress.Any, SERVER_PORT));
      serverSocket.Listen(CLIENT_LIMIT * 2);
      thAccept.Start(serverSocket);

      Debug.Log("[Chat Server] Server socket start listening.");
    } else {
      Debug.LogError("[Chat Server] Server socket is already bound.");
    }
  }

  public void Close() {
    foreach(var (_, socket, thread) in peers) {
      socket.Close();
      thread.Abort();
    }

    serverSocket?.Close();
  }

  private void AcceptClient(object obj) {
    if(obj == null || obj is not Socket) throw new ArgumentNullException();

    Socket serverSocket = (Socket)obj;

    while(true) {
      Socket clientSocket = serverSocket.Accept();

      Debug.Log($"[Chat Server] Client connected from {clientSocket.RemoteEndPoint}.");

      if(peers.Count < CLIENT_LIMIT) {
        Debug.Log("[Chat Server] Waiting for client hello request...");

        byte[] buffer = new byte[BUFFER_SIZE];
        clientSocket.Receive(buffer);

        ChatPacket packet = ChatPacket.FromBytes(buffer);
        if(!(packet.PacketType == PacketType.ClientManagementRequest
          && packet.ClientManagementRequestType == ClientManagementRequestType.Hello)) {
          Debug.LogWarning($"[Chat Server] Client {clientSocket.RemoteEndPoint} rejected, due to invalid hello request");
          clientSocket.Close();
          continue;
        }

        Debug.Log($"[Chat Server] Player #{packet.SenderPlayerNumber} ({clientSocket.RemoteEndPoint}) accepted");
        Thread thChatProcess = new(ChatProcess);
        Tuple<short, Socket, Thread> peer = new(packet.SenderPlayerNumber, clientSocket, thChatProcess);
        peers.Add(peer);
        thChatProcess.Start(peer);
      } else {
        Debug.LogWarning($"[Chat Server] Client {clientSocket.RemoteEndPoint} rejected, due to client count limit");
        clientSocket.Close();
      }
    }
  }

  private void ChatProcess(object obj) {
    if(obj == null || obj is not Tuple<short, Socket, Thread>) throw new ArgumentNullException();

    var (playerNumber, clientSocket, _) = (Tuple<short, Socket, Thread>)obj;

    while(true) {
      byte[] buffer = new byte[BUFFER_SIZE];
      int recv = clientSocket.Receive(buffer);

      if(recv == 0) {
        ClosePlayer(playerNumber);
        break;
      }

      ChatPacket packet = ChatPacket.FromBytes(buffer);
      Debug.Log($"[Chat Server] Player #{packet.SenderPlayerNumber} ({clientSocket.RemoteEndPoint}) sent: {packet.Content}");

      ProcessPacket(packet);
    }
  }

  private void ClosePlayer(short playerNumber) {
    var (_, playerSocket, playerChatThread) = peers.Find(peer => peer.Item1 == playerNumber);

    Debug.Log($"[Chat Server] Closing Player #{playerNumber} ({playerSocket.RemoteEndPoint}) connection.");

    if(playerSocket != null) {
      playerSocket.Close();
      peers.RemoveAll(peer => peer.Item2 == playerSocket);
    }

    if(playerChatThread != null) {
      playerChatThread.Abort();
    }
  } 

  private void ProcessPacket(ChatPacket packet) {
    if(packet == null) throw new ArgumentNullException();

    var (playerNumber, playerSocket, playerChatThread) = peers.Find(peer => peer.Item1 == packet.SenderPlayerNumber);

    switch(packet.PacketType) {
      case PacketType.ClientManagementRequest:
        if(packet.ClientManagementRequestType == ClientManagementRequestType.Bye) {
          ClosePlayer(playerNumber);
        }
        break;

      case PacketType.ServerManagementResponse:
        break;

      case PacketType.Chat:
        if(packet.ChatType == ChatType.Text) {
          Debug.Log($"[Chat Server] Player #{packet.SenderPlayerNumber} ({playerSocket.RemoteEndPoint}) sent: {packet.Content}");

          foreach(var peer in peers) {
            if(peer.Item2 != playerSocket) {
              peer.Item2.Send(packet.ToBytes(), SocketFlags.None);
            }
          }
        } else if(packet.ChatType == ChatType.Sticker) {
          // TODO
        }
        break;

      default:
        Debug.Log($"[Chat Server] Unknown packet type from Player #{packet.SenderPlayerNumber}, got {packet.PacketType}.");
        break;
    }
  }
}
