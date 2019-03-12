namespace RSMaster.Objects
{
    internal class ImapDetails
    {
        public string Host { get; set; }
        public int Port { get; set; } = 993;
        public bool UseSsl { get; set; } = true;
    }
}
