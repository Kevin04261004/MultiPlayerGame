using System;
using Unity.Collections;
using UnityEngine;

public enum EGamePacketType
{
    SocketType,
    ClientToServerPacketType,
    ServerToClientListPacketType,
    DataByteSize,
}
public enum ESocketType
{
    Undefined,
    Server,
    Client1,
    Client2,
    Client3,
    Client4,
}

public enum EClientToServerPacketType
{
    RequestConnect, // 서버에게 연결요청. 성공시) 자신은 클라이언트는 연결 안되는데, 서버는 클라와 연결됨.
    RequestDisconnect, // 서버에게 연결 해제 요청. 성공시) 서버로부터 sock을 닫아도 된다는 패킷이 날아옴. 그전에는 못 받고 강종해야 닫을 수 있음.
}

public enum EServerToClientListPacketType
{ // 해당 enum은 clientList에게만 보내는 것만 있는 것이 아니지만, 거의 다 리스트에게 보내는 패킷임.(앞에 Target붙으면 하나만.)
    TargetClientConnected, // 연결 요청을 보낸 클라이언트에게만 연결이 성공적으로 되었다고 전해줌. (다시 socket을 생성하지 못함.)
    TargetClientDisConnected, // 해제 요청을 보낸 클라이언트에게만 확인했고, 연결해제해도 된다고 전해줌.
    ClientConnected, // 모든 클라이언트에게 뒤에 오는 데이터(class)클라이언트가 연결되었다고 전해줌.
    ClientDisConnected, // 모든 클라이언트에게 뒤에 오는 데이터(class)클라이언트가 해제되었다고 전해줌.
    PlayerInfo,
}
public class GamePacket
{
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
    
    public byte[] ChangeToByte(BitField32 bitField32)
    {
        uint temp = bitField32.Value;
        byte[] arr = BitConverter.GetBytes(temp);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(arr);
        }
        return arr;
    }

    public BitField32 ChangeToBitField32(byte[] bytes)
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
    
    public void GetValueWithBitField32(in BitField32 bitField32, out ESocketType socketType, out uint data, out uint size)
    {
        socketType = (ESocketType)bitField32.GetBits(0, 3);
        data = bitField32.GetBits(3, 12);
        size = bitField32.GetBits(15, 10);
    }
    
    public void SetGamePacket(ref BitField32 bitField32, ESocketType socketType, int packetType, int size = 0)
    {
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

    private void SetBitWithValue(ref BitField32 bitField32, EGamePacketType packetType, int value)
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
    private void SetBit(ref BitField32 bitField32, int startPos, int size, int value)
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
}
