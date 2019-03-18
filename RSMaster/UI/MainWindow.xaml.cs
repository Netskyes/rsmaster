using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Forms;

namespace RSMaster.UI
{
    using Data;
    using Config;
    using Models;
    using Utility;
    using Helpers;
    using Application = System.Windows.Application;

    public partial class MainWindow : MetroWindow
    {
        public bool ShutdownInProgress;

        internal static Settings Settings { get; set; }
        internal static ProxyWindow ProxyWindow { get; set; }
        internal static AccountManager AccountManager { get; set; }
        internal static ScheduleManager ScheduleManager { get; set; }
        internal static ServicesWindow ServicesWindow { get; set; }
        internal static NetworkManager NetworkManager { get; set; }
        internal static Dialogs.LoginDialog LoginDialog { get; set; }
        internal static SchedulerWindow SchedulerWindow { get; set; }

        public ICollectionView AccountsListItems { get; set; }
        public ICollectionView ProxyListItems { get; set; }
        public ICollectionView SocksProxyListItems { get; set; }
        public ICollectionView GroupListItems { get; set; }
        public AccountModel AccountOpen { get; set; } = new AccountModel();

        internal ObservableCollection<AccountModel> accountsListItems = new ObservableCollection<AccountModel>();
        internal ObservableCollection<ProxyModel> proxyListItems = new ObservableCollection<ProxyModel>();
        internal ObservableCollection<GroupModel> groupListItems = new ObservableCollection<GroupModel>();
        private bool accountLoading;
        private bool updatesRequest;

        public MainWindow()
        {
            InitializeComponent();
            Cryptography.AES.SetDefaultKey("ukjdpxw6");

            #region Assign Callbacks

            LogHandler = Log;
            ShutdownHandler = AppShutdown;
            GetSelectedAccountHandler = GetSelectedAccount;
            GetGroupByIdHandler = GetGroupById;
            UpdateAccountHandler = UpdateAccount;
            LaunchAccountHandler = LaunchAccount;
            GetAccountsHandler = GetLoadedAccounts;
            StopAccountCallback = StopAccount;

            #endregion

            AccountManager = new AccountManager();
            NetworkManager = new NetworkManager();
            ScheduleManager = new ScheduleManager();

            // Default Views
            ProxyListItems = CollectionViewSource.GetDefaultView(proxyListItems);
            GroupListItems = CollectionViewSource.GetDefaultView(groupListItems);
            AccountsListItems = CollectionViewSource.GetDefaultView(accountsListItems);
            AccountsListItems.Filter = (o) => 
                ((o as AccountModel)?.Temporary ?? 0) < 1;

            SocksProxyListItems = new CollectionViewSource()
                { Source = proxyListItems }.View;
            SocksProxyListItems.Filter = (o) => (o as ProxyModel)?.Type.Equals("SOCKS") ?? false;

            // Data Contexts
            AccountsList.DataContext = this;
            AccountTabDetails.DataContext = AccountOpen;

            LoadAccounts();
            LoadGroups();
            LoadProxies();

            LoadSettings();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Log("RS Master 1.0.0");
            Log("OSBot Version: " + (OSBotHelper.GetLocalBotVersion() ?? "Unknown"));
            Log("HWID: " + Util.GetHWID());

            CheckJavaExists();
            ShowLoginDialog();
        }

        private void CheckJavaExists()
        {
            if (OSBotHelper.JavaInPath())
                return;

            if (!OSBotHelper.JavaInstalled())
            {
                Log("Warning: Java doesn't seem to be installed on your system, please install it to ensure full functionality.");
                return;
            }

            var javaPath = OSBotHelper.GetJavaInstallPath();
            if (javaPath != null)
            {
                OSBotHelper.SetJavaSystemPath(javaPath);
                Log("Notice: Java path was not set in your system env, automatically set.");
            }
        }

        private void LoadSettings()
        {
            Settings = Settings.GetSettings();

            var osbotUserLocation = OSBotHelper.UserLocation;
            if (OSBotHelper.UserLocationExists() && string.IsNullOrEmpty(Settings.BotBaseLocation))
            {
                Settings.BotBaseLocation = osbotUserLocation;
                TxtBoxBotLocation.PreviewMouseDown -= TxtBoxBotLocation_MouseDown;
            }

            // Set data contexts
            SettingsTab.DataContext = Settings;
            ClientOptionsTab.DataContext = Settings;
        }

