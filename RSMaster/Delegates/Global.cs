using RSMaster.UI.Models;
using System.Threading.Tasks;

namespace RSMaster
{
    internal delegate void LogDelegate(string text);
    internal delegate void ShutdownDelegate();
    internal delegate Task LaunchAccountDelegate(AccountModel account, bool autoLaunch);
    internal delegate AccountModel GetSelectedAccountDelegate();
    internal delegate AccountModel GetAccountByIdDelegate(int accountId);
    internal delegate void UpdateAccountDelegate(AccountModel accountModel);
}
