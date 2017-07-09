using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using Geoway.ADF.MIS.Utility.Log;

namespace Geoway.ADF.AsyncTcpServer
{
    public partial class Form1 : Form
    {
        private bool isExit = false;
        System.Collections.ArrayList clientlist = new System.Collections.ArrayList();
        TcpListener listener;
        private delegate void SetListBoxCallBack(string str);
        private SetListBoxCallBack setlistboxcallback;
        private delegate void SetRichTextBoxCallBack(string str);
        private SetRichTextBoxCallBack setrichtextboxcallback;
        private delegate void SetComboBoxCallBack(string str);
        private SetComboBoxCallBack setcomboboxcallback;
        private delegate void RemoveComboBoxItemsCallBack(GwTcpServerDataReadWrite datareadwrite);
        private RemoveComboBoxItemsCallBack removecomboboxcallback;
        private ManualResetEvent allDone = new ManualResetEvent(false);

        GwTcpServer _gwTcpServer = new GwTcpServer();

        private delegate void showmessage(string message);
        private showmessage showinfomsg;
        private showmessage showrecmsg;
        private showmessage showcmbmsg;
        private RemoveComboBoxItemsCallBack showrmvmsg;

        public Form1()
        {
            InitializeComponent();
            showinfomsg = SetListBox2UI;
            showrecmsg=SetReceiveText2UI;
            showcmbmsg=SetComboBox2UI;
            showrmvmsg = RemoveComboBoxItems2UI;

            _gwTcpServer.ServerinfoCallback = new ServerInfoCallBack(SetListBox);
            _gwTcpServer.ReceiveMessage = new ReceiveMessageCallBack(SetReceiveText);
            _gwTcpServer.AddclientCallback = new AddClientCallBack(SetComboBox);
            _gwTcpServer.RemoveclientCallback = new RemoveClientCallBack(RemoveComboBoxItems);

        }
        
        private void buttonStart_Click(object sender, EventArgs e)
        {
            _gwTcpServer.Startup();
            if (_gwTcpServer.IsRunning)
            {
                buttonStart.Enabled = false;
                buttonStop.Enabled = true;
            }
            //Thread myThread = new Thread(new ThreadStart(AcceptConnection));
            //myThread.Start();
            //buttonStart.Enabled = false;
            //buttonStop.Enabled = true;
        }
        //private void AcceptConnection()
        //{
        //    IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
        //    IPAddress localIP = null;
        //    foreach (IPAddress ip in ips)
        //    {
        //        if (ip.AddressFamily.ToString() == "InterNetwork")
        //        {
        //            localIP = ip;
        //            break;
        //        }
        //    }
        //    if (localIP == null)
        //    {
        //        return;
        //    }
        //    // TcpListener server = new TcpListener(port);
        //    listener = new TcpListener(localIP, 56789);
        //    listener.Start();
        //    while (isExit == false)
        //    {
        //        try
        //        {
        //            allDone.Reset();
        //            AsyncCallback callback = new AsyncCallback(AcceptTcpClientCallBack);
        //            listBoxStatus.Invoke(setlistboxcallback, "开始等待连接");
        //            listener.BeginAcceptTcpClient(callback, listener);
        //            allDone.WaitOne();
        //        }
        //        catch(Exception e)
        //        {
        //            listBoxStatus.Invoke(setlistboxcallback,e.Message);
        //            break;
        //        }
        //    }
        //}
        //private void AcceptTcpClientCallBack(IAsyncResult iar)
        //{
        //    try
        //    {
        //        //allDone.Set();
        //        TcpListener mylistener = (TcpListener)iar.AsyncState;
        //        TcpClient client = mylistener.EndAcceptTcpClient(iar);
        //        listBoxStatus.Invoke(setlistboxcallback, "已接受客户连接：" + client.Client.RemoteEndPoint);
        //        //listBoxStatus.Items.Add( "已接受客户连接：" + client.Client.RemoteEndPoint);
        //        comboBox1.Invoke(setcomboboxcallback, client.Client.RemoteEndPoint.ToString());
        //        GwTcpServerDataReadWrite datareadwrite = new GwTcpServerDataReadWrite(client);
        //        clientlist.Add(datareadwrite);
        //        SendString(datareadwrite, "服务器已经接受连接，请通话");
        //        datareadwrite.ns.BeginRead(datareadwrite.read, 0, datareadwrite.read.Length, ReadCallBack, datareadwrite);
        //    }
        //    catch (Exception e)
        //    {
        //        listBoxStatus.Invoke(setlistboxcallback,e.Message);
        //        return;
        //    }
        //}
        //private void ReadCallBack(IAsyncResult iar)
        //{
        //    try
        //    {
        //        GwTcpServerDataReadWrite datareadwrite = (GwTcpServerDataReadWrite)iar.AsyncState;
        //        int recv = datareadwrite.ns.EndRead(iar);