        public void LoginSuccessPostEvent()
        {
            ScheduleManager.Begin();
            LoadPlugins();
        }

        internal void LoadPlugins()
        {
            var helper = new PluginsHelper();
            foreach (var plugin in helper.FetchPlugins())
            {
                try
                {
                    new Api.CoreBase().LaunchPlugin(plugin);
                }
                catch (Exception e)
                {
                }
            }
        }

        internal void LoadGroups()
        {
            Invoke(() =>
            {
                groupListItems.Clear();
                DataProvider.GetModels<GroupModel>("groups").ToList().ForEach(x => groupListItems.Add(x));
            });
        }

        internal void LoadAccounts()
        {
            Invoke(() =>
            {
                accountsListItems.Clear();
                DataProvider.GetAccounts().ToList().ForEach(x => accountsListItems.Add(x));
            });
        }

        internal void LoadProxies()
        {
            Invoke(() =>
            {
                proxyListItems.Clear();
                DataProvider.GetModels<ProxyModel>("proxies").ToList().ForEach(x => proxyListItems.Add(x));
            });
        }

        private void Log(string text)
        {
            var dateTime = DateTime.Now.ToString("HH:mm:ss");
            Invoke(() => Console.AppendText(dateTime + ": " + text + Environment.NewLine));
        }

        private void ShowLoginDialog()
        {
            LoginDialog = new Dialogs.LoginDialog(this);
            this.ShowMetroDialogAsync(LoginDialog);
        }

        #region Helpers

        private IEnumerable<AccountModel> GetLoadedAccounts()
            => Invoke(() => accountsListItems);

        private GroupModel GetGroupById(int groupId)
        {
            return Invoke(() => groupListItems.FirstOrDefault(x => x.Id == groupId));
        }

        private AccountModel GetSelectedAccount()
        {
            return Invoke(() => AccountsList.SelectedItem) as AccountModel;
        }

        private IEnumerable<AccountModel> GetSelectedAccounts()
        {
            return Invoke(() => AccountsList.SelectedItems).OfType<AccountModel>();
        }

        private void UpdateAccount(AccountModel account)
        {
            if (DataProvider.UpdateAccount(account))
            {
                Invoke(() =>
                {
                    var item = accountsListItems.FirstOrDefault(x => x.Id == account.Id);
                    if (item != null)
                    {
                        Invoke(() =>
                            Util.UpdateObjByProps(account, item, false));
                    }
                });
            }
        }

        private void AppShutdown()
        {
            Invoke(Application.Current.Shutdown);
        }

        internal void AddAccountToList(AccountModel account) => Invoke(() => accountsListItems.Insert(0, account));

        internal void StopAccount(int? accountId = null)
        {
            if (accountId.HasValue)
            {
                var item = Invoke(() => accountsListItems.FirstOrDefault(x => x.Id == accountId));
                if (item != null)
                {
                    AccountManager.StopAccount(item);
                }
            }
            else
            {
                var item = GetSelectedAccount();
                if (item != null)
                {
                    AccountManager.StopAccount(item);
                }
            }
        }

