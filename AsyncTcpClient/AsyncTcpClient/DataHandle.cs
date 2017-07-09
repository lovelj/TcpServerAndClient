using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Geoway.ADF.AsyncTcpClient
{
    class DataRead
    {
        public NetworkStream ns;
        public byte[] msg;
        public DataRead(NetworkStream ns,int buffersize)
        {
            this.ns=ns;
            msg=new byte[buffersize];
        }
    }
}
