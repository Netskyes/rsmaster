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

        #region Delegates

        internal static LogDelegate Log;
        internal static ShutdownDelegate Shutdown;
        internal static GetAccountByIdDelegate AccountGetById;
        internal static UpdateAccountDelegate AccountUpdate;
        internal static GetSelectedAccountDelegate AccountGetSelected;
        internal static LaunchAccountDelegate AccountLaunch;

        #endregion

        public ICollectionView AccountsListItems { get; set; }
        public ICollectionView ProxyListItems { get; set; }
        public ICollectionView SocksProxyListItems { get; set; }
        public AccountModel AccountOpen { get; set; } = new AccountModel();

        internal ObservableCollection<AccountModel> accountsListItems = new ObservableCollection<AccountModel>();
        internal ObservableCollection<ProxyModel> proxyListItems = new ObservableCollection<ProxyModel>();
        private bool accountLoading;
        private bool updatesRequest;

        public MainWindow()
        {
            InitializeComponent();
            Cryptography.AES.SetDefaultKey("ukjdpxw6");

            #region Delegates

            Log = ConsoleLog;
            Shutdown = AppShutdown;
            AccountGetById = GetAccountById;
            AccountGetSelected = GetSelectedAccount;
            AccountUpdate = UpdateAccount;
            AccountLaunch = LaunchAccount;

            #endregion

            AccountManager = new AccountManager();
            NetworkManager = new NetworkManager();
            ScheduleManager = new ScheduleManager();

            // Default Views
            ProxyListItems = CollectionViewSource.GetDefaultView(proxyListItems);
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
            LoadProxies();
            LoadSettings();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ConsoleLog("RS Master 1.0.0");
            ConsoleLog("OSBot Version: " + OSBotHelper.GetLocalBotVersion());
            ConsoleLog("HWID: " + Util.GetHWID());

            //ShowLoginDialog();
            ScheduleManager.Begin();
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

        private void ConsoleLog(string text)
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

        public void AddAccountToList(AccountModel account) => Invoke(() => accountsListItems.Add(account));

        private AccountModel GetAccountById(int accountId)
        {
            return Invoke(() => accountsListItems.FirstOrDefault(x => x.Id == accountId));
        }

        private AccountModel GetSelectedAccount()
        {
            return Invoke(() => AccountsList.SelectedItem) as AccountModel;
        }

        private void UpdateAccount(AccountModel accountModel)
        {
            Invoke(() =>
            {
                var item = accountsListItems.FirstOrDefault(x => x.Id == accountModel.Id);
                if (item != null)
                {
                    Util.UpdateObjByProps(accountModel, item, false);
                }
            });
        }

        public async Task<bool> ShowMessageDialog
            (string title, string description, MessageDialogStyle messageDialogStyle = MessageDialogStyle.AffirmativeAndNegative)
        {
            var dialog = new Dialogs.MessageDialog(this, title, description, messageDialogStyle);
            await this.ShowMetroDialogAsync(dialog);
            await 
                dialog.WaitUntilUnloadedAsync();

            return dialog.Result;
        }

        public async Task LaunchAccount(AccountModel account, bool autoLaunch = false)
        {
            #region Validation

            if (!OSBotHelper.LocalBotExists())
            {
                ConsoleLog("Cannot find local OSBot Jar, please run updater to obtain the latest.");

                return;
            }

            if (Util.AnyStringNullOrEmpty
                (account.Username, account.Password, Settings.Username, Settings.Password))
            {
                ConsoleLog("Missing OSBot client or accounts details.");

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
                ConsoleLog("Account launched!");
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
                        ConsoleLog("Your OSBot client is out of date, please close the program and run updater.");
                    }

                    break;

                case 3:
                    ConsoleLog("Invalid OSBot client username or password!");
                    break;

                case 5:
                    ConsoleLog("Web Walking has been updated, please manually run osbot.jar and install it through Boot UI.");
                    break;

                default:
                    ConsoleLog("Launching account " + account.Username + " timed out!");
                    break;
            }

            #endregion
        }

        public async Task ImportAccounts(bool temporary)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            try
            {
                await Task.Run(() =>
                {
                    var lines = File.ReadAllLines(dialog.FileName);
                    foreach (var line in lines)
                    {
                        var args = line.Split('/');
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
                });
            }
            catch (Exception ex)
            {
                Util.LogException(ex);
            }
        }

        private void AppShutdown()
        {
            Invoke(Application.Current.Shutdown);
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
                    Util.UpdateObjByProps
                    (account, AccountOpen, clearUp));
            });

            accountLoading = false;
        }

        private void ButtonClientOptions_Click(object sender, RoutedEventArgs e)
        {
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
            if (ShutdownInProgress)
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
            if (account != null)
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
        }

        private void Settings_Changed(object sender, RoutedEventArgs e)
        {
            Settings.Save();
        }

        private void ButtonDelAccount_Click(object sender, RoutedEventArgs e)
        {
            var item = GetSelectedAccount();
            if (item != null)
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
            var item = GetSelectedAccount();
            if (item == null)
                return;

            if (item != null)
            {
                await LaunchAccount(item);
            }
        }

        private void ButtonStopAccount_Click(object sender, RoutedEventArgs e)
        {
            var item = GetSelectedAccount();
            if (item != null)
            {
                AccountManager.StopAccount(item);
            }
        }

        private void ButtonAccountSchedule_Click(object sender, RoutedEventArgs e)
        {
            var item = GetSelectedAccount();
            if (item != null)
            {
                //if (SchedulerWindow != null && SchedulerWindow.IsVisible)
                //{
                //    SchedulerWindow.Activate();

                //    return;
                //}

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

        #endregion

        private async void ButtonImportExistingAccounts_Click(object sender, RoutedEventArgs e)
        {
            await ImportAccounts(false);
        }
    }
}
