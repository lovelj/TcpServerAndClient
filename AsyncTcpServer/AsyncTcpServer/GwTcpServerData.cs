using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Geoway.ADF.AsyncTcpServer
{
    public class GwTcpServerData
    {
        public TcpClient client;
        public NetworkStream ns;
        public byte[] read;
        public byte[] write;
        public GwTcpServerData(TcpClient client)
        {
            this.client = client;
            ns = client.GetStream();
            read = new byte[client.ReceiveBufferSize];
            write=new byte[client.SendBufferSize];
        }
        public void InitReadArray()
        { 
            read=new byte[client.ReceiveBufferSize];
        }
        public void InitWriteArray()
        { 
            write=new byte[client.SendBufferSize];
        }
    }
}
