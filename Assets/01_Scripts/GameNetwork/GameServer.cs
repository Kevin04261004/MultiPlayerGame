using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TMPro;
using Unity.Collections;
using UnityEngine;

public class GameServer : MonoBehaviour
{
    /* Classes */
    private RoomManager _roomManager;
    private DataParser _dataParser;
    private DataManager _dataManager;
    private UIManager _uiManager;
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
    private char firstWord = '\0';
    [SerializeField] private int PointPerOneLetter = 100;
    
    /* UI */
    [SerializeField] private TextMeshProUGUI myIP_TMP;
    private void Awake()
    {
        _roomManager = FindAnyObjectByType<RoomManager>();
        _dataParser = FindAnyObjectByType<DataParser>();
        _dataManager = FindAnyObjectByType<DataManager>();
        _uiManager = FindAnyObjectByType<UIManager>();
        FindMyIP();
        myIP_TMP.text = _myIP;
    }

    [ContextMenu("Start Listen")]
    public void StartListening()
    {
        if (_canListen)
        {
            Debug.Log("서버가 이미 생성되어있음.");
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
            _sock.Bind(new IPEndPoint(IPAddress.Any, _portNumber));

            /* 모든 클라이언트의 데이터를 수신할 수 있게 설정 */
            EndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            print("서버 열었음.");
            /*  콜백 함수 시작. 비동기 읽기를 참조하는 IAsyncResult를 반환함.
                참고로 여기서 buf에 자동으로 받아줌.
            ref: https://learn.microsoft.com/ko-kr/dotnet/api/system.net.sockets.socket.beginreceivefrom?view=net-7.0
             */
            _sock.BeginReceiveFrom(_buf, 0, BUFSIZE, SocketFlags.None, ref clientEndPoint, ReceiveCallback, null);
            _dataParser.SetDataDictionary();
            _canListen = true;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }
    public void StartGame()
    {
        if (!_canListen)
        {
            return;
        }
        if(!_roomManager.IsAllReady())
        {
            return;
        }
        GamePacket.SetGamePacket(ref _packet, ESocketType.Server, (int)EServerToClientListPacketType.StartGame, 0);
        byte[] packetArr = GamePacket.ChangeToByte(in _packet);
        SendClientListData(packetArr);
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
            _sock?.BeginReceiveFrom(_buf, 0, BUFSIZE, SocketFlags.None, ref _peerEndPoint, ReceiveCallback, null);
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
                ProcessPacket_ClientToServer((EClientToServerPacketType)data, in receivedData, in socketType);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    
    private void ProcessPacket_ClientToServer(EClientToServerPacketType clientToServerPacketType, in byte[] receivedData, in ESocketType socketType = 0)
    {
        int size = 0;
        byte[] playerInfoArr;
        byte[] strArr;
        byte[] packetArr;
        byte[] tempArr;
        byte[] pointArr;
        string str;
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

                    bool[] temp = new bool[4];
                    for (int i = 0; i < temp.Length; ++i)
                    {
                        temp[i] = false;
                    }

                    for (int i = 0; i < _playerInfoList.Count; ++i)
                    {
                        temp[(int)_playerInfoList[i].socketType - 2] = true;
                    }

                    for (int i = 0; i < temp.Length; ++i)
                    {
                        if (temp[i] == false)
                        {
                            playerInfo.socketType = (ESocketType)i + 2;
                            break;
                        }
                    }
                    if (playerInfo.socketType == ESocketType.Undefined)
                    {
                        GamePacket.SetGamePacket(ref _packet, ESocketType.Server,(int)EServerToClientListPacketType.MaxRoom,0);
                        packetArr = GamePacket.ChangeToByte(in _packet);
                        SendTargetClientData(in packetArr, _peerEndPoint);
                        break;
                    }

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
                // 패킷 이후에 오는 데이터 처리. (플레이어 룸에서 제거 및 게임오브젝트 제거하라고 보내기)
                size = receivedData.Length - 4;
                playerInfoArr = new byte[size];
                Array.Copy(receivedData, 4, playerInfoArr, 0, size);
                playerInfo =  GamePlayerInfo.ChangeToGamePlayerInfo(playerInfoArr);
                GamePacket.SetGamePacket(ref _packet, ESocketType.Server,(int)EServerToClientListPacketType.ClientDisConnected,0);
                packetArr = GamePacket.ChangeToByte(in _packet);
                playerInfoArr = GamePlayerInfo.ChangeToBytes(playerInfo);
                GamePacket.AddBytesAfterPacket(out byte[] sendData3, in packetArr, in playerInfoArr);
                // 클라이언트 리스트에서 제거.
                SendClientListData(in sendData3);
                _clientList.Remove(_peerEndPoint);
                for (int i = 0; i < _playerInfoList.Count; ++i)
                {
                    if (_playerInfoList[i].playerName == playerInfo.playerName && _playerInfoList[i].socketType == playerInfo.socketType)
                    {
                        _playerInfoList.Remove(_playerInfoList[i]);
                        break;
                    }
                }
                // 소켓 꺼도 된다고 전달.
                GamePacket.SetGamePacket(ref _packet, ESocketType.Server, (int)EServerToClientListPacketType.TargetClientDisConnected, 0);
                packetArr = GamePacket.ChangeToByte(_packet);
                SendTargetClientData(packetArr, _peerEndPoint);
                break;
            case EClientToServerPacketType.SendWord:
                // 패킷 이후에 오는 데이터 처리.
                size = receivedData.Length - 4;
                if (size == 0)
                {
                    break;
                }
                strArr = new byte[size];
                Array.Copy(receivedData, 4, strArr, 0, size);
                // 디코딩
                str = Encoding.Default.GetString(strArr);
                EYellReturnType yellReturnType = _dataManager.YellWord(str);
                if (firstWord != str[0] && firstWord != '\0')
                {
                    // 앞 글자가 다름.
                    GamePacket.SetGamePacket(ref _packet, socketType, (int)EServerToClientListPacketType.DifferentFirstLetter, 0);
                    packetArr = GamePacket.ChangeToByte(_packet);
                    SendClientListData(packetArr);
                    break;
                }
                switch (yellReturnType)
                {
                    case EYellReturnType.Good:
                        GamePacket.SetGamePacket(ref _packet, socketType, (int)EServerToClientListPacketType.GoodWord, 0);
                        packetArr = GamePacket.ChangeToByte(_packet);
                        GamePacket.AddBytesAfterPacket(out byte[] sendData8, in packetArr, in strArr);
                        SendClientListData(sendData8);
                        firstWord = str[str.Length-1];
                        GamePacket.SetGamePacket(ref _packet, socketType, (int)EServerToClientListPacketType.SetFirstLetter, 0);
                        packetArr = GamePacket.ChangeToByte(_packet);
                        tempArr = Encoding.Default.GetBytes(firstWord.ToString());
                        GamePacket.AddBytesAfterPacket(out byte[] sendData7, in packetArr, in tempArr);
                        SendClientListData(sendData7);
                        GamePacket.SetGamePacket(ref _packet, socketType, (int)EServerToClientListPacketType.AddPoint, 0);
                        packetArr = GamePacket.ChangeToByte(_packet);
                        int point = str.Length * PointPerOneLetter;
                        pointArr = BitConverter.GetBytes(point);
                        GamePacket.AddBytesAfterPacket(out byte[] sendData5, in packetArr, in pointArr);
                        SendClientListData(sendData5);
                        break;
                    case EYellReturnType.NonWord:
                        GamePacket.SetGamePacket(ref _packet, socketType, (int)EServerToClientListPacketType.NoneWord, 0);
                        packetArr = GamePacket.ChangeToByte(_packet);
                        GamePacket.AddBytesAfterPacket(out byte[] sendData9, in packetArr, in strArr);
                        SendClientListData(sendData9);
                        break;
                    case EYellReturnType.UsedWord:
                        GamePacket.SetGamePacket(ref _packet, socketType, (int)EServerToClientListPacketType.UsedWord, 0);
                        packetArr = GamePacket.ChangeToByte(_packet);
                        GamePacket.AddBytesAfterPacket(out byte[] sendData10, in packetArr, in strArr);
                        SendClientListData(sendData10);
                        break;
                    default:
                        Debug.Assert(true, "Add Case");
                        break;
                }
                break;
            case EClientToServerPacketType.ChangeWord:
                // 패킷 이후에 오는 데이터 처리.
                size = receivedData.Length - 4;
                GamePacket.SetGamePacket(ref _packet, socketType,(int)EServerToClientListPacketType.WordChanged,0);
                packetArr = GamePacket.ChangeToByte(in _packet);
                if (size != 0)
                {
                    strArr = new byte[size];
                    Array.Copy(receivedData, 4, strArr, 0, size);
                    GamePacket.AddBytesAfterPacket(out byte[] sendData8, in packetArr, in strArr);
                    SendClientListData(in sendData8);
                }
                else
                {
                    SendClientListData(in packetArr);
                }
                break;
            case EClientToServerPacketType.RequestReady:
                GamePacket.SetGamePacket(ref _packet, socketType, (int)EServerToClientListPacketType.ReadyGame, 0);
                packetArr = GamePacket.ChangeToByte(_packet);
                for (int i = 0; i < _playerInfoList.Count; ++i)
                {
                    if (_playerInfoList[i].socketType == socketType)
                    {
                        _playerInfoList[i].isReady = true;
                    }
                }
                SendClientListData(packetArr);
                break;
            case EClientToServerPacketType.FailInputWord:
                GamePacket.SetGamePacket(ref _packet, socketType, (int)EServerToClientListPacketType.InputWordFailed, 0);
                packetArr = GamePacket.ChangeToByte(_packet);
                SendClientListData(packetArr);
                firstWord = '\0';
                GamePacket.SetGamePacket(ref _packet, socketType, (int)EServerToClientListPacketType.SetFirstLetter, 0);
                packetArr = GamePacket.ChangeToByte(_packet); 
                tempArr = Encoding.Default.GetBytes(firstWord.ToString());
                GamePacket.AddBytesAfterPacket(out byte[] sendData11, in packetArr, in tempArr);
                SendClientListData(sendData11);
                _dataManager.ResetDictionary();
                
                GamePacket.SetGamePacket(ref _packet, socketType, (int)EServerToClientListPacketType.MinusPoint, 0);
                packetArr = GamePacket.ChangeToByte(_packet);
                pointArr = BitConverter.GetBytes(_uiManager.GetMinusPoint());
                GamePacket.AddBytesAfterPacket(out byte[] sendData12, in packetArr, in pointArr);
                SendClientListData(sendData12);
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
            _sock.Close();
            _sock = null;
        }
    }
    [ContextMenu("내 아이피 찾기")]
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
        _myIP = temp;
        Debug.Log($"나의 IP: {_myIP}");
        return temp;
    }
}
