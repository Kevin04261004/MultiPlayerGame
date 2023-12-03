using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using System.Text;
using UnityEngine;

public enum EGamePacketType
{
    SocketType,
    ClientToServerPacketType,
    ServerToClientListPacketType,
    DataByteSize,
}
public enum ESocketType
{// 순서 변경 못합니다. 이걸로 int 변환해서 연산합니다.
    Undefined,
    Server,
    Client1,
    Client2,
    Client3,
    Client4,
}

public enum EClientToServerPacketType
{
    RequestConnect, // 서버에게 연결요청. 성공시) 자신은 클라이언트는 연결 안되는데, 서버는 클라와 연결됨. + 뒤에는 플레이어 정보가 보내짐.
    RequestDisconnect, // 서버에게 연결 해제 요청. 성공시) 서버로부터 sock을 닫아도 된다는 패킷이 날아옴. 그전에는 못 받고 강종해야 닫을 수 있음.
    SendWord, // 서버에게 단어 보내기. 서버는 판단해서 실패, 성공 여부 반환.
    RequestReady, // 서버에게 준비완료 보내기.
    ChangeWord, // 서버한테 변경된 단어 보내기.
    FailInputWord, // 입력 실패.
}

public enum EServerToClientListPacketType
{ // 해당 enum은 clientList에게만 보내는 것만 있는 것이 아니지만, 거의 다 리스트에게 보내는 패킷임.(앞에 Target붙으면 하나만.)
    TargetClientConnected, // 연결 요청을 보낸 클라이언트에게만 연결이 성공적으로 되었다고 전해줌. (다시 socket을 생성하지 못함.)
    TargetClientDisConnected, // 해제 요청을 보낸 클라이언트에게만 확인했고, 연결해제해도 된다고 전해줌.
    ClientConnected, // 모든 클라이언트에게 뒤에 오는 데이터(class)클라이언트가 연결되었다고 전해줌. + 뒤에는 플레이어 정보가 보내짐.
    ClientDisConnected, // 모든 클라이언트에게 뒤에 오는 데이터(class)클라이언트가 해제되었다고 전해줌.
    NoneWord, // 존재하지 않은 단어를 적음. (socket타입을 해당 클라이언트로 지정)
    UsedWord, // 이미 사용한 단어를 적음. (socket타입을 해당 클라이언트로 지정)
    DifferentFirstLetter, // 첫 번째 글자가 다름. (socket타입을 해당 클라이언트로 지정)
    GoodWord, // 글자가 맞음. (socket타입을 해당 클라이언트로 지정)
    AddPoint, // 점수 추가 (socket타입을 해당 클라이언트로 지정) 뒤에 데이터는 int형(추가 점수)로 들어옴.
    StartGame, // 게임 시작.
    ReadyGame, // socket타입의 플레이어가 레디를 누름.
    MaxRoom, // 룸에 인원수가 꽉 참.
    SetFirstLetter, // 첫번째 단어를 세팅하라고 전해줌.
    WordChanged, // 인풋필드 내의 단어가 변경됨
    InputWordFailed, // 단어 적는걸 실패함.
    MinusPoint // 점수 마이너스 (socket타입을 해당 클라이언트로 지정) 뒤에 데이터는 int형(추가 점수)로 들어옴.
}
public class GamePacket
{
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
    public static byte[] ChangeToByte(in BitField32 bitField32)
    {
        uint temp = bitField32.Value;
        byte[] arr = BitConverter.GetBytes(temp);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(arr);
        }
        return arr;
    }
    public static BitField32 ChangeToBitField32(in byte[] bytes)
    {
        BitField32 returnBitField32;
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        uint temp = BitConverter.ToUInt32(bytes, 0);
        returnBitField32.Value = temp;
        return returnBitField32;
    }
    public static void GetValueWithBitField32(in BitField32 bitField32, out ESocketType socketType, out uint data, out uint size)
    {
        socketType = (ESocketType)bitField32.GetBits(0, 3);
        data = bitField32.GetBits(3, 12);
        size = bitField32.GetBits(15, 10);
    }
    public static void SetGamePacket(ref BitField32 bitField32, ESocketType socketType, int packetType, int size = 0)
    {
        bitField32.Clear();
        SetBitWithValue(ref bitField32, EGamePacketType.SocketType, (int)socketType);
        if (socketType == ESocketType.Server) // Server
        {
            SetBitWithValue(ref bitField32, EGamePacketType.ServerToClientListPacketType, packetType);
        }
        else // Client
        {
            SetBitWithValue(ref bitField32, EGamePacketType.ClientToServerPacketType, packetType);
        }
        SetBitWithValue(ref bitField32, EGamePacketType.DataByteSize, size);
    }
    private static void SetBitWithValue(ref BitField32 bitField32, EGamePacketType packetType, int value)
    {
        switch (packetType)
        {
            case EGamePacketType.SocketType:
                SetBit(ref bitField32, 0, 3, value);
                break;
            case EGamePacketType.ClientToServerPacketType:
            case EGamePacketType.ServerToClientListPacketType:
                SetBit(ref bitField32, 3, 12, value);
                break;
            case EGamePacketType.DataByteSize:
                SetBit(ref bitField32, 15, 10, value);
                break;
            default:
                Debug.Assert(true, "add case");
                break;
        }
    }
    private static void SetBit(ref BitField32 bitField32, int startPos, int size, int value)
    {
        for (int i = 0; i < size; ++i)
        {
            int pos = startPos + i;
            if (value % 2 == 0)
            {
                bitField32.SetBits(pos, false, 1);
            }
            else
            {
                bitField32.SetBits(pos, true, 1);
            }
            value /= 2;
        }
    }
    public static int AddStringAfterPacket(out byte[] sendData, in byte[] packetArr, in string str)
    {
        byte[] messageArr = Encoding.Default.GetBytes(str);
        sendData = new byte[packetArr.Length + messageArr.Length];
        Buffer.BlockCopy(packetArr,0,sendData,0,packetArr.Length);
        Buffer.BlockCopy(messageArr,0,sendData,packetArr.Length,messageArr.Length);
        return sendData.Length;
    }
    public static int AddBytesAfterPacket(out byte[] sendData, in byte[] packetArr, in byte[] bytes)
    {
        sendData = new byte[packetArr.Length + bytes.Length];
        Buffer.BlockCopy(packetArr,0,sendData,0,packetArr.Length);
        Buffer.BlockCopy(bytes,0,sendData,packetArr.Length,bytes.Length);
        return sendData.Length;
    }
}
