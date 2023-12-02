using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using Unity.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameClient : MonoBehaviour
{
    /* Classes */
    private RoomManager _roomManager;
    
    /* Client Setting */
    private int _portNumber = 9000;
    [SerializeField] private string _serverIP = "127.0.0.1";
    private bool _isSocketReady = false;
    private Socket _sock;
    private IPEndPoint _serverAddr;
    private EndPoint _peerEndPoint;
    
    /* data */
    private BitField32 _packet;
    private ESocketType _socketType;
    private int _returnVal;
    private const int BUFSIZE = 1028;
    private byte[] _buf = new byte[BUFSIZE];
    private PlayerManager myPlayerManager;
    /* UI */
    [SerializeField] private TMP_InputField IPInputField;

    private void Awake()
    {
        _roomManager = FindAnyObjectByType<RoomManager>();
    }

    public void ConnectToServer()
    {
        if (_isSocketReady)
        {
            Debug.Log("[Client] 소켓이 이미 생성됨!!!");
            return;
        }
        
        // 여기서 서버 아이피, 포트 넘버(9000고정) 초기화해주기
        
        try
        {
            if (!string.IsNullOrEmpty(IPInputField.text))
            {
                _serverIP = IPInputField.text;
            }
            /* 소켓 생성 및 초기화 */
            if (_sock == null)
            {
                _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }
            _serverAddr = new IPEndPoint(IPAddress.Parse(_serverIP), _portNumber);
            _peerEndPoint = new IPEndPoint(IPAddress.Any, 0);
            print("[Client] UDP Client 소켓 생성 및 초기화 완료");
            
            /* 자기 연결해달라는 패킷으로 초기화 */
            GamePacket.SetGamePacket(ref _packet,_socketType,(int)EClientToServerPacketType.RequestConnect,0);
            byte[] packetArr = GamePacket.ChangeToByte(in _packet);
            
            /* 플레이어 정보 보내기. */
            GamePlayerInfoData playerInfo = new()
            {
                socketType = ESocketType.Undefined,
                playerName = "Test" + Random.Range(100, 1000).ToString()
            };
            byte[] playerInfoArr = GamePlayerInfo.ChangeToBytes(in playerInfo);
            
            int size = GamePacket.AddBytesAfterPacket(out byte[] sendData, in packetArr, in playerInfoArr);
            /* 서버한테 자기 연결해달라는 패킷 보내기 */
            _sock.SendTo(sendData, 0, size, SocketFlags.None, _serverAddr);
            /* 콜백 함수 시작 */
            _sock.BeginReceiveFrom(_buf, 0, BUFSIZE, SocketFlags.None, ref _peerEndPoint,ReceiveCallBack,null);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }
    public void DisConnectToServer()
    {
        if (!_isSocketReady) // 소켓이 없으면 리턴.
        {
            Debug.Log("소켓이 존재 하지 않음!!!");
            return;
        }
        myPlayerManager = _roomManager.GetMyPlayerManagerOrNull();
        if (myPlayerManager == null)
        {
            return;
        }
        GamePacket.SetGamePacket(ref _packet, myPlayerManager.PlayerInfoData.socketType,(int)EClientToServerPacketType.RequestDisconnect);
        byte[] packetArr = GamePacket.ChangeToByte(in _packet);
        byte[] playerInfoArr = GamePlayerInfo.ChangeToBytes(myPlayerManager.PlayerInfoData);
        int size = GamePacket.AddBytesAfterPacket(out byte[] sendData,in packetArr, in playerInfoArr);
        _sock.SendTo(sendData, 0, size, SocketFlags.None, _serverAddr);
    }

    public void SendWord(in string word)
    {
        if (!_isSocketReady) // 소켓이 없으면 리턴.
        {
            Debug.Log("소켓이 존재 하지 않음!!!");
            return;
        }
        // 내 턴이 아니면 리턴
        myPlayerManager = _roomManager.GetMyPlayerManagerOrNull();
        if (myPlayerManager == null && !myPlayerManager.IsMyTurn())
        {
            return;
        }
        
        GamePacket.SetGamePacket(ref _packet, myPlayerManager.PlayerInfoData.socketType, (int)EClientToServerPacketType.SendWord);
        byte[] wordArr = Encoding.Default.GetBytes(word);
        byte[] packetArr = GamePacket.ChangeToByte(_packet);
        int size = GamePacket.AddBytesAfterPacket(out byte[] sendData, in packetArr, in wordArr);
        _sock.SendTo(sendData, 0, size, SocketFlags.None, _serverAddr);
    }

    public void RequestReady()
    {
        if (!_isSocketReady) // 소켓이 없으면 리턴.
        {
            Debug.Log("소켓이 존재 하지 않음!!!");
            return;
        }
        GamePacket.SetGamePacket(ref _packet, myPlayerManager.PlayerInfoData.socketType, (int)EClientToServerPacketType.RequestReady);
        byte[] packetArr = GamePacket.ChangeToByte(_packet);
        _sock.SendTo(packetArr, 0, packetArr.Length, SocketFlags.None, _serverAddr);
    }
    private void ReceiveCallBack(IAsyncResult ar)
    {
        if (_sock == null || !_sock.Connected)
        {
            // 소켓 없거나, 연결이 끊겼다면 return;
            return;
        }
        try
        {
            EndPoint recivedEndPoint = new IPEndPoint(IPAddress.Any, 0);
            
            int bytesRead = _sock.EndReceiveFrom(ar, ref recivedEndPoint);
            /* 만약 서버에서 온 데이터가 아니면 리턴. */
            if (!IsDataFromEndPoint(recivedEndPoint, _serverAddr))
            {
                return;
            }
            
            // 4바이트(패킷 크기) 이상 && 받은 데이터의 주소가 서버랑 같을때(서버에서 온 데이터일때)
            if (bytesRead >= 4) // 패킷을 받았다면,
            {
                // 배열 복사.(내가 읽을 용)
                byte[] receivedData = new byte[bytesRead];
                Array.Copy(_buf, receivedData, bytesRead);
                // 읽어들인 만큼 배열에서 제거.
                Array.Clear(_buf,0,bytesRead);
                //패킷 처리.
                ProcessPacket(in receivedData);
            }

            _sock?.BeginReceiveFrom(_buf, 0, BUFSIZE, SocketFlags.None, ref _peerEndPoint, ReceiveCallBack,null);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            throw;
        }
    }
    private void ProcessPacket(in byte[] receivedData)
    {
        // 첫 4바이트는 패킷으로 고정이니까 읽어들이기.
        byte[] packetHeader = new byte[4];
        Array.Copy(receivedData, 0, packetHeader, 0, 4);
        // 패킷 처리.
        BitField32 packet = GamePacket.ChangeToBitField32(in packetHeader);
        GamePacket.GetValueWithBitField32(in packet, out ESocketType socketType, out uint data, out uint AfterDataSize);

        // 아래 스위치문 겹치는거 많아서 그냥 지정하고 처리.
        int size = 0;
        byte[] playerInfoArr;
        GamePlayerInfoData playerInfo;
        
        switch ((EServerToClientListPacketType)data)
        {// 서버로부터 받은 데이터 처리.
            case EServerToClientListPacketType.TargetClientConnected:
                // 패킷 이후에 오는 데이터 처리.
                size = receivedData.Length - 4;
                playerInfoArr = new byte[size];
                Array.Copy(receivedData, 4, playerInfoArr, 0, size);
                // playerInfo
                playerInfo = GamePlayerInfo.ChangeToGamePlayerInfo(in playerInfoArr);
                Debug.Log($"{playerInfo.playerName}, {playerInfo.socketType}");
                MainThreadWorker.Instance.EnqueueJob(()=>_roomManager.InstantiatePlayer(in playerInfo, true));
                _isSocketReady = true;
                break;
            case EServerToClientListPacketType.ClientConnected:
                // 패킷 이후에 오는 데이터 처리.
                size = receivedData.Length - 4;
                playerInfoArr = new byte[size];
                Array.Copy(receivedData, 4, playerInfoArr, 0, size);
                // playerInfo
                playerInfo = GamePlayerInfo.ChangeToGamePlayerInfo(in playerInfoArr);
                Debug.Log($"{playerInfo.playerName}, {playerInfo.socketType}");
                MainThreadWorker.Instance.EnqueueJob(() => _roomManager.InstantiatePlayer(in playerInfo));
                break;
            case EServerToClientListPacketType.TargetClientDisConnected:
                CloseClient();
                break;
            case EServerToClientListPacketType.ClientDisConnected:
                // 패킷 이후에 오는 데이터 처리.
                size = receivedData.Length - 4;
                playerInfoArr = new byte[size];
                Array.Copy(receivedData, 4, playerInfoArr, 0, size);
                // playerInfo
                playerInfo = GamePlayerInfo.ChangeToGamePlayerInfo(in playerInfoArr);
                Debug.Log($"{playerInfo.playerName}, {playerInfo.socketType}가 게임을 종료하였습니다.");
                MainThreadWorker.Instance.EnqueueJob(()=>_roomManager.PlayerExit(playerInfo));  
                break;
            case EServerToClientListPacketType.AddPoint:
                size = receivedData.Length - 4;
                byte[] pointArr = new byte[size];
                Array.Copy(receivedData, 4, pointArr, 0, size);
                _roomManager.ProcessPacket((EServerToClientListPacketType)data,socketType, pointArr);
                break;
            case EServerToClientListPacketType.GoodWord:
            case EServerToClientListPacketType.NoneWord:
            case EServerToClientListPacketType.UsedWord:
            case EServerToClientListPacketType.DifferentFirstLetter:
            case EServerToClientListPacketType.ReadyGame:
                _roomManager.ProcessPacket((EServerToClientListPacketType)data,socketType);
                break;
            case EServerToClientListPacketType.StartGame:
                _roomManager.StartGame();
                break;
            default:
                Debug.Assert(true, "[Client] Add Case");
                break;
        }
    }
    private bool IsDataFromEndPoint(EndPoint endPoint, IPEndPoint fromIpEndPoint)
    {
        return endPoint.Equals(fromIpEndPoint);
    }
    private void OnApplicationQuit() // 강종해도 꺼지게.
    {
        CloseClient();
    }
    public void CloseClient()
    {
        _sock?.Close();
        _sock = null;
        _isSocketReady = false;
        Debug.Log("소켓을 종료함.");
    }
}
