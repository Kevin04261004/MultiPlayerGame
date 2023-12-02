using System;
using System.Text;

public class GamePlayerInfo
{
    public static byte[] ChangeToBytes(in GamePlayerInfoData playerInfo)
    {
        byte[] temp = BitConverter.GetBytes((int)playerInfo.socketType);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(temp);
        }

        byte[] temp2 = Encoding.Default.GetBytes(playerInfo.playerName);

        byte[] returnArr = new byte[temp.Length + temp2.Length];
        Buffer.BlockCopy(temp,0,returnArr,0,temp.Length);
        Buffer.BlockCopy(temp2,0,returnArr,temp.Length,temp2.Length);
        return returnArr;
    }
    public static GamePlayerInfoData ChangeToGamePlayerInfo(in byte[] bytes)
    {
        byte[] temp = new byte[sizeof(ESocketType)]; // 4바이트
        Array.Copy(bytes, 0, temp, 0, 4);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(temp);
        }

        int temp2 = BitConverter.ToInt32(temp, 0);
        byte[] temp3 = new byte[bytes.Length - sizeof(ESocketType)];
        Array.Copy(bytes, 4, temp3, 0, temp3.Length);
        GamePlayerInfoData returnData = new()
        {
            socketType = (ESocketType)temp2,
            playerName =  Encoding.Default.GetString(temp3)
        };
        return returnData;
    }
}
