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
    Client,
    Server,
    Client1,
    Client2,
    Client3,
    Client4,
}

public enum EClientToServerPacketType
{
    RequestConnect,
}

public enum EServerToClientListPacketType
{
    
}
public class GamePacket
{
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
        uint temp = BitConverter.ToUInt32(bytes, 0);
        returnBitField32.Value = temp;
        return returnBitField32;
    }
    
    public void GetValueWithBitField32(in BitField32 bitField32, out ESocketType socketType, out EClientToServerPacketType clientToServerPacketType, out EServerToClientListPacketType serverToClientListPacketType, out uint size)
    {
        socketType = (ESocketType)bitField32.GetBits(0, 3);
        clientToServerPacketType = (EClientToServerPacketType)bitField32.GetBits(3, 6);
        serverToClientListPacketType = (EServerToClientListPacketType)bitField32.GetBits(9, 6);
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
                SetBit(ref bitField32, 3, 6, value);
                break;
            case EGamePacketType.ServerToClientListPacketType:
                SetBit(ref bitField32, 9, 6, value);
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
            int pos = startPos + size - i;
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
