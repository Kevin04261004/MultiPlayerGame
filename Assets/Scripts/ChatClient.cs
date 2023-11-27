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
    [SerializeField] private TMP_InputField _ipTMPInputField; // 아이피 주소
    [SerializeField] private TMP_InputField _portTMPInputField; // 포트 번호
    [SerializeField] private TMP_InputField _nickNameTMPInputField; // 닉네임
    [SerializeField] private TMP_InputField _sendTMPInputField; // 입력해서 서버에 보내는 인풋필드
    private Chat _chat; // 스크립트
    private string _clientName; // 클라이언트의 이름
    private bool _isSocketReady = false; // 소켓이 이미 생성된 상태인가?
    private TcpClient _socket; // 소켓
    private NetworkStream _stream; // 네트워크 통신에 필요한 stream (읽고 쓰기 위함)
    private StreamWriter _writer; // Writer
    private StreamReader _reader; // Reader
    
    private void Awake()
    {
        TryGetComponent(out _chat); // Chat 스크립트를 자기 자신에게서 가져옵니다. 예외처리는 안함.
        // 여기서 TryGetComponent를 굳이 사용하는 이유는 GC를 발생시키지 않음과 동시에 bool값을 리턴하기에 예외처리를 할 수 있습니다.
    }

    public void ConnectToServer()
    {
        if (_isSocketReady) // 소켓이 생성된 상태라면 리턴
        {
            return;
        }
        /* 아이피와 포트넘버를 인풋필드 변수들로 부터 얻어옵니다. 여기서 string.IsNullOrEmpty함수를 사용하여 비어있으면 자기 자신과 포트번호 9000을 넣습니다. */
        string Ip = string.IsNullOrEmpty(_ipTMPInputField.text) ? "127.0.0.1" : _ipTMPInputField.text;
        int PortNum = string.IsNullOrEmpty(_portTMPInputField.text) ? 9000 : int.Parse(_portTMPInputField.text);

        try // 통신에는 많은 예외가 있기에 try catch finally문으로 예외처리
        {
            _socket = new TcpClient(Ip, PortNum); // 소켓 생성
            _stream = _socket.GetStream(); // 스트림 가져오기
            _writer = new StreamWriter(_stream); // 가져온 스트림으로 writer 만들기
            _reader = new StreamReader(_stream); // 가져온 스트림으로 reader 만들기
            _isSocketReady = true; // 소켓 생성 성공
        }
        catch (Exception ex) // 예외처리문 chat에 출력. 여기서 버그 발생!!! <- 버그가 발생하면 채팅에 글이 안써지는 버그가 있음. 왜인지는 못 찾음 ㅠ
        {
            Console.WriteLine(ex);
            _chat.AddMessage($"Socket Error : {ex.Message}");
        }
    }

    private void Update()
    {
        if (_isSocketReady && _stream.DataAvailable) // 만약 소켓이 생성되어있고, 스트림을 읽을 수 있거나 값이 있다면
        {
            string data = _reader.ReadLine(); // reader로 stream(버퍼) 읽고, data에 저장
            if (!string.IsNullOrEmpty(data)) // data가 널이 아닐때,
            {
                OnIncomingData(data); // 함수 실행.
            }
        }
    }

    private void OnIncomingData(string data)
    {
        if (data == "%Name") // 만약 데이터가 "%Name"이면 이름으로 &Name|{_clientName}으로 바꾸고 Send()함수를 써서 stream에 다시 쓰고 return; 아니면 채팅에 추가.
        {
            _clientName = string.IsNullOrEmpty(_nickNameTMPInputField.text) ? "Guest" + Random.Range(1000,10000) : _nickNameTMPInputField.text;
            Send($"&Name|{_clientName}");
            return;
        }
        _chat.AddMessage(data);
    }

    private void Send(string data)
    {
        if (!_isSocketReady) // 소켓 생성 안되어있으면 return;
        {
            return;
        }
        
        _writer.WriteLine(data); // data를 임시 버퍼에 적음.
        _writer.Flush(); // Flush()함수를 사용.
        /*
         * 자, Flush()함수가 무엇이냐?
         * 바로, Close()함수를 사용하여 닫지 않고 현재 입력된 버퍼를 BaseStream으로 옮긴다는 뜻이다.
         * 참조: https://planek.tistory.com/40
         */
    }

    public void OnTextChange() // TMP InputField에서 변경사항마다 OnClick처럼 Event를 호출하여 사용하는 함수이다.
    {
        if (_sendTMPInputField.text.EndsWith("\n")) // 마지막이 '\n'으로 끝나면 Enter을 누른것이니 SendButton()함수 호출
        {
            SendButton();
        }
    }
    private void SendButton()
    {
#if (UNITY_EDITOR || UNITY_STANDALONE)
        if (!Input.GetButtonDown("Submit")) // 없어도 되지 않나?
        {
            return;            
        }
        _sendTMPInputField.ActivateInputField(); // 포커스 주기.
#endif
        if (string.IsNullOrEmpty(_sendTMPInputField.text.Trim())) // 널이 아닐때
        {
            return;
        }
        Send(_sendTMPInputField.text); // Send()함수로 stream에 옮기기.
        _sendTMPInputField.text = ""; // null로 만들기.
    }

    private void OnApplicationQuit()
    {
        CloseSocket(); // 나가면 소켓 종료
    }

    private void CloseSocket()
    {
        if (!_isSocketReady) // 소켓이 생성되있지 않으면 리턴
        {
            return;
        }
        _writer.Close(); // 해제1
        _reader.Close(); // 해제2
        _socket.Close(); // 해제3
        _isSocketReady = false; // false;
    }
}