using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.Collections;
using UnityEngine;

public class GameServer : MonoBehaviour
{
    /* Classes */
    private RoomManager _roomManager;
    
    /* Server Setting */
    private int _portNumber = 9000;
    private string _myIP = "";
    private List<EndPoint> _clientList = new List<EndPoint>();
    private Socket _sock = null;
    private bool _canListen = false;
    private const int BUFSIZE = 1028;// 클라이언트가 보낼 수 있는 최대 바이트 = 4(packet) + 1024(string)
    private byte[] _buf = new byte[BUFSIZE];
    private IPEndPoint _anyAddr;
    
    /* Data */
    private EndPoint _peerEndPoint;
    private BitField32 _packet;
    private List<GamePlayerInfoData> _playerInfoList = new List<GamePlayerInfoData>();

    private void Awake()
    {
        _roomManager = FindAnyObjectByType<RoomManager>();
    }

    [ContextMenu("Start Listen")]
    public void StartListening()
    {
        if (_canListen)
        {
            return;
        }
        try
        {
            /* 서버 소켓 생성 및 초기화 */
            _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            /*  ReuseAddress 옵션 활성화.
             *  ref: https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.setsocketoption?view=net-8.0
             */
            _sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            /* 바인딩 */
            _sock.Bind(new IPEndPoint(IPAddress.Any, 9000));

            /* 모든 클라이언트의 데이터를 수신할 수 있게 설정 */
            EndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            print("서버 열었음.");
            /*  콜백 함수 시작. 비동기 읽기를 참조하는 IAsyncResult를 반환함.
                참고로 여기서 buf에 자동으로 받아줌.
            ref: https://learn.microsoft.com/ko-kr/dotnet/api/system.net.sockets.socket.beginreceivefrom?view=net-7.0
             */
            _sock.BeginReceiveFrom(_buf, 0, BUFSIZE, SocketFlags.None, ref clientEndPoint, ReceiveCallback, null);

            _canListen = true;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        if (_sock == null || !_sock.Connected)
        {
            // 소켓 없거나, 연결이 끊겼다면 return;
            return;
        }
        try
        {
            // 모든 클라이언트에서 마음껏 접근 가능.
            _peerEndPoint = new IPEndPoint(IPAddress.Any, 0);
            // 비동기 읽기를 끝냄.
            int bytesRead = _sock.EndReceiveFrom(ar, ref _peerEndPoint);
            if (bytesRead >= 4) // 패킷을 받았다면,
            {
                // 배열 복사.(내가 읽을 용)
                byte[] receivedData = new byte[bytesRead];
                Array.Copy(_buf, receivedData, bytesRead);
                // 읽어들인 만큼 배열에서 제거.
                Array.Clear(_buf,0,bytesRead);
                ProcessPacket(in receivedData);
            }

            // 다시 콜백 받을 수 있게.
            _sock.BeginReceiveFrom(_buf, 0, BUFSIZE, SocketFlags.None, ref _peerEndPoint, ReceiveCallback, null);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
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

        switch (socketType)
        {
            case ESocketType.Undefined:
                ProcessPacket_ClientToServer((EClientToServerPacketType)data, in receivedData);
                break;
            case ESocketType.Server:
                break;
            case ESocketType.Client1:
            case ESocketType.Client2:
            case ESocketType.Client3:
            case ESocketType.Client4:
                ProcessPacket_ClientToServer((EClientToServerPacketType)data, in receivedData);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    
    private void ProcessPacket_ClientToServer(EClientToServerPacketType clientToServerPacketType, in byte[] receivedData)
    {
        int size = 0;
        byte[] playerInfoArr;
        byte[] packetArr;
        GamePlayerInfoData playerInfo;
        switch (clientToServerPacketType)
        {
            case EClientToServerPacketType.RequestConnect:
                // clientEndPoint가 있고, 이가 clientList에 존재하지 않을때.
                if (_peerEndPoint != null &&!_clientList.Contains(_peerEndPoint))
                {
                    // 리스트에 추가해주고
                    _clientList.Add(_peerEndPoint);

                    // 패킷 이후에 오는 데이터 처리.
                    size = receivedData.Length - 4;
                    playerInfoArr = new byte[size];
                    Array.Copy(receivedData, 4, playerInfoArr, 0, size);
                    // playerInfo 지정
                    playerInfo = GamePlayerInfo.ChangeToGamePlayerInfo(in playerInfoArr);
                    playerInfo.socketType = (ESocketType)_clientList.Count + 1;
                    _playerInfoList.Add(playerInfo);
                    
                    // 해당 클라이언트에게 연결되었다는 정보 보내기.
                    GamePacket.SetGamePacket(ref _packet, ESocketType.Server,(int)EServerToClientListPacketType.TargetClientConnected,0);
                    packetArr = GamePacket.ChangeToByte(in _packet);
                    playerInfoArr = GamePlayerInfo.ChangeToBytes(playerInfo);
                    GamePacket.AddBytesAfterPacket(out byte[] sendData, in packetArr, in playerInfoArr);
                    SendTargetClientData(in sendData, _peerEndPoint);
                    Debug.Log("[Server] 연결되었음을 해당 client에게 보냄.");
                    
                    // 전부에게 다시 보내기.
                    GamePacket.SetGamePacket(ref _packet, ESocketType.Server,(int)EServerToClientListPacketType.ClientConnected,size);
                    packetArr = GamePacket.ChangeToByte(in _packet);
                    for (int i = 0; i < _playerInfoList.Count; ++i)
                    {
                        playerInfoArr = GamePlayerInfo.ChangeToBytes(_playerInfoList[i]);
                        GamePacket.AddBytesAfterPacket(out byte[] sendData2, in packetArr, in playerInfoArr);
                        SendClientListData(in sendData2);
                    }
                }
                break;
            case EClientToServerPacketType.RequestDisconnect:
                // 클라이언트 리스트에서 제거.
                _clientList.Remove(_peerEndPoint);
                // 소켓 꺼도 된다고 전달.
                GamePacket.SetGamePacket(ref _packet, ESocketType.Server, (int)EServerToClientListPacketType.TargetClientDisConnected, 0);
                
                // 패킷 이후에 오는 데이터 처리. (플레이어 룸에서 제거 및 게임오브젝트 제거하라고 보내기)
                size = receivedData.Length - 4;
                playerInfoArr = new byte[size];
                Array.Copy(receivedData, 4, playerInfoArr, 0, size);
                playerInfo =  GamePlayerInfo.ChangeToGamePlayerInfo(playerInfoArr);
                GamePacket.SetGamePacket(ref _packet, ESocketType.Server,(int)EServerToClientListPacketType.ClientDisConnected,0);
                packetArr = GamePacket.ChangeToByte(in _packet);
                playerInfoArr = GamePlayerInfo.ChangeToBytes(playerInfo);
                GamePacket.AddBytesAfterPacket(out byte[] sendData3, in packetArr, in playerInfoArr);
                SendClientListData(in sendData3);
                break;
            default:
                Debug.Assert(true, "Add Case");
                break;
        }
    }
    private void SendClientListData(in byte[] data)
    {
        for (int i = 0; i < _clientList.Count; ++i)
        {
            _sock.SendTo(data, 0, data.Length, SocketFlags.None, _clientList[i]);
        }
    }
    private void SendTargetClientData(in byte[] data, in EndPoint target)
    {
        _sock.SendTo(data, 0, data.Length,SocketFlags.None,target);
    }
    private void OnApplicationQuit()
    {
        CloseServer();
    }
    public void CloseServer()
    {
        /* 여기다가 모든 클라이언트한테 데이터 전송. */
        if (_sock != null)
        {
            _canListen = false;
            //_sock.Shutdown(SocketShutdown.Both);
            _sock.Close();
        }
    }
    private string FindMyIP()
    {
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        string temp = string.Empty;
        for (int i = 0; i < host.AddressList.Length; ++i)
        {
            if (host.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
            {
                temp = host.AddressList[i].ToString();
                break;
            }
        }
        return temp;
    }
}
