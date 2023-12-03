using System;
using System.Text;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Net;


public class CheckPort
{
    public static bool PingTest(string ip)
    {
        bool result = false;
        try
        {
            Ping pp = new Ping();
            PingOptions po = new PingOptions();

            po.DontFragment = true;

            byte[] buf = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaa");

            PingReply reply = pp.Send(
                IPAddress.Parse(ip),
                10, buf, po
            );

            if (reply.Status == IPStatus.Success)
            {
                result= true;
            }
            else
            {
                result = false;
            }
            return result;
        }
        catch
        {
            throw;
        }
    }


    public static bool ConnectTest(string ip, int port)
    {
        bool result = false;

        Socket socket = null;
        try
        {
            socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream, 
                ProtocolType.Tcp
            );

            socket.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.DontLinger,
                false
            );


            IAsyncResult ret = socket.BeginConnect(ip, port, null, null);

            result = ret.AsyncWaitHandle.WaitOne(100, true);
        }
        catch { }
        finally
        {
            if (socket != null)
            {
                socket.Close();
            }
        }
        return result;
    }
}