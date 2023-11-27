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
    public TMP_InputField _portNumInputField; // 포트 번호 인풋 필드

    private List<ServerClient> _clients; // 모든 클라이언트 리스트 (서버 포함)
    private List<ServerClient> _disconnectList; // 서버 끊긴 클라이언트들 리스트
    private TcpListener _server; // 서버
    private bool _isServerOn; // 서버가 켜져있는가?
    private Chat _chat; // Chat 스크립트
    private ChatClient _chatClient; // ChatClient 스크립트

    private void Awake()
    {
        TryGetComponent(out _chat); // 스크립트 초기화
        TryGetComponent(out _chatClient); // 스크립트 초기화2
    }

    public void ServerCreate()
    {
        _clients = new List<ServerClient>(); // 생성과 동시에 리스트 생성
        _disconnectList = new List<ServerClient>(); // 생성과 동시에 리스트 생성2

        try // 예외처리
        {
            /* 포트번호 얻어오기, 서버 생성, 서버 시작, 서버 리스닝, 서버 시작 채팅에 입력, 클라이언트 만들어서 서버에 Connect. */
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
        if (!_isServerOn) // 서버 켜져있지 않으면 리턴
        {
            return;
        }

        foreach (var client in _clients) // foreach문으로 모든 클라이언트에게.
        {
            if (!IsConnected(client._client)) // 연결이 꺼져있으면?
            {
                client._client.Close(); // 클라이언트 친절하게 꺼주고,
                _disconnectList.Add(client); // 꺼진 클라이언트 리스트에 추가해주기.
                continue;
            }
            else // 켜져있으면
            {
                NetworkStream stream = client._client.GetStream(); // 클라이언트에서 stream가져오기.
                if (stream.DataAvailable) // stream에서 데이터를 읽고 쓸 수 있으면,
                {
                    string data = new StreamReader(stream, true).ReadLine(); // 스트림에서 읽기.
                    if (data != null)
                    {
                        OnIncomingData(client, data); // 그리고 클라이언트한테 출력
                    }
                }
            }

            for (int i = 0; i < _disconnectList.Count - 1; i++)
            {
                Broadcast($"{_disconnectList[i]._clientName}님의 연결이 끊겼습니다", _clients); // 꺼진 얘들 꺼졌다고 말해주고
                _clients.Remove(_disconnectList[i]); // 꺼진거 리스트에서 제거하고
                _disconnectList.RemoveAt(i); // 옮기기
            }
        }
    }

    private bool IsConnected(TcpClient client) // 연결 되어 있는지 확인해주고 boolean리턴해주는 함수
    {
        try // 예외처리
        {
            if (client != null && client.Client != null && client.Client.Connected) // null이 아니거나 연결이 되어있으면
            {
                /* 소켓 상태를 확인해서 읽을 수 있는 상태인지 확인
                   참조 :  https://learn.microsoft.com/ko-kr/dotnet/api/system.net.sockets.socket.poll?view=net-7.0
                */
                if (client.Client.Poll(0, SelectMode.SelectRead))
                {
                    return !(client.Client.Receive(new byte[1], SocketFlags.Peek) == 0); // 읽을 수 있으면, 받고 리턴
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
    private void StartListening() // 서버 리스닝 시작하자.
    {
        _server.BeginAcceptTcpClient(AcceptTcpClient, _server); // tcp client들 listening해주는 매개변수임. 걍 외우거나 .net api보면서 사용.
        _isServerOn = true;
    }

    private void AcceptTcpClient(IAsyncResult ar) // 이해 안감. Async 동기화에 대해 더 공부해봐야할듯....
    {
        TcpListener listener = (TcpListener)ar.AsyncState;
        _clients.Add(new ServerClient(listener.EndAcceptTcpClient(ar))); 
        StartListening();
        
        Broadcast("%Name", new List<ServerClient>() {_clients[_clients.Count - 1]});
    }

    void OnIncomingData(ServerClient client, string data) // client랑 똑같은 함수
    {
        if (data.Contains("&Name"))
        {
            client._clientName = data.Split('|')[1];
            Broadcast($"{client._clientName}님이 연결되었습니다.", _clients);
            return;
        }
        Broadcast($"{client._clientName} : {data}", _clients);
    }
    private void Broadcast(string data, List<ServerClient> clients) // 클라이언트와 똑같음.2
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
