using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Geoway.ADF.GwCommunication
{    
    
    public class GwMessageData
    {
        public NetworkStream ns;
        public byte[] msg;
        public GwMessageData(NetworkStream ns, int buffersize)
        {
            this.ns=ns;
            msg=new byte[buffersize];
        }
    }
}
