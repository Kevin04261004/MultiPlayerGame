using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using Unity.Collections;
using UnityEngine;

public class GameClient : MonoBehaviour
{

    /* Client Setting */
    private int _portNumber = 9000;
    private string _serverIP = "127.0.0.1";
    private bool _isSocketReady = false;
    private Socket _sock;
    private IPEndPoint _serverAddr;
    private GamePacket _gamePacket = new GamePacket();

    private EndPoint _peerEndPoint;
    /* data */
    private BitField32 _packet;
    private ESocketType _socketType;
    private int _returnVal;
    private const int BUFSIZE = 1028;
    private byte[] _buf = new byte[BUFSIZE];

    /* UI */
    [SerializeField] private TMP_InputField IPInputField;
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
            _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _serverAddr = new IPEndPoint(IPAddress.Parse(_serverIP), _portNumber);
            _peerEndPoint = new IPEndPoint(IPAddress.Any, 0);
            print("[Client] UDP Client 소켓 생성 및 초기화 완료");
            
            /* 자기 연결해달라는 패킷으로 초기화 */
            _packet.Clear();
            _gamePacket.SetGamePacket(ref _packet,_socketType,(int)EClientToServerPacketType.RequestConnect,0);

            string str = "Hello World";
            /* 인코딩 */
            byte[] packetArr = _gamePacket.ChangeToByte(_packet);
            int size = AddStringAfterPacket(out byte[] sendData, in packetArr, in str);
            
            // TEST CODE
            int strSize = size - 4;
            byte[] test = new byte[size];
            Array.Copy(sendData, 4, test, 0, strSize);
            string temp = Encoding.Default.GetString(test);
            Debug.Log($"[Client] size: {size} 를 보냈습니다.\ndata: {temp}" );
            
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
            return;
        }
        
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
                // 첫 4바이트는 패킷으로 고정이니까 읽어들이기.
                byte[] packetHeader = new byte[4];
                Array.Copy(receivedData, 0, packetHeader, 0, 4);
                
                //패킷 처리.
                ProcessPacket(packetHeader);
            }

            _sock.BeginReceiveFrom(_buf, 0, BUFSIZE, SocketFlags.None, ref _peerEndPoint, ReceiveCallBack,null);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            throw;
        }
    }
    private void ProcessPacket(byte[] packetHeader)
    {
        // 패킷 처리.
        BitField32 packet = _gamePacket.ChangeToBitField32(packetHeader);
        _gamePacket.GetValueWithBitField32(in packet, out ESocketType socketType, out uint data, out uint AfterDataSize);
        if (socketType != ESocketType.Server)
        {// 받은게 서버로부터 받은게 아니면 return
            Debug.Log("[Client] 소켓 타입이 서버가 아님.");
            return;
        }

        switch ((EServerToClientListPacketType)data)
        {// 서버로부터 받은 데이터 처리.
            case EServerToClientListPacketType.TargetClientConnected:
                _isSocketReady = true;
                Debug.Log("[Client] 소켓이 준비 됨.");
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
    private int AddStringAfterPacket(out byte[] sendData, in byte[] packetArr, in string str)
    {
        byte[] messageArr = Encoding.Default.GetBytes(str);
        sendData = new byte[packetArr.Length + messageArr.Length];
        Buffer.BlockCopy(packetArr,0,sendData,0,packetArr.Length);
        Buffer.BlockCopy(messageArr,0,sendData,packetArr.Length,messageArr.Length);
        return sendData.Length;
    }
    private void OnApplicationQuit() // 강종해도 꺼지게.
    {
        CloseClient();
    }
    public void CloseClient()
    {
        _sock?.Close();
    }
}
