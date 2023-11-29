using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class GameServer : MonoBehaviour
{
    /* Server Setting */
    private int _portNumber = 9000;
    private string _myIP = "";
    private List<string> _clientList;
    private Socket _sock = null;
    private bool _canListen = false;
    private const int BUFSIZE = 1028;// 클라이언트가 보낼 수 있는 최대 바이트 = 4(packet) + 1024(string)
    private byte[] buf = new byte[BUFSIZE];
    private IPEndPoint anyAddr;
    
    [ContextMenu("Start Listen")]
    private void StartListening()
    {
        try
        {
            _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _sock.Bind(new IPEndPoint(IPAddress.Any, 9000));

            EndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            print("서버 열었음.");
            _sock.BeginReceiveFrom(buf, 0, BUFSIZE, SocketFlags.None, ref clientEndPoint, ReceiveCallback, null);

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
            // Socket is closed or not connected
            return;
        }
        try
        {
            EndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            int bytesRead = _sock.EndReceiveFrom(ar, ref clientEndPoint);
            
            print($"{bytesRead}만큼 데이터를 받았습니다.");
            if (bytesRead > 4)
            {
                byte[] receivedData = new byte[bytesRead];
                Array.Copy(buf, receivedData, bytesRead);

                // Extract packet header (first 4 bytes)
                byte[] packetHeader = new byte[4];
                Array.Copy(receivedData, 0, packetHeader, 0, 4);

                // Extract actual message data (after the header)
                int size = bytesRead - 4;
                byte[] messageData = new byte[size];
                Array.Copy(receivedData, 4, messageData, 0, size);
                print($"array size: {size}");
                string message = Encoding.Default.GetString(messageData);
                Debug.Log("Received message from client " + clientEndPoint.ToString());
                Debug.Log(message);
            }

            // Continue listening for more messages
            _sock.BeginReceiveFrom(buf, 0, BUFSIZE, SocketFlags.None, ref clientEndPoint, ReceiveCallback, null);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }
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
            _sock.Shutdown(SocketShutdown.Both);
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
