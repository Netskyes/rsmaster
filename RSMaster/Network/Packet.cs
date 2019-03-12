using System.Net;

namespace RSMaster.Network
{
    internal class Packet
    {
        public byte[] Buffer { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public bool UdpPacket { get; set; }
    }
}
