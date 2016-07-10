using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

namespace BitMoose.Core.Communication
{
    public delegate void ClientReceivedMessageHandler(string sender, string message);

    public enum CommunicationState
    {
        Connecting,
        Connected,
        Disconnected
    }

    public class Client
    {
        public event ClientReceivedMessageHandler ClientReceivedMessage;

        private Thread ClientThread = null;
        private bool Terminate = false;

        private CommunicationState m_State = CommunicationState.Disconnected;

        #region " Properties "

        public CommunicationState State
        {
            get
            {
                return m_State;
            }
            protected set
            {
                m_State = value;
            }
        }

        #endregion

        #region " Methods "

        public void Start()
        {
            Terminate = false;
            ClientThread = new Thread(new ThreadStart(BeginListening));
            ClientThread.Start();
            m_State = CommunicationState.Connecting;
        }

        private void BeginListening()
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                ReceiveTimeout = 400 /*400ms timeout*/,
                SendTimeout = 400 /*400ms timeout*/
            };

            IPEndPoint bcEndPoint = null;
            //find an open port to listen to the service multicast
            for (int x = 0; x < 10; x++)
            {
                try
                {
                    bcEndPoint = new IPEndPoint(IPAddress.Loopback, 49740 + x);
                    sock.Bind(bcEndPoint);
                    break;
                }
                catch (SocketException)
                {
                    bcEndPoint = null;
                }
            }
            
            if (bcEndPoint != null)
            {
                EndPoint ep = (EndPoint)bcEndPoint;
                while (Terminate == false)
                {
                    m_State = CommunicationState.Connected;
            
                    int len = 0;
                    byte[] data = new byte[2];
                    
                    try
                    {
                        int recv = sock.ReceiveFrom(data, ref ep);
                        if (data != null && recv == 2)
                        {
                            len = data[0] * 256;
                            len += data[1];
                        }
                        if (len > 0)
                        {
                            data = new byte[len];
                            sock.ReceiveFrom(data, ref ep);
                            if (data != null && data.Length > 0)
                            {
                                string msg = Encoding.UTF8.GetString(data);
                                if (msg != null && msg.Length > 0 && ClientReceivedMessage != null)
                                {
                                    int sepIndex = msg.IndexOf(':');
                                    if (sepIndex > -1)
                                    {
                                        string name = msg.Substring(0, sepIndex);
                                        string message = msg.Substring(sepIndex + 1);
                                        message = message.TrimEnd('\0');
                                        ClientReceivedMessage(name, message);
                                    }
                                    else
                                    {
                                        Trace.TraceWarning("Unexpected UDP Message: " + msg);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Terminate = true;
                        }
                    }
                    catch (SocketException ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                    if (Terminate == false)
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            
            //exiting loop, so kill instance
            if (sock != null)
            {
                if (sock.Connected)
                {
                    sock.Disconnect(true);
                }
                sock.Close();
            }
            
            Terminate = false;
            m_State = CommunicationState.Disconnected;
        }

        public void Stop()
        {
            Terminate = true;
            if (ClientThread != null)
            {
                //wait 500ms for client to close, if it doesn't kill the thread
                for (int x = 0; x < 10; x++)
                {
                    if (ClientThread.ThreadState == System.Threading.ThreadState.Stopped)
                        return;

                    Thread.Sleep(50);
                }
                ClientThread.Abort();
            }
        }

        #endregion
    }
}