        internal async Task LaunchAccount(AccountModel account, bool autoLaunch = false)
        {
            #region Validation

            if (!OSBotHelper.LocalBotExists())
            {
                Log("Cannot find local OSBot Jar, please run updater to obtain the latest.");

                return;
            }

            if (Util.AnyStringNullOrEmpty
                (account.Username, account.Password, Settings.Username, Settings.Password))
            {
                Log("Missing OSBot client or accounts details.");

                return;
            }

            #endregion

            if (account.ProxyEnabled != 0
                && !string.IsNullOrEmpty(account.ProxyName))
            {
                account.Proxy = Invoke(() => proxyListItems.FirstOrDefault(x => x.Alias == account.ProxyName));
            }

            var result = await AccountManager.LaunchAccount(account, autoLaunch);
            if (result.success)
            {
                Log("Account launched!");
                return;
            }

            #region Error Handling

            switch (result.errorCode)
            {
                case 2:
                    if (!account.AutoLaunched)
                    {
                        var dialogResult = await ShowMessageDialog
                            ("OSBot Client Update", "Your OSBot client is out of date, update now?");

                        if (dialogResult)
                        {
                            updatesRequest = true;
                            Application.Current.Shutdown();
                        }
                    }
                    else
                    {
                        Log("Your OSBot client is out of date, please close the program and run updater.");
                    }

                    break;

                case 3:
                    Log("Invalid OSBot client username or password!");
                    break;

                case 5:
                    Log("Web Walking has been updated, please manually run osbot.jar and install it through Boot UI.");
                    break;

                case 6:
                    Log("Your OSBot account is banned, can't launch accounts.");
                    break;

                default:
                    Log("Launching account " + account.Username + " timed out!");
                    break;
            }

            #endregion
        }

        internal async Task<bool> ShowMessageDialog
            (string title, string description, MessageDialogStyle messageDialogStyle = MessageDialogStyle.AffirmativeAndNegative)
        {
            var dialog = new Dialogs.MessageDialog(this, title, description, messageDialogStyle);
            await this.ShowMetroDialogAsync(dialog);
            await 
                dialog.WaitUntilUnloadedAsync();

            return dialog.Result;
        }

        internal async Task<string[]> RequestImportDialog()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return null;

            try
            {
                return (await Task.Run(() => File.ReadAllLines(dialog.FileName)));
            }
            catch (Exception ex)
            {
                Util.LogException(ex);
            }

            return null;
        }

        internal async Task ImportAccounts(bool temporary = false)
        {
            var lines = await RequestImportDialog();
            if (lines != null)
            {
                foreach (var line in lines)
                {
                    try
                    {
                        ImportAccount(line.Split('/'), temporary);
                    }
                    catch (Exception e)
                    {
                        Util.LogException(e);
                    }
                }
            }
        }

        private void ImportAccount(string[] args, bool temporary = false)
        {
            var account = new AccountModel
            {
                Name = "Imported",
                Temporary = Convert.ToInt32(temporary),
                Username = args[0],
                Password = args[1]
            };

            if (args.Length > 2 && args[2] != "null")
                account.BankPIN = args[2];

            if (args.Length > 3 && args[3] != "null")
                account.Script = args[3];

            if (args.Length > 4 && args[4] != "null")
            {
                int.TryParse(args[4], out int accountWorld);
                if (accountWorld > 0)
                {
                    account.World = accountWorld;
                }
            }

            if (args.Length > 5 && args[5] != "null")
            {
                var proxy = DataProvider.GetModels<ProxyModel>
                    ("proxies", new DataRequestFilter
                {
                    Conditions = new Dictionary<string, object> { { "Alias", args[5] } }

                }).FirstOrDefault();

                if (proxy != null)
                {
                    account.ProxyEnabled = 1;
                    account.ProxyName = proxy.Alias;
                }
            }

            var existingAccount = DataProvider.GetAccounts().FirstOrDefault(x => x.Username == account.Username);
            if (existingAccount is null
                && DataProvider.SaveAccount(account))
            {
                account = DataProvider.GetAccounts().FirstOrDefault();
                if (account != null)
                {
                    AddAccountToList(account);
                }
            }
        }

        private void Invoke(Action action) => Dispatcher.Invoke(action);

        private T Invoke<T>(Func<T> action)
        {
            T result = default(T);
            Dispatcher.Invoke(() => result = action.Invoke());
            return result;
        }

#endregion

#region UI Event Handlers

        private void ButtonBotSettings_Click(object sender, RoutedEventArgs e)
        {
            Security.AeonGuard.Begin();

            Invoke(() =>
            {
                if (AccountTab.IsVisible)
                {
                    AccountTab.Visibility = Visibility.Hidden;
                }

                if (ClientOptionsTab.IsVisible)
                {
                    ClientOptionsTab.Visibility = Visibility.Hidden;
                }

                SettingsTab.Visibility = (SettingsTab.IsVisible)
                    ? Visibility.Hidden : Visibility.Visible;

                if (!SettingsTab.IsVisible && !string.IsNullOrEmpty(TxtBoxAccountName.Text))
                {
                    AccountTab.Visibility = Visibility.Visible;
                }
            });
        }
         
