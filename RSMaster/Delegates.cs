using System.Collections.Generic;
using System.Threading.Tasks;

namespace RSMaster
{
    using UI.Models;

    internal delegate void LogCallback(string text);
    internal delegate void ShutdownCallback();
    internal delegate Task LaunchAccountCallback(AccountModel account, bool autoLaunch = false);
    internal delegate AccountModel GetSelectedAccountCallback();
    internal delegate void UpdateAccountCallback(AccountModel accountModel);
    internal delegate GroupModel GetGroupByIdCallback(int groupId);
    internal delegate IEnumerable<AccountModel> GetAccountsCallback();

    public delegate void PacketReceiveHandler(byte[] buffer);
    public delegate void AccountCreatedHandler(AccountModel accountModel);
}
