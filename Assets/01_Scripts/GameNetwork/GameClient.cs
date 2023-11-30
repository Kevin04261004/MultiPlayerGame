using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
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
    private byte[] buf = new byte[BUFSIZE];
    

    [ContextMenu("Connect To Server Test")]
    public void ConnectToServer()
    {
        if (_isSocketReady)
        {
            return;
        }
        
        // 여기서 서버 아이피, 포트 넘버(9000고정) 초기화해주기
        
        try
        {
            /* 소켓 생성 및 초기화 */
            _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _serverAddr = new IPEndPoint(IPAddress.Parse(_serverIP), _portNumber);
            _peerEndPoint = new IPEndPoint(IPAddress.Any, 0);
            print("UDP Client 소켓 생성 및 초기화 완료");
            
            /* 자기 연결해달라는 패킷으로 초기화 */
            _packet.Clear();
            _gamePacket.SetGamePacket(ref _packet,_socketType,(int)EClientToServerPacketType.RequestConnect,0);

            string str = "Hello World";
            /* 인코딩 */
            byte[] packetArr = _gamePacket.ChangeToByte(_packet);
            int size = AddStringAfterPacket(out byte[] sendData, in packetArr, in str);
           
            /* 서버한테 자기 연결해달라는 패킷 보내기 */
            _sock.SendTo(sendData, 0, size, SocketFlags.None, _serverAddr);
            
            /* 콜백 함수 시작 */
            _sock.BeginReceiveFrom(buf, 0, BUFSIZE, SocketFlags.None, ref _peerEndPoint,ReceiveCallBack,null);
            // _isSocketReady = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
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
            int byteRead = _sock.EndReceiveFrom(ar, ref recivedEndPoint);
            
            // 4바이트 이상 && 받은 데이터의 주소가 서버랑 같을때(서버에서 온 데이터일때)
            if (byteRead >= 4 && IsDataFromEndPoint(recivedEndPoint, _serverAddr))
            {
                Debug.Log("서버에서 데이터가 옴.");
                Debug.Log("연결됨.");
            }

            _sock.BeginReceiveFrom(buf, 0, BUFSIZE, SocketFlags.None, ref _peerEndPoint, ReceiveCallBack,null);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            throw;
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
