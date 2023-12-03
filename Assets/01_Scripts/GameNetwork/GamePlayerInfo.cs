using System;
using System.Text;

public class GamePlayerInfo
{
    public static byte[] ChangeToBytes(in GamePlayerInfoData playerInfo)
    {
        byte[] socketTypeArr = BitConverter.GetBytes((int)playerInfo.socketType);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(socketTypeArr);
        }
        
        byte[] isReadyArr = BitConverter.GetBytes(playerInfo.isReady);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(isReadyArr);
        }
        
        byte[] playerNameArr = Encoding.Default.GetBytes(playerInfo.playerName);

        byte[] returnArr = new byte[socketTypeArr.Length + playerNameArr.Length + isReadyArr.Length];
        int offset = 0;
        Buffer.BlockCopy(socketTypeArr,0,returnArr,offset,socketTypeArr.Length);
        offset += socketTypeArr.Length;
        
        Buffer.BlockCopy(isReadyArr,0,returnArr,offset,isReadyArr.Length);
        offset += isReadyArr.Length;
        
        Buffer.BlockCopy(playerNameArr,0,returnArr,offset,playerNameArr.Length);
        return returnArr;
    }
    public static GamePlayerInfoData ChangeToGamePlayerInfo(in byte[] bytes)
    {
        byte[] socketTypeArr = new byte[sizeof(ESocketType)]; // 4바이트
        Array.Copy(bytes, 0, socketTypeArr, 0, socketTypeArr.Length);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(socketTypeArr);
        }
        int socketType = BitConverter.ToInt32(socketTypeArr, 0);
        byte[] isReadyArr = new byte[sizeof(bool)];
        Array.Copy(bytes, sizeof(ESocketType), isReadyArr, 0, isReadyArr.Length);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(isReadyArr);
        }
        bool isReady = BitConverter.ToBoolean(isReadyArr, 0);
        
        byte[] strArr = new byte[bytes.Length - sizeof(ESocketType) - sizeof(bool)];
        Array.Copy(bytes, sizeof(ESocketType)+ sizeof(bool), strArr, 0, strArr.Length);
        GamePlayerInfoData returnData = new()
        {
            socketType = (ESocketType)socketType,
            isReady =  isReady,
            playerName =  Encoding.Default.GetString(strArr)
        };
        return returnData;
    }
}