        //        richTextBoxRecv.Invoke(setrichtextboxcallback, string.Format("[来自{0}]{1}", datareadwrite.client.Client.RemoteEndPoint, Encoding.UTF8.GetString(datareadwrite.read, 0, recv)));
        //        if (isExit == false)
        //        {
        //            datareadwrite.InitReadArray();
        //            datareadwrite.ns.BeginRead(datareadwrite.read, 0, datareadwrite.read.Length, ReadCallBack, datareadwrite);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        listBoxStatus.Invoke(setlistboxcallback,e.Message);
        //    }
        //}
        //private void SendString(GwTcpServerDataReadWrite datareadwrite, string str)
        //{
        //    try
        //    {
        //        datareadwrite.write = Encoding.ASCII.GetBytes(str + "\r\n");
        //        datareadwrite.ns.BeginWrite(datareadwrite.write, 0, datareadwrite.write.Length, new AsyncCallback(SendCallBack), datareadwrite);
        //        datareadwrite.ns.Flush();
        //        listBoxStatus.Invoke(setlistboxcallback, string.Format("向{0}发送:{1}", datareadwrite.client.Client.RemoteEndPoint, str));
        //    }
        //    catch (Exception e)
        //    {
        //        listBoxStatus.Items.Add(e.Message);
        //    }
        //}
        //private void SendCallBack(IAsyncResult iar)
        //{
        //    GwTcpServerDataReadWrite datareadwrite = (GwTcpServerDataReadWrite)iar.AsyncState;
        //    try
        //    {
        //        datareadwrite.ns.EndWrite(iar);
        //    }
        //    catch (Exception e)
        //    {
        //        listBoxStatus.Invoke(setlistboxcallback,e.Message);
        //        comboBox1.Invoke(removecomboboxcallback,datareadwrite);
        //    }
        //}
       
        private void RemoveComboBoxItems(GwTcpServerDataReadWrite datareadwrite)
        {
            Invoke(showrmvmsg, datareadwrite);
        }
        private void SetListBox(string str)
        {
            if (!IsHandleCreated)
            {
                return;
            }
            Invoke(showinfomsg,str);           
        }
        private void SetReceiveText(string str)
        {
            Invoke(showrecmsg, str);   
        }
        private void SetComboBox(object obj)
        {
            Invoke(showcmbmsg, obj);   
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            _gwTcpServer.Stop();
            //isExit = true;
            //allDone.Set();
            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
            
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            int index = comboBox1.SelectedIndex;
            if (index == -1)
            {
                MessageBox.Show("请先选择接收方，再单击发送");
            }
            else
            {
                //GwTcpServerDataReadWrite obj = (GwTcpServerDataReadWrite)clientlist[index];
                _gwTcpServer.SendTo(comboBox1.Text, richTextBoxSend.Text);// SendString(obj, richTextBoxSend.Text);
                richTextBoxSend.Clear();
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            buttonStop_Click(null,null);
        }

        private void Form1_FormClosing_1(object sender, FormClosingEventArgs e)
        {
            buttonStop_Click(null, null);
        }


        #region 界面事件
        private void SetListBox2UI(string str)
        {
            listBoxStatus.Items.Add(str);
            listBoxStatus.SelectedIndex = listBoxStatus.Items.Count - 1;
            listBoxStatus.ClearSelected();
        }
        private void SetReceiveText2UI(string str)
        {
            richTextBoxRecv.AppendText(str);
        }
        private void SetComboBox2UI(object obj)
        {
            comboBox1.Items.Add(obj);
        }
        private void RemoveComboBoxItems2UI(GwTcpServerDataReadWrite datareadwrite)
        {
            int index = clientlist.IndexOf(datareadwrite);
            comboBox1.Items.RemoveAt(index);
        }
        #endregion

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

    }
}