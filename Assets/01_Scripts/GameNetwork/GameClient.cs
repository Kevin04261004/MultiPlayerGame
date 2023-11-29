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
    
    /* data */
    private BitField32 _packet;
    private ESocketType _socketType;
    private int _returnVal;
    
    // private byte[] ChangeStructToByte<T>(T structT)
    // {
    //     int size = Marshal.SizeOf(structT);
    //     byte[] arr = new byte[size];
    //
    //     IntPtr structPtr = Marshal.AllocHGlobal(size);
    //     Marshal.StructureToPtr(structT,structPtr,true);
    //     
    //     Marshal.Copy(structPtr,arr,0,size);
    //     Marshal.FreeHGlobal(structPtr);
    //
    //     return arr;
    // }

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
            print("UDP Client 소켓 생성 및 초기화 완료");
            
            /* 자기 연결해달라는 패킷으로 초기화 */
            _packet.Clear();
            _gamePacket.SetGamePacket(ref _packet,_socketType,(int)EClientToServerPacketType.RequestConnect,0);

            string temp = "Hello World";
            /* 인코딩 */
            byte[] packetArr = _gamePacket.ChangeToByte(_packet);
            byte[] messageArr = Encoding.Default.GetBytes(temp);
            byte[] sendData = new byte[packetArr.Length + messageArr.Length];
            Buffer.BlockCopy(packetArr,0,sendData,0,packetArr.Length);
            Buffer.BlockCopy(messageArr,0,sendData,packetArr.Length,messageArr.Length);
            /* 서버한테 자기 연결해달라는 패킷 보내기 */
            _returnVal = _sock.SendTo(sendData, 0, sendData.Length, SocketFlags.None, _serverAddr);
            _isSocketReady = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
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