        private void ButtonAccountSettings_Click(object sender, RoutedEventArgs e)
        {
            Security.AeonGuard.Begin();

            accountLoading = true;
            var item = GetSelectedAccount();
            if (item is null)
                return;

            var account = DataProvider.GetAccountById(item.Id);
            if (account is null)
                return;

            Invoke(() =>
            {
                var clearUp = false;
                if (AccountTab.IsVisible && AccountOpen != null && item.Id == AccountOpen.Id)
                {
                    AccountTab.Visibility = Visibility.Hidden;
                    clearUp = true;
                }
                else
                {
                    if (SettingsTab.IsVisible)
                    {
                        SettingsTab.Visibility = Visibility.Hidden;
                    }

                    if (ClientOptionsTab.IsVisible)
                    {
                        ClientOptionsTab.Visibility = Visibility.Hidden;
                    }

                    AccountTab.Visibility = Visibility.Visible;
                }

                Invoke(() => 
                    Util.UpdateObjByProps(item, AccountOpen, clearUp));
            });

            accountLoading = false;
        }

        private void ButtonClientOptions_Click(object sender, RoutedEventArgs e)
        {
            Security.AeonGuard.Begin();

            Invoke(() =>
            {
                if (AccountTab.IsVisible)
                {
                    AccountTab.Visibility = Visibility.Hidden;
                }

                if (SettingsTab.IsVisible)
                {
                    SettingsTab.Visibility = Visibility.Hidden;
                }

                ClientOptionsTab.Visibility = (ClientOptionsTab.IsVisible)
                    ? Visibility.Hidden : Visibility.Visible;

                if (!ClientOptionsTab.IsVisible && !string.IsNullOrEmpty(TxtBoxAccountName.Text))
                {
                    AccountTab.Visibility = Visibility.Visible;
                }
            });
        }

        private void ButtonServices_Click(object sender, RoutedEventArgs e)
        {
            Security.AeonGuard.Begin();

            if (ServicesWindow != null)
            {
                if (ServicesWindow.IsVisible)
                {
                    ServicesWindow.Activate();
                }
                else
                {
                    ServicesWindow.Visibility = Visibility.Visible;
                }

                return;
            }

            ServicesWindow = new ServicesWindow(this);
            ServicesWindow.Show();
        }

        private void ButtonProxySettings_Click(object sender, RoutedEventArgs e)
        {
            Security.AeonGuard.Begin();

            if (ProxyWindow != null && ProxyWindow.IsVisible)
            {
                ProxyWindow.Activate();
                return;
            }

            ProxyWindow = new ProxyWindow(this);
            ProxyWindow.Show();
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            if (Settings.KillClientsOnExit)
            {
                var runningAccounts = Invoke(() => 
                    accountsListItems.Where(x => AccountManager.IsRunning(x.Username))).ToArray();

                for (int i = 0; i < runningAccounts.Length; i++)
                {
                    AccountManager.StopAccount(runningAccounts[i]);
                }
            }

            if (updatesRequest)
            {
                Task.Run(() => Process.Start(Util.AssemblyDirectory + @"\Updater.exe"));
            }
        }

        private async void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            if (ShutdownInProgress || !this.IsVisible)
                return;

            e.Cancel = true;
            ShutdownInProgress = true;

            if (await ShowMessageDialog("Exit RSMaster", "Are you sure you'd like to exit?"))
            {
                Application.Current.Shutdown();

                return;
            }

