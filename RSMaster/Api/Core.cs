using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSMaster.Api
{
    using Objects;

    public class Core : PluginObject
    {
        /// <summary>
        /// Log text on main UI log tab.
        /// </summary>
        /// <param name="text"></param>
        public void Log(string text)
        {
            proxyBase.Log(text);
        }

        /// <summary>
        /// Notifies you when RSMaster receives a message from OSBot bridge.
        /// </summary>
        public event PacketReceiveHandler OnPacketReceive
        {
            add => proxyBase.OnPacketReceive += value;
            remove => proxyBase.OnPacketReceive -= value;
        }

        /// <summary>
        /// Notifies you when an account is successfully created.
        /// </summary>
        public event AccountCreatedHandler OnAccountCreated
        {
            add => proxyBase.OnAccountCreated += value;
            remove => proxyBase.OnAccountCreated -= value;
        }

        /// <summary>
        /// Get all loaded accounts.
        /// </summary>
        public List<Account> GetLoadedAccounts() => proxyBase.GetLoadedAccounts();

        /// <summary>
        /// Get loaded account by id.
        /// </summary>
        /// <param name="accountId"></param>
        public Account GetAccountById(int accountId)
        {
            return proxyBase.GetLoadedAccounts().FirstOrDefault(x => x.Id == accountId);
        }

        /// <summary>
        /// Get loaded account by username.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public Account GetAccountByUsername(string username)
        {
            return proxyBase.GetLoadedAccounts().FirstOrDefault(x => x.Username == username);
        }

        /// <summary>
        /// Launch loaded account by id.
        /// </summary>
        /// <param name="accountId"></param>
        public void LaunchAccountById(int accountId) => proxyBase.LaunchAccountById(accountId);

        /// <summary>
        /// Launch loaded account.
        /// </summary>
        /// <param name="account"></param>
        public void LaunchAccount(Account account) => proxyBase.LaunchAccount(account);

        /// <summary>
        /// Stop loaded running account by id.
        /// </summary>
        /// <param name="accountid"></param>
        public void StopAccountById(int accountid) => proxyBase.StopAccountById(accountid);

        public CoreBase proxyBase;
    }
}
