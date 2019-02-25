using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace RSMaster.Network
{
    public class Client : ClientBase
    {
        public int ID { get; set; }

        public Client(Server server, Socket socket)
        {
            ID = new Random().Next(100, 9999);
            BUFFER_SIZE = (1024 * 1024) * 20; // 20MB
            HEADER_SIZE = 2;

            BeginListen(server, socket);
        }

        public Client()
        {
            ID = new Random().Next(100, 9999);
            BUFFER_SIZE = (1024 * 1024) * 20; // 20MB
            HEADER_SIZE = 2;
        }

        protected override void AsyncRecvProcess(Packet packet)
        {
        }
    }
}
