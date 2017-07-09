using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Geoway.ADF.GwCommunication;

namespace Geoway.ADF.AsyncTcpClient
{
    public partial class Form1 : Form
    {       
		private bool isLive = false;
        private TcpClient client;
        private NetworkStream ns;
       
		private ManualResetEvent allDone = new ManualResetEvent(false);
		private delegate void SetListBoxCallBack(string str);
    
		private SetListBoxCallBack setlistboxcallback;
        private delegate void SetRichTextBoxReceiveCallBack(string str);
        private SetRichTextBoxReceiveCallBack setRichTextBoxReceiveCallBack;

        GwTcpClient gwClient = new GwTcpClient("192.98.12.10",56789);
        public delegate void ShowMessage(string message);
        private ShowMessage _showtablespace;
        public Form1()
        {
            InitializeComponent();
            _showtablespace = Settext2;
            gwClient.ReceiveMessage = new ReceiveMessageCallBack(SetRichTextBoxReceive);
            gwClient.ExecuteMessage = new ExecuteMessageCallBack(SetListBox);
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
           // return;
            //if (gwClient.IsLive)
            //{
            //    return;
            //}
            //if (gwClient != null)
            //{
            //    gwClient=new GwTcpClient("192.98.102.139",56789);//.connect();
            //    gwClient.ReceiveMessage = new ReceiveMessageCallBack(SetRichTextBoxReceive);
            //    gwClient.ExecuteMessage = new ExecuteMessageCallBack(SetListBox);
            //}
            gwClient.connect();
        }
        private void SetListBox(string str)
        {
            //listBoxStatus.Items.Add(str);
            //listBoxStatus.SelectedIndex = listBoxStatus.Items.Count - 1;
            //listBoxStatus.ClearSelected();
        }
        private void SetRichTextBoxReceive(string str)
        {
            if (!IsHandleCreated)
            {
                return;
            }
            Invoke(_showtablespace, str);
            
        }
        private void Settext2(string str)
        {
            richTextBoxRecv.AppendText(str);
        }
        private void buttonSend_Click(object sender, EventArgs e)
        {
            gwClient.SendMessage(richTextBoxSend.Text);
            richTextBoxSend.Clear();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            gwClient.Close();
            
        }

    
    }
}