using System;

namespace RSMaster.Api.Objects
{
    public class Account : PluginObject
    {
        internal Account()
        {
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Script { get; set; }
        public int? World { get; set; }
        public int ProxyId { get; set; }
        public string ProxyName { get; set; }
        public bool ProxyEnabled { get; set; }
        public string BankPIN { get; set; }
        public bool IsTemporary { get; set; }
        public int? GroupId { get; set; }
        public string GroupName { get; set; }
        public string Comments { get; set; }
        public int? PID { get; set; }
        public DateTime Created { get; set; }
        public string Status { get; set; }
        public bool Visible { get; set; }
        public bool AutoLaunched { get; set; }
        public IntPtr WindowHandle { get; set; }
    }
}
