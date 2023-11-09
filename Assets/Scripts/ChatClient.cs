using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ChatClient : MonoBehaviour
{ 
    [SerializeField] private TMP_InputField _ipTMPInputField;
    [SerializeField] private TMP_InputField _portTMPInputField;
    [SerializeField] private TMP_InputField _nickNameTMPInputField;
    [SerializeField] private TMP_InputField _sendTMPInputField;
    private Chat _chat;
    private string _clientName;
    private bool _isSocketReady = false;
    private TcpClient _socket;
    private NetworkStream _stream;
    private StreamWriter _writer;
    private StreamReader _reader;
    
    private void Awake()
    {
        TryGetComponent(out _chat);
    }

    public void ConnectToServer()
    {
        if (_isSocketReady)
        {
            return;
        }

        string Ip = string.IsNullOrEmpty(_ipTMPInputField.text) ? "127.0.0.1" : _ipTMPInputField.text;
        int PortNum = string.IsNullOrEmpty(_portTMPInputField.text) ? 9000 : int.Parse(_portTMPInputField.text);

        try
        {
            _socket = new TcpClient(Ip, PortNum);
            _stream = _socket.GetStream();
            _writer = new StreamWriter(_stream);
            _reader = new StreamReader(_stream);
            _isSocketReady = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            _chat.AddMessage($"Socket Error : {ex.Message}");
        }
    }

    private void Update()
    {
        if (_isSocketReady && _stream.DataAvailable)
        {
            string data = _reader.ReadLine();
            if (!string.IsNullOrEmpty(data))
            {
                OnIncomingData(data);
            }
        }
    }

    private void OnIncomingData(string data)
    {
        if (data == "%Name")
        {
            _clientName = string.IsNullOrEmpty(_nickNameTMPInputField.text) ? "Guest" + Random.Range(1000,10000) : _nickNameTMPInputField.text;
            Send($"&Name|{_clientName}");
            return;
        }
        _chat.AddMessage(data);
    }

    private void Send(string data)
    {
        if (!_isSocketReady)
        {
            return;
        }
        
        _writer.WriteLine(data);
        _writer.Flush();
    }

    public void OnTextChange()
    {
        if (_sendTMPInputField.text.EndsWith("\n"))
        {
            SendButton();
        }
    }
    private void SendButton()
    {
#if (UNITY_EDITOR || UNITY_STANDALONE)
        if (!Input.GetButtonDown("Submit"))
        {
            return;            
        }
        _sendTMPInputField.ActivateInputField();
#endif
        if (string.IsNullOrEmpty(_sendTMPInputField.text.Trim()))
        {
            return;
        }
        Send(_sendTMPInputField.text);
        _sendTMPInputField.text = "";
    }

    private void OnApplicationQuit()
    {
        CloseSocket();
    }

    private void CloseSocket()
    {
        if (!_isSocketReady)
        {
            return;
        }
        _writer.Close();
        _reader.Close();
        _socket.Close();
        _isSocketReady = false;
    }
}