using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.Collections;
using UnityEngine;

public class Client : MonoBehaviour
{
    /* Server, Client Setting */
    private int _portNumber = 9001;
    private string _serverIP = "127.0.0.1";
    private bool _isSocketReady = false;
    private Socket sock;
    private IPEndPoint serverAddr;
    
    
    /* data */
    private BitField32 packet;
    private void Awake()
    {
        
        print(System.Runtime.InteropServices.Marshal.SizeOf(packet));
    }


    public void ConnectToServer()
    {
        if (_isSocketReady)
        {
            return;
        }
        
        try
        {
            /* 소켓 생성 및 초기화 */
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serverAddr = new IPEndPoint(IPAddress.Parse(_serverIP), _portNumber);
            print("UDP Client 소켓 생성 및 초기화 완료");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
        
        
    }
}
