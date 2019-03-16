using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MahApps.Metro.Controls;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace RSMaster.UI
{
    using Data;
    using Enums;
    using Models;
    using Services;
    using Utility;
    using Interfaces;
    using Helpers;
    using Objects;
    using Extensions;

    public partial class ServicesWindow : MetroWindow
    {
        public ICollectionView TaskListItems { get; set; }
        public ICollectionView AccountsImportList { get; set; }
        public ICollectionView AccountsUnlockList { get; set; }
        public ICollectionView SocksProxyListItems { get; set; }

        private MainWindow Host { get; set; }
        private ObservableCollection<UnlockModel> unlocksListItems = new ObservableCollection<UnlockModel>();
        private ObservableCollection<TaskModel> taskListItems = new ObservableCollection<TaskModel>();
        private readonly object taskListItemsLock = new object();

        internal IService Service { get; set; }
        internal RsWebHelper RsWebHelper { get; set; }

        public ServicesWindow(MainWindow host)
        {
            InitializeComponent();
            Host = host;
            Host.Closed += Host_Closed;

            AccountsImportList = new CollectionViewSource()
                { Source = host.accountsListItems }.View;
            AccountsImportList.Filter = (o) => ((o as AccountModel)?.Temporary ?? 0) > 0;

            SocksProxyListItems = new CollectionViewSource()
                { Source = host.proxyListItems }.View;
            SocksProxyListItems.Filter = (o) => (o as ProxyModel)?.Type.Equals("SOCKS") ?? false;

            AccountsUnlockList = CollectionViewSource.GetDefaultView(unlocksListItems);

            AccountCreationSettings.DataContext = MainWindow.Settings;
            ComboBoxCreateAccountProxy.ItemsSource = host.ProxyListItems;
            ComboBoxAccountDefaultGroup.ItemsSource = host.GroupListItems;

            TasksList.DataContext = this;
            TaskListItems = CollectionViewSource.GetDefaultView(taskListItems);

            ComboBoxServiceAction.Items.Add(new PairValueModel(0, "Create - Imported Accounts"));
            ComboBoxServiceAction.Items.Add(new PairValueModel(1, "Create - Email Prefix"));
            ComboBoxServiceAction.Items.Add(new PairValueModel(2, "Create - In-Built Method"));
            ComboBoxServiceAction.Items.Add(new PairValueModel(3, "Unlock Accounts"));

            LoadUnlockList();
        }

        internal void LoadUnlockList()
        {
            Invoke(() =>
            {
                unlocksListItems.Clear();
                DataProvider.GetModels<UnlockModel>("unlocks").ToList().ForEach(x => unlocksListItems.Add(x));
            });
        }

        public void AddTask(TaskModel task)
        {
            lock (taskListItemsLock)
            {
                Invoke(() => taskListItems.Add(task));
            }
        }

        public void DeleteTaskById(int id)
        {
            lock (taskListItemsLock)
            {
                Invoke(() =>
                {
                    var task = taskListItems.FirstOrDefault(x => x.Id == id);
                    if (task != null)
                    {
                        taskListItems.Remove(task);
                    }
                });
            }
        }

#region Account Service Event Handler

        private void AccountService_OnStatusUpdate(ServiceStatusCode statusCode, IRuneScapeForm form = null, string message = null)
        {
            if (statusCode == ServiceStatusCode.Updated)
            {
                Invoke(() =>
                {
                    var task = taskListItems.FirstOrDefault(x => x.GUID == form.RequestId);
                    if (task != null)
                    {
                        task.Description = message;
                    }
                });
            }
            else if (statusCode == ServiceStatusCode.Started)
            {
                AddTask(new TaskModel()
                {
                    GUID = form.RequestId,
                    Name = form.Email,
                    Description = "Awaiting...",
                    IsRunning = true
                });
            }
            else if (statusCode == ServiceStatusCode.Success)
            {
                var task = Invoke(() => taskListItems.FirstOrDefault(x => x.GUID == form.RequestId));
                if (task != null)
                {
                    task.Description = "Complete";

                    var service = (Service as AccountService);
                    if (service.AccountServiceType == AccountServiceType.Creation)
                    {
                        HandleCreate(form as RSAccountForm);
                    }
                    else
                    {
                        Invoke(() =>
                        {
                            var item = unlocksListItems.FirstOrDefault(x => x.Email == form.Email);
                            if (item != null)
                            {
                                unlocksListItems.Remove(item);
                            }
                        });
                    }
                }
            }
            else if (statusCode == ServiceStatusCode.Complete)
            {
                ConsoleLog("Stopped");
            }
        }

#endregion

#region Event Handlers

        private void Host_Closed
            (object sender, EventArgs e) => Close();

        private void Settings_Changed(object sender, RoutedEventArgs e)
            => MainWindow.Settings.Save();

        private async void ButtonStartService_Click(object sender, RoutedEventArgs e)
        {
            Invoke(() => Console.Clear());

            if (!(Service is null) && Service.IsRunning)
            {
                ConsoleLog($"{Service.Name} is already running...");

                return;
            }

            if (string.IsNullOrEmpty(MainWindow.Settings.CaptchaApiKey))
            {
                ConsoleLog("Please set your 2captcha api key under settings");

                return;
            }

            var balanceResult = await CaptchaHelper.GetCaptchaBalance();
            if (balanceResult == "ERROR_ZERO_BALANCE")
            {
                ConsoleLog("Not enough balance to perform 2captcha operations");

                return;
            }

            var servActionId = (int)(Invoke(() => ComboBoxServiceAction.SelectedItem as PairValueModel)?.Key ?? -1);
            if (servActionId < 0)
                return;

            var accountService = (Service = new AccountService()) as AccountService;
            accountService.OnStatusUpdate += AccountService_OnStatusUpdate;

            switch (servActionId)
            {
                case 0:
                    QueueImportedAccounts();
                    break;

                case 1:
                    GenerateQueuePrefixEmails();
                    break;

                case 2:
                    GenerateAndQueueAccounts();
                    break;

                case 3:
                    QueueUnlockAccounts();
                    accountService.AccountServiceType = AccountServiceType.Unlocking;
                    break;
            }

            var result = await Service.Start();
            if (!result)
            {
                ConsoleLog(Service.LastError);
                return;
            }

            ConsoleLog($"{Service.Name} started!");
            Invoke(() => ServiceTabs.SelectedIndex = 3);
        }

        private void ButtonStopService_Click(object sender, RoutedEventArgs e)
        {
            Service?.Stop();
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            if (Host != null && !Host.ShutdownInProgress)
            {
                e.Cancel = true;
                Visibility = Visibility.Hidden;
            }
        }

        private async void ButtonImportList_Click(object sender, RoutedEventArgs e)
        {
            await Host.ImportAccounts(true);
        }

        private void ButtonDeleteImported_Click(object sender, RoutedEventArgs e)
        {
            var item = Invoke(() => AccountsList.SelectedItem) as AccountModel;
            if (item != null)
            {
                if (DataProvider.DeleteAccount(item.Id))
                {
                    Invoke(() => Host.accountsListItems.Remove(item));
                }
            }
        }

        private void ButtonClearStatuses_Click(object sender, RoutedEventArgs e)
        {
            Invoke(() => taskListItems.Clear());
        }

        private async void ButtonImportAccountUnlock_Click(object sender, RoutedEventArgs e)
        {
            var lines = await Host.RequestImportDialog();
            if (lines != null)
            {
                foreach (var line in lines)
                {
                    try
                    {
                        ImportUnlock(line.Split('/'));
                    }
                    catch (Exception ex)
                    {
                        Util.LogException(ex);
                    }
                }
            }
        }

        private void ButtonDeleteAccountUnlock_Click(object sender, RoutedEventArgs e)
        {
            var item = Invoke(() => UnlocksList.SelectedItem) as UnlockModel;
            if (item != null)
            {
                if (DataProvider.DeleteModel(item.Id, "unlocks"))
                {
                    Invoke(() => unlocksListItems.Remove(item));
                }
            }
        }

#endregion

        private void GenerateQueuePrefixEmails()
        {
            var (provider, prefix, min, max) = GetPrefixDetails();
            var random = new Random();

            for (int i = min; i <= max; i++)
            {
                var account = new RSAccountForm()
                {
                    Email = prefix + i + provider,
                    Password = Util.RandomString(random.Next(7, 14)).ToLower()
                };

                (Service as AccountService).QueueReqForm(account);
            }
        }

        private void QueueUnlockAccounts()
        {
            var unlocks = Invoke(() => AccountsUnlockList).OfType<UnlockModel>();
            if (unlocks.Count() < 1)
                return;

            foreach (var unlock in unlocks)
            {
                var masterEmail = (unlock.SubEmail.HasValue 
                    && unlock.SubEmail == 1) ? Regex.Replace(unlock.Email, @"\+.*(?=\@)", "") : null;
                
                (Service as AccountService).QueueReqForm(new RSRecoveryForm
                {
                    Email = unlock.Email,
                    EmailPassword = unlock.EmailPassword,
                    NewPassword = unlock.NewPassword,
                    Provider = unlock.EmailProvider,
                    MasterEmail = masterEmail
                });
            }
        }

        private void GenerateAndQueueAccounts()
        {
            var random = new Random();
            int getRandom(int min, int max) => random.Next(min, max);

            var amount = MainWindow.Settings.AccountServiceLimit != 0 ? MainWindow.Settings.AccountServiceLimit : 100;
            var names = Properties.Resources.names.Split
                (new string[] { "\n" }, StringSplitOptions.None).Select(x => x.Replace("\r", "")).ToArray();
            var emails = Properties.Resources.emails.Split
                (new string[] { "\n" }, StringSplitOptions.None).Select(x => x.Replace("\r", "")).ToArray();

            for (int i = 0; i < amount; i++)
            {
                var name = names[getRandom(0, names.Length)];
                var email = emails[getRandom(0, emails.Length)];

                var salt = Util.RandomString(getRandom(2, 4));
                var password = Util.RandomString(getRandom(7, 14)).ToLower();

                var username = ((name + salt).Shuffle() + "@" + email).ToLower();
                var account = new RSAccountForm()
                {
                    Email = username,
                    Password = password
                };

                // Queue account for creation
                (Service as AccountService).QueueReqForm(account);
            }
        }

        private void QueueImportedAccounts()
        {
            var accounts = Invoke(() => AccountsImportList).OfType<AccountModel>();
            if (accounts.Count() < 1)
                return;
            
            foreach (var account in accounts)
            {
                (Service as AccountService).QueueReqForm(new RSAccountForm
                {
                    Email = account.Username,
                    Password = account.Password,
                    ProxyName = account.ProxyName
                });
            }
        }

        private void HandleCreate(RSAccountForm account)
        {
            var settings = MainWindow.Settings;

            var accountModel = DataProvider.GetAccounts().FirstOrDefault(x => x.Username == account.Email);
            if (accountModel is null)
            {
                accountModel = new AccountModel("New Account")
                {
                    Username = account.Email,
                    Password = account.Password
                };
            }
            
            if (settings.AccountDefaultWorld > 0 
                && (accountModel.World is null || accountModel.World == 0))
            {
                int world = settings.AccountDefaultWorld;
                if (world > 0 && (world >= 301 && world <= 525))
                {
                    accountModel.World = world;
                }
            }

            if (string.IsNullOrEmpty(accountModel.ProxyName)
                && settings.CreateAccountUseProxy 
                && !string.IsNullOrEmpty(settings.CreateAccountProxyName))
            {
                accountModel.ProxyEnabled = 1;
                accountModel.ProxyName = settings.CreateAccountProxyName;
            }

            if (string.IsNullOrEmpty(accountModel.Script)
                && !string.IsNullOrEmpty(settings.AccountDefaultScript))
            {
                accountModel.Script = settings.AccountDefaultScript;
            }

            if (!accountModel.GroupId.HasValue 
                && settings.AccountDefaultGroupId > 0)
            {
                accountModel.GroupId = settings.AccountDefaultGroupId;
            }
            
            if (accountModel.Id == 0) 
            {
                if (DataProvider.SaveAccount(accountModel))
                {
                    accountModel = DataProvider.GetAccounts().FirstOrDefault(x => x.Username == accountModel.Username);
                    if (accountModel != null)
                    {
                        Host.AddAccountToList(accountModel);
                        MainWindow.AccountCreatedCallback?.Invoke(accountModel);
                    }
                }
            }
            else
            {
                accountModel.Temporary = 0;
                if (DataProvider.UpdateAccount(accountModel))
                {
                    Invoke(() =>
                    {
                        var item = Host.accountsListItems.FirstOrDefault(x => x.Username == accountModel.Username);
                        if (item != null)
                        {
                            Util.UpdateObjByProps(accountModel, item, false);

                            AccountsImportList.Refresh();
                            Host.AccountsListItems.Refresh();
                        }
                    });
                }
            }

            if (settings.LaunchAccountOnCreate)
            {
                var item = MainWindow.GetAccountsHandler().FirstOrDefault(x => x.Id == accountModel.Id);
                if (item != null)
                {
                    Task.Run(async () => await Host.LaunchAccount(item, true));
                }
            }
        }

        public void ImportUnlock(string[] args)
        {
            var unlock = new UnlockModel()
            {
                Email = args.Take(0),
                EmailPassword = args.Take(1),
                Password = args.Take(2),
                NewPassword = args.Take(3),
                EmailProvider = args.Take(4),
                SubEmail = (args.Take(5) == "sub") ? 1 : 0
            };

            UnlockModel getExistingUnlock()
            {
                var existingUnlock = DataProvider.GetModels<UnlockModel>("unlocks", new DataRequestFilter
                {
                    Conditions = new Dictionary<string, object> { { "Email", unlock.Email } }
                });

                return existingUnlock.FirstOrDefault();
            }

            if (getExistingUnlock() is null
                && DataProvider.SaveModel(unlock, "unlocks"))
            {
                unlock = getExistingUnlock();
                if (unlock != null)
                {
                    Invoke(() => unlocksListItems.Add(unlock));
                }
            }
        }

#region Helpers

        private void ConsoleLog(string text)
        {
            Invoke(() => Console.AppendText(text + Environment.NewLine));
        }

        private (string provider, string prefix, int min, int max) GetPrefixDetails()
        {
            string prefix = null, 
                   provider = null;
            int min = 0, 
                max = 0;
            
            Invoke(() =>
            {
                prefix = TxtBoxEmailPrefix.Text;
                provider = TxtBoxEmailProvider.Text;
                int.TryParse(TxtBoxIncrementLow.Text, out min);
                int.TryParse(TxtBoxIncrementHigh.Text, out max);
            });

            return (provider, prefix, min, max);
        }

        private void Invoke(Action action) => Dispatcher.Invoke(action);

        private T Invoke<T>(Func<T> action)
        {
            T result = default(T);
            Dispatcher.Invoke(() => result = action.Invoke());
            return result;
        }

#endregion
    }
}
