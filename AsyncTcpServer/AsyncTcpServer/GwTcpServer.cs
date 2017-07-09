using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Geoway.ADF.MIS.Utility.Log;

namespace Geoway.ADF.AsyncTcpServer
{
    public delegate void ServerInfoCallBack(string str);
    public delegate void ReceiveMessageCallBack(string str);
    public delegate void AddClientCallBack(string str);
    public delegate void RemoveClientCallBack(GwTcpServerData datareadwrite);

    /// <summary>
    /// 服务端
    /// </summary>
    public class GwTcpServer
    {
        System.Collections.ArrayList clientlist = new System.Collections.ArrayList();
        TcpListener listener;
        private ManualResetEvent allDone = new ManualResetEvent(false);
        Thread accepthread;
        private bool _isRunning = false;
          
        #region 委托
        public ServerInfoCallBack ServerinfoCallback;
        public ReceiveMessageCallBack ReceiveMessage;
        public AddClientCallBack AddclientCallback;
        public RemoveClientCallBack RemoveclientCallback;
        #endregion

        #region  属性
        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }
        }
        #endregion
        /// <summary>
        /// 启动
        /// </summary>
        /// <returns></returns>
        public bool Startup()
        {
            try
            {
                if (accepthread != null)
                { accepthread.Abort(); }
                accepthread = new Thread(new ThreadStart(AcceptConnection));
                accepthread.Start();
                _isRunning = true;
            }
            catch (Exception ex)
            {
                LogHelper.Error.Append(ex);
                _isRunning = false;
                return false;
            }
            return true;
        }
        /// <summary>
        /// 停止
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            _isRunning = false;
            allDone.Set();
            listener.Stop();
            return true;
        }
   
        /// <summary>
        /// 发送到指定目标
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool SendTo(string endpoint,string message)
        {
            foreach (var item in clientlist)
            {
               var iendpoint = (item as GwTcpServerData).client.Client.RemoteEndPoint.ToString();

               if (iendpoint.Equals(endpoint))
               {
                   SendString(item as GwTcpServerData, message);
                   return true;
               }
            }   
         
            return false;
        }

        #region
        private void AcceptConnection()
        {
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
            IPAddress localIP = null;
            foreach (IPAddress ip in ips)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    localIP = ip;
                    break;
                }
            }
            if (localIP == null)
            {
                return;
            }
            if (listener == null)
            {
                int port = GetConfigValue("LISTENERPORT", 56789);
                listener = new TcpListener(localIP, port);
            }
            //listener.Stop();
            listener.Start();
            while (_isRunning)
            {
                try
                {
                    allDone.Reset();
                    AsyncCallback callback = new AsyncCallback(AcceptTcpClientCallBack);
                  
                    SetServerInfo("开始等待连接");
                    
                    listener.BeginAcceptTcpClient(callback, listener);
                    allDone.WaitOne();
                }
                catch (Exception e)
                {
                    LogHelper.Error.Append(e);
                    if (ServerinfoCallback != null)
                    {
                        ServerinfoCallback.Invoke(e.Message);
                    } 
                    break;
                }
            }
        }
        private void AcceptTcpClientCallBack(IAsyncResult iar)
        {
            try
            {
                if (IsRunning)
                {
                    //allDone.Set();
                    TcpListener mylistener = (TcpListener)iar.AsyncState;
                    TcpClient client = mylistener.EndAcceptTcpClient(iar);
                    SetServerInfo("已接受连接：" + client.Client.RemoteEndPoint);

                    if (AddclientCallback != null)
                    {
                        AddclientCallback.Invoke(client.Client.RemoteEndPoint.ToString());
                    }
                    mylistener.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClientCallBack), mylistener);
                    GwTcpServerData datareadwrite = new GwTcpServerData(client);
                    clientlist.Add(datareadwrite);
                    SendString(datareadwrite, "connect ok");
                    datareadwrite.ns.BeginRead(datareadwrite.read, 0, datareadwrite.read.Length, ReadCallBack, datareadwrite);
                }
            }
            catch (Exception e)
            {
                if (ServerinfoCallback != null)
                {
                    ServerinfoCallback.Invoke(e.Message);
                }
                //if (_isRunning)
                //{
                //    AcceptConnection();
                //}
                return;
            }
        }
        private void ReadCallBack(IAsyncResult iar)
        {
            GwTcpServerData datareadwrite = (GwTcpServerData)iar.AsyncState;
            try
            {               
                int recv = datareadwrite.ns.EndRead(iar);
                
                if (_isRunning)
                {
                    ReceiveMessage.Invoke(string.Format("{0}#{1}", datareadwrite.client.Client.RemoteEndPoint, Encoding.UTF8.GetString(datareadwrite.read, 0, recv)));
                    datareadwrite.InitReadArray();
                }
                datareadwrite.ns.BeginRead(datareadwrite.read, 0, datareadwrite.read.Length, ReadCallBack, datareadwrite); 
            }
            catch (Exception e)
            {
                LogHelper.Error.Append(e);
                SetServerInfo(e.Message);
                //datareadwrite.client.Close();
                
            }
        }
        private void SendString(GwTcpServerData datareadwrite, string str)
        {
            try
            {
                datareadwrite.write = Encoding.UTF8.GetBytes(str);
                datareadwrite.ns.BeginWrite(datareadwrite.write, 0, datareadwrite.write.Length, new AsyncCallback(SendCallBack), datareadwrite);
                datareadwrite.ns.Flush();
                SetServerInfo(string.Format("向{0}发送:{1}", datareadwrite.client.Client.RemoteEndPoint, str));
            }
            catch (Exception e)
            {
                SetServerInfo(e.Message);
                RemoveclientCallback.Invoke(datareadwrite);
            }
        }
        private void SendCallBack(IAsyncResult iar)
        {
            GwTcpServerData datareadwrite = (GwTcpServerData)iar.AsyncState;
            try
            {
                datareadwrite.ns.EndWrite(iar);
            }
            catch (Exception e)
            {
                SetServerInfo(e.Message);
                RemoveclientCallback.Invoke(datareadwrite);
            }
        }

        private void SetServerInfo(string message)
        {
            if (ServerinfoCallback != null)
            {
                ServerinfoCallback.Invoke(message);
            }
        }
        private int GetConfigValue(string name, int defaultValue = 56789)
        {
            string val = System.Configuration.ConfigurationManager.AppSettings.Get(name);
            if (val == null)
            {
                return defaultValue;
            }
            int iVal = defaultValue;
            try
            {
                iVal = int.Parse(val);
            }
            catch (Exception ex)
            {
                LogHelper.Error.Append(ex);
                return defaultValue;
            }
            return iVal;
        }     
        #endregion

    }
}
