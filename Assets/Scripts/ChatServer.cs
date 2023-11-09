using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;

public class ChatServer : MonoBehaviour
{
    public TMP_InputField _portNumInputField;

    private List<ServerClient> _clients;
    private List<ServerClient> _disconnectList;
    private TcpListener _server;
    private bool _isServerOn;
    private Chat _chat;
    private ChatClient _chatClient;

    private void Awake()
    {
        TryGetComponent(out _chat);
        TryGetComponent(out _chatClient);
    }

    public void ServerCreate()
    {
        _clients = new List<ServerClient>();
        _disconnectList = new List<ServerClient>();

        try
        {
            int port = string.IsNullOrEmpty(_portNumInputField.text) ? 9000 : int.Parse(_portNumInputField.text);
            _server = new TcpListener(IPAddress.Any, port);
            _server.Start();
            
            StartListening();
            _chat.AddMessage($"서버가 {port}에서 시작되었습니다.");
            _chatClient.ConnectToServer();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            _chat.AddMessage($"Socket Error: {ex.Message}");
        }
    }

    private void Update()
    {
        if (!_isServerOn)
        {
            return;
        }

        foreach (var client in _clients)
        {
            if (!IsConnected(client._client))
            {
                client._client.Close();
                _disconnectList.Add(client);
                continue;
            }
            else
            {
                NetworkStream stream = client._client.GetStream();
                if (stream.DataAvailable)
                {
                    string data = new StreamReader(stream, true).ReadLine();
                    if (data != null)
                    {
                        OnIncomingData(client, data);
                    }
                }
            }

            for (int i = 0; i < _disconnectList.Count - 1; i++)
            {
                Broadcast($"{_disconnectList[i]._clientName}님의 연결이 끊겼습니다", _clients);
                _clients.Remove(_disconnectList[i]);
                _disconnectList.RemoveAt(i);
            }
        }
    }

    private bool IsConnected(TcpClient client)
    {
        try
        {
            if (client != null && client.Client != null && client.Client.Connected)
            {
                if (client.Client.Poll(0, SelectMode.SelectRead))
                {
                    return !(client.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
                }

                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }
    private void StartListening()
    {
        _server.BeginAcceptTcpClient(AcceptTcpClient, _server);
        _isServerOn = true;
    }

    private void AcceptTcpClient(IAsyncResult ar)
    {
        TcpListener listener = (TcpListener)ar.AsyncState;
        _clients.Add(new ServerClient(listener.EndAcceptTcpClient(ar)));
        StartListening();
        
        Broadcast("%Name", new List<ServerClient>() {_clients[_clients.Count - 1]});
    }

    void OnIncomingData(ServerClient client, string data)
    {
        if (data.Contains("&Name"))
        {
            client._clientName = data.Split('|')[1];
            Broadcast($"{client._clientName}님이 연결되었습니다.", _clients);
            return;
        }
        Broadcast($"{client._clientName} : {data}", _clients);
    }
    private void Broadcast(string data, List<ServerClient> clients)
    {
        foreach (var client in clients)
        {
            try
            {
                StreamWriter writer = new StreamWriter(client._client.GetStream());
                writer.WriteLine(data);
                writer.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _chat.AddMessage($"$Write Error: {ex.Message}, {client._clientName}");
            }
        }
    }
}
