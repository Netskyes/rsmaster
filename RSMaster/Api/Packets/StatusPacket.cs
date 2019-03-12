using System;

namespace RSMaster.Api.Packets
{
    [Serializable]
    public class StatusPacket
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public int StatusCode { get; set; }
    }
}
