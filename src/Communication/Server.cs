using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BitMoose.Core.Communication
{
    public class Server : IDisposable
    {
        private Socket BroadcastSocket = null;
        private IPEndPoint[] BroadcastEndPoints = null;

        public void Start()
        {
            BroadcastSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            BroadcastEndPoints = new IPEndPoint[10];
            for (int x = 0; x < BroadcastEndPoints.Length; x++)
            {
                BroadcastEndPoints[x] = new IPEndPoint(IPAddress.Loopback, 49740 + x);
            }
            
            BroadcastSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
        }

        public void Stop()
        {
            if (BroadcastSocket != null)
            {
                BroadcastSocket.Close();
                BroadcastSocket = null;
            }
        }

        public void Write(string message)
        {
            if (BroadcastSocket != null)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
            
                int len = data.Length;
                if (len > ushort.MaxValue)
                {
                    len = (int)ushort.MaxValue;
                }
                
                for (int x = 0; x < BroadcastEndPoints.Length; x++)
                {
                    BroadcastSocket.SendTo(new byte[] { (byte)(len / 256), (byte)(len & 255) }, BroadcastEndPoints[x]);
                    BroadcastSocket.SendTo(data, BroadcastEndPoints[x]);
                }
            }
        }

        public void Dispose()
        {
            if (BroadcastSocket != null)
            {
                BroadcastSocket.Dispose();
                BroadcastSocket = null;
            }
        }
    }
}