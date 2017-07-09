using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Geoway.ADF.MIS.Utility.Log;

namespace Geoway.ADF.GwCommunication
{
    public delegate void ReceiveMessageCallBack(string str);
    public delegate void ExecuteMessageCallBack(string str); 
    public class GwTcpClient
    {
        private bool isLive = false;
        private TcpClient client;
        private NetworkStream networkStream;
        private ManualResetEvent allDone = new ManualResetEvent(false);
        public ReceiveMessageCallBack ReceiveMessage;
        public ExecuteMessageCallBack ExecuteMessage;

        private string _ip;
        private int _port = 5679;
        private string message;
        private bool isConnected = false;

        public bool IsLive
        {
            get { return isLive; }
        }

        public GwTcpClient(string ip,int port)
        {
            //client = new TcpClient(AddressFamily.InterNetwork);
            _ip = ip;
            _port = port;
        }

        public bool connect()
        {
            try
            {
                client = new TcpClient(AddressFamily.InterNetwork);
                
                IPAddress IP = IPAddress.Parse(_ip);
                //client.Connect(IP, 56789);
                AsyncCallback connectCallBack = new AsyncCallback(ConnectCallBack);

                allDone.Reset();
                var res= client.BeginConnect(IP, _port, connectCallBack, client);
                
                allDone.WaitOne();
                int count = 10;
                int index = 0;
                while (true)
                {
                    if (isConnected)
                    {
                        return true;
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                    if (index < count)
                    {
                        index++;
                    }
                    else
                    {
                        break;
                    }
                }
                return false;
            }
            catch(Exception ex)
            {
                LogHelper.Error.Append(ex);
                return false;
            }
        }
        public void SendMessage(string message)
        {
            SendData(message);            
        }

        public bool Close()
        {
            try
            {
                client.GetStream().Close();
                client.Close();
            }
            catch (Exception ex)
            {
                LogHelper.Error.Append(ex);
            }
            isLive = true;
            allDone.Set();
            return true;
        }

        #region callback
        
        private void ConnectCallBack(IAsyncResult iar)
        {
            message = "ok";
            allDone.Set();
            try
            {
                client = (TcpClient)iar.AsyncState;
                client.EndConnect(iar);

                networkStream = client.GetStream();

                GwMessageData dataRead = new GwMessageData(networkStream, client.ReceiveBufferSize);
                networkStream.BeginRead(dataRead.msg, 0, dataRead.msg.Length, ReadCallBack, dataRead);
                isConnected = true;
                
            }
            catch (Exception e)
            {
                message = e.Message;
                LogHelper.Error.Append(e);
                isConnected = false;
                ExecuteMessage.Invoke(e.Message);
            }

        }
        private void ReadCallBack(IAsyncResult iar)
        {
            try
            {
                GwMessageData dataRead = (GwMessageData)iar.AsyncState;
                int recv = dataRead.ns.EndRead(iar);
                if (ReceiveMessage != null)
                {
                    ReceiveMessage.Invoke(Encoding.UTF8.GetString(dataRead.msg, 0, recv));
                }               
                if (isLive == false)
                {
                    dataRead = new GwMessageData(networkStream, client.ReceiveBufferSize);
                    networkStream.BeginRead(dataRead.msg, 0, dataRead.msg.Length, ReadCallBack, dataRead);
                }
            }
            catch (Exception e)
            {
                LogHelper.Error.Append(e);
                ExecuteMessage.Invoke(e.Message);
            }
        }
        private bool SendData(string str)
        {
            try
            {
                byte[] bytesdata = Encoding.UTF8.GetBytes(str + "\r\n");
                networkStream.BeginWrite(bytesdata, 0, bytesdata.Length, new AsyncCallback(SendCallBack), networkStream);
                networkStream.Flush();
                return true;
            }
            catch (Exception e)
            {
                LogHelper.Error.Append(e);
                ExecuteMessage.Invoke(e.Message);
                return false;
            }
        }
        private void SendCallBack(IAsyncResult iar)
        {
            try
            {
                networkStream.EndWrite(iar);
            }
            catch (Exception e)
            {
                LogHelper.Error.Append(e);
            }
        }
        #endregion
    }
}
