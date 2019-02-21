using System;

namespace RSMaster.Config
{
    using Utility;

    [Serializable]
    public class Settings
    {
        public string RSMasterUsername { get; set; }
        public string RSMasterPassword { get; set; }
        public string LicenseKey { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string BotBaseLocation { get; set; }
        public string CaptchaApiKey { get; set; }
        public string ClientMemory { get; set; }
        public string AccountDefaultScript { get; set; }
        public string AccountDefaultProxy { get; set; }
        public string CreateAccountHttpProxy { get; set; }
        public bool ClientDataCollection { get; set; }
        public bool ClientReflection { get; set; }
        public bool ClientLowCPU { get; set; }
        public bool ClientLowResource { get; set; }
        public bool ClientNoRender { get; set; }
        public bool ClientNoInterface { get; set; }
        public bool ClientNoRandoms { get; set; }
        public bool ClientLaunchHidden { get; set; }
        public bool LaunchAccountOnCreate { get; set; }
        public bool CreateAccountUseHttpProxy { get; set; }
        public bool AccountDefaultEnableProxy { get; set; }
        public bool RememberLoginDetails { get; set; }
        public bool KillClientsOnExit { get; set; }
        public bool DebugMode { get; set; }

        public int AccountDefaultWorld { get; set; }
        public int AccountCreateQueueLimit { get; set; }
        public int AccountCreateBreakTime { get; set; }
        public int AccountCreateLimit { get; set; }

        public void Save()
        {
            Serializer.Save(this, Util.AssemblyDirectory + @"\settings.xml");
        }

        public static Settings GetSettings()
        {
            return Serializer.Load(new Settings(), Util.AssemblyDirectory + @"\settings.xml");
        }
    }
}
