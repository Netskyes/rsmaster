using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSMaster.Api
{
    using UI;
    using Objects;

    public class CoreBase : PluginObject
    {
        protected internal List<PluginHandler> LaunchedPlugins { get; set; } = new List<PluginHandler>();

        public CoreBase()
        {
            MainWindow.NetworkManager.PacketReceiveCallback += (b) => { OnPacketReceive?.Invoke(b); };
            MainWindow.AccountCreatedCallback += (a) => 
            {
                OnAccountCreated?.Invoke(GetAccountByModel(a));
            };
        }

        protected internal void LaunchPlugin(string asmPath)
        {
            var ph = new PluginHandler(this, asmPath);
            ph.Launch();
        }

        #region Public Exposed

        public event PacketReceiveHandler OnPacketReceive;
        public event AccountCreatedHandler OnAccountCreated;

        public void Log(string text)
        {
            MainWindow.LogHandler(text);
        }

        public List<Account> GetLoadedAccounts()
        {
            return MainWindow.GetAccountsHandler().Select(GetAccountByModel).ToList();
        }

        public void LaunchAccountById(int accountId)
        {
            var account = MainWindow.GetAccountsHandler().FirstOrDefault(x => x.Id == accountId);
            if (account != null)
            {
                MainWindow.LaunchAccountHandler(account, false);
            }
        }

        #endregion

        private Account GetAccountByModel(UI.Models.AccountModel source)
        {
            if (source is null)
                return null;

            var account = new Account
            {
                Id = source.Id,
                Name = source.Name,
                Username = source.Username,
                Password = source.Password,
                Script = source.Script,
                ProxyName = source.ProxyName,
                ProxyEnabled = Convert.ToBoolean(source.ProxyEnabled),
                BankPIN = source.BankPIN,
                IsTemporary = Convert.ToBoolean(source.Temporary),
                GroupId = source.GroupId,
                Comments = source.Comments,
                PID = source.PID,
                Created = source.Created,
                Status = source.Status,
                Visible = source.Visible,
                AutoLaunched = source.AutoLaunched,
                WindowHandle = source.WindowHandle,
                World = source.World
            };

            return account;
        }
    }
}
