using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using System.Threading;

namespace Geoway.ADF.AsyncTcpServer
{
    public partial class FormMain : DevExpress.XtraEditors.XtraForm
    {

        #region

        GwTcpServer _gwTcpServer = new GwTcpServer();


        private delegate void RemoveComboBoxItemsCallBack(GwTcpServerData datareadwrite);
        private RemoveComboBoxItemsCallBack removecomboboxcallback;
        private delegate void ShowMessage(string message);
        private ShowMessage showinfomsg;
        private ShowMessage showrecmsg;
        private ShowMessage showcmbmsg;
        private RemoveComboBoxItemsCallBack rmvmsg;
        VecTileProcessCreator _tileCreator = new VecTileProcessCreator();
        #endregion


        public FormMain()
        {
            InitializeComponent();
            showinfomsg = SetListBox2UI;
            showrecmsg = SetReceiveText2UI;
            showcmbmsg = SetComboBox2UI;
            rmvmsg = RemoveComboBoxItems2UI;

            _gwTcpServer.ServerinfoCallback = new ServerInfoCallBack(SetListBox);
            _gwTcpServer.ReceiveMessage = new ReceiveMessageCallBack(SetReceiveText);
            _gwTcpServer.AddclientCallback = new AddClientCallBack(SetComboBox);
            _gwTcpServer.RemoveclientCallback = new RemoveClientCallBack(RemoveComboBoxItems);
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            int index = cmbClient.SelectedIndex;
            if (index == -1)
            {
                MessageBox.Show("请先选择接收方，再单击发送");
            }
            else
            {
                //GwTcpServerDataReadWrite obj = (GwTcpServerDataReadWrite)clientlist[index];
                _gwTcpServer.SendTo(this.cmbClient.Text, richTextBoxSend.Text);// SendString(obj, richTextBoxSend.Text);
                richTextBoxSend.Text="";
            }
        }

        private void btnStartup_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            _gwTcpServer.Startup();
            if (_gwTcpServer.IsRunning)
            {
                btnStartup.Enabled = false;
                btnStop.Enabled = true;
            }
        }

        private void btnStop_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (_gwTcpServer.IsRunning)
            {
                _gwTcpServer.Stop();
            }
            //isExit = true;
            //allDone.Set();
            btnStartup.Enabled = true;
            btnStop.Enabled = false;
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            btnStop_ItemClick(null, null);
        }

        #region 
        private void RemoveComboBoxItems(GwTcpServerData datareadwrite)
        {
            Invoke(rmvmsg, datareadwrite);
        }
        private void SetListBox(string str)
        {
            if (!IsHandleCreated)
            {
                return;
            }
            Invoke(showinfomsg, str);
        }
        private void SetReceiveText(string str)
        {
            Invoke(showrecmsg, str);

        }
        private void SetComboBox(object obj)
        {
            Invoke(showcmbmsg, obj);
        }

        private void SetListBox2UI(string str)
        {
            listBoxStatus.Items.Add(str);
            listBoxStatus.SelectedIndex = listBoxStatus.Items.Count - 1;
            listBoxStatus.SelectedItem = null;
        }

        private void SetReceiveText2UI(string str)
        {
            richTextBoxRecv.Text+=System.Environment.NewLine+str;
            if(str.Split('#').Length!=3)
            {
                return;
            }
            string message=string.Empty;
            string client = str.Split('#')[0];
            string strtype = str.Split('#')[1];
            string strmsg = str.Split('#')[2];
            if (strtype.ToLower().Equals("vectile"))
            {
                Thread t = new Thread(new ParameterizedThreadStart(vectileThread));
                t.Start(str);  
            }
        }

        private void vectileThread(object message)
        {
            string outmessage = string.Empty;
            string client = message.ToString().Split('#')[0];
            string strtype = message.ToString().Split('#')[1];
            string strmsg = message.ToString().Split('#')[2];

            if (!_tileCreator.CreateIndex(strmsg, out outmessage))
            {
               // SetListBox2UI(message);
                _gwTcpServer.SendTo(client, "error:" + outmessage);
            }
            else
            { _gwTcpServer.SendTo(client, "ok"); }
        }
        private void SetComboBox2UI(object obj)
        {
            cmbClient.Properties.Items.Add(obj);
            //_gwTcpServer.SendTo(obj.ToString(), "connect ok"); 
        }
        private void RemoveComboBoxItems2UI(GwTcpServerData datareadwrite)
        {
            foreach (var item in cmbClient.Properties.Items)
            {
                if (item.ToString().Equals(datareadwrite.client.Client.RemoteEndPoint.ToString()))
                {
                    cmbClient.Properties.Items.Remove(item);
                    break;
                }
            }           
        }
        #endregion

    }
}