            ShutdownInProgress = false;
        }

        private void TxtBoxBotLocation_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var browserDialog = new FolderBrowserDialog
            {
                ShowNewFolderButton = false,
                Description = "OSBot Base Location Directory"
            };

            if (browserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Settings.BotBaseLocation = browserDialog.SelectedPath;
                Invoke(() => TxtBoxBotLocation.Text = browserDialog.SelectedPath);
                Settings.Save();
            }
        }

        private void AccountDetails_Changed(object sender, RoutedEventArgs e)
        {
            if (accountLoading)
                return;

            var account = Invoke(() => AccountOpen);
            if (account != null 
                && !GetLoadedAccounts().Any(x => x.Username == account.Username && x.Id != account.Id))
            {
                UpdateAccount(account);
            }
        }

        private void Settings_Changed(object sender, RoutedEventArgs e)
        {
            Settings.Save();
        }

        private async void ButtonDelAccount_Click(object sender, RoutedEventArgs e)
        {
            Security.AeonGuard.Begin();

            var items = GetSelectedAccounts().ToList();
            if (!items.Any())
                return;

            if (items.Count > 1 
                && !await ShowMessageDialog($"Delete Accounts ({items.Count})", "Are you sure you'd like to delete these accounts?"))
            {
                return;
            }

            foreach (var item in items)
            {
                if (DataProvider.DeleteAccount(item.Id))
                {
                    Invoke(() =>
                    {
                        accountsListItems.Remove(item);
                        if (AccountTab.IsVisible)
                        {
                            AccountTab.Visibility = Visibility.Hidden;
                            Invoke(() => Util.UpdateObjByProps(null, AccountOpen, true));
                        }
                    });
                }
            }
        }
        
        private void ButtonAddAccount_Click(object sender, RoutedEventArgs e)
        {
            Security.AeonGuard.Begin();

            var account = new AccountModel("New Account");
            if (DataProvider.SaveAccount(account))
            {
                account = DataProvider.GetAccounts().FirstOrDefault();
                if (account != null)
                {
                    AddAccountToList(account);
                }
            }
        }

        private async void ButtonLaunchAccount_Click(object sender, RoutedEventArgs e)
        {
            Security.AeonGuard.Begin();

            var items = GetSelectedAccounts().ToList();
            if (items.Count > 1)
            {
                foreach (var account in items.Where(x => x != null))
                {
                    AccountManager.QueueAccount(account);
                }
            }
            else
            {
                var item = items.FirstOrDefault();
                if (item != null)
                {
                    await LaunchAccount(item);
                }
            }
        }

        private void ButtonStopAccount_Click(object sender, RoutedEventArgs e)
        {
            Security.AeonGuard.Begin();

            var items = GetSelectedAccounts().ToList();
            if (items.Any())
            {
                foreach (var id in items.Where(x => x != null).Select(x => x.Id))
                {
                    StopAccount(id);
                }
            }
        }

        private void ButtonAccountSchedule_Click(object sender, RoutedEventArgs e)
        {
            Security.AeonGuard.Begin();

            var item = GetSelectedAccount();
            if (item != null)
            {
                SchedulerWindow = new SchedulerWindow(this, item);
                SchedulerWindow.ShowDialog();
            }
        }

        private void AccountVisibility_Click(object sender, RoutedEventArgs e)
        {
            var item = GetSelectedAccount();
            if (item != null && item.PID.HasValue)
            {
                var showWindowCommand = (!item.Visible) ? WinAPI.ShowWindowCommands.Show : WinAPI.ShowWindowCommands.Hide;
                WinAPI.ShowWindow(item.WindowHandle, showWindowCommand);

                Invoke(() => item.Visible = !item.Visible);
            }
        }

        private async void ButtonImportExistingAccounts_Click(object sender, RoutedEventArgs e)
        {
            await ImportAccounts();
        }

        private void ButtonAccountGroups_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new GroupsWindow(this);
            dialog.ShowDialog();
        }

        private void AccountsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Invoke(() =>
            {
                var item = GetSelectedAccount();
                if (AccountTab.Visibility == Visibility.Visible && item != AccountOpen)
                {
                    ButtonAccountSettings_Click(null, null);
                }
            });
        }

        private void ButtonOpenDiscord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/YfCwH6D");
        }

        private void ButtonOpenLicenses_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://selly.gg/g/091d43");
        }

        private async void SetDetailsMenu_Click(object sender, RoutedEventArgs e)
        {
            await this.ShowMetroDialogAsync
                (new Dialogs.CustomDetailsDialog(this));
        }

        #endregion
    }
}
