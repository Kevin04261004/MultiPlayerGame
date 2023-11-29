using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Unity.Collections;
using UnityEngine;

public class GameClient : MonoBehaviour
{
    /* Server, Client Setting */
    private int _portNumber = 9000;
    private string _serverIP = "127.0.0.1";
    private bool _isSocketReady = false;
    private Socket sock;
    private IPEndPoint serverAddr;
    
    
    /* data */
    private BitField32 packet;
    private int returnVal;
    private void Awake()
    {
        /* 아래는 모두 테스트 코드 */
        print(System.Runtime.InteropServices.Marshal.SizeOf(packet));
        packet.Clear();
        packet.SetBits(0, true, 32);
        print(packet.GetBits(0,1));
        byte[] temp = ChangeStructToByte(packet);
        for (int i = 0; i < temp.Length; ++i)
        {
            print(temp[i]);
        }
    }

    private byte[] ChangeStructToByte<T>(T structT)
    {
        int size = Marshal.SizeOf(structT);
        byte[] arr = new byte[size];

        IntPtr structPtr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(structT,structPtr,true);
        
        Marshal.Copy(structPtr,arr,0,size);
        Marshal.FreeHGlobal(structPtr);

        return arr;
    }

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
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serverAddr = new IPEndPoint(IPAddress.Parse(_serverIP), _portNumber);
            print("UDP Client 소켓 생성 및 초기화 완료");
            
            /* 인코딩 */
            byte[] senddata = ChangeStructToByte(packet);
            /* 서버한테 자기 연결해달라는 패킷 보내기 */
            returnVal = sock.SendTo(senddata, 0, Marshal.SizeOf(packet), SocketFlags.None, serverAddr);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
        
        
    }
}
