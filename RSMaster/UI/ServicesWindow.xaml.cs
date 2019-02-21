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

namespace RSMaster.UI
{
    using Models;
    using Services;
    using RuneScape.Models;
    using Utility;
    using Interfaces;
    using Helpers;

    public partial class ServicesWindow : MetroWindow
    {
        public IService Service { get; set; }
        public ICollectionView TaskListItems { get; set; }
        public ICollectionView HttpsProxyListItems { get; set; }
        public ICollectionView AccountsImportList { get; set; }

        private MainWindow Host { get; set; }
        private ObservableCollection<TaskModel> taskListItems = new ObservableCollection<TaskModel>();
        private readonly object taskListItemsLock = new object();
        private RsWebHelper RsWebHelper { get; set; }

        public ServicesWindow(MainWindow host)
        {
            InitializeComponent();
            Host = host;
            Host.Closed += Host_Closed;

            HttpsProxyListItems = new CollectionViewSource()
                { Source = host.proxyListItems }.View;
            HttpsProxyListItems.Filter = (o) => (o as ProxyModel)?.Type.Contains("HTTP") ?? false;

            AccountsImportList = new CollectionViewSource()
                { Source = host.accountsListItems }.View;
            AccountsImportList.Filter = (o) => ((o as AccountModel)?.Temporary ?? 0) > 0;

            ComboBoxAccountDefaultProxy.DataContext = host;
            AccountCreationSettings.DataContext = MainWindow.Settings;

            TasksList.DataContext = this;
            TaskListItems = CollectionViewSource.GetDefaultView(taskListItems);

            ComboBoxAccountCreationMethod.Items.Add(new PairValueModel(0, "Imported Accounts"));
            ComboBoxAccountCreationMethod.Items.Add(new PairValueModel(1, "Email Prefix"));
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

        #region Account Creation Event Handler

        private void AccountService_OnStatusUpdate(CreationStatusCode statusCode, RSAccountForm account = null, string message = null)
        {
            if (statusCode == CreationStatusCode.Updated)
            {
                Invoke(() =>
                {
                    var task = taskListItems.FirstOrDefault(x => x.GUID == account.RequestId);
                    if (task != null)
                    {
                        task.Description = message;
                    }
                });
            }
            else if (statusCode == CreationStatusCode.Started)
            {
                AddTask(new TaskModel()
                {
                    GUID = account.RequestId,
                    Name = account.Email,
                    Description = "Awaiting...",
                    IsRunning = true
                });
            }
            else if (statusCode == CreationStatusCode.Created)
            {
                var task = Invoke(() => taskListItems.FirstOrDefault(x => x.GUID == account.RequestId));
                if (task != null)
                {
                    task.Description = "Complete";
                    HandleCreate(account);
                }
            }
            else if (statusCode == CreationStatusCode.Complete)
            {
                ConsoleLog("Completed");
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
                ConsoleLog("Account creation in progress...");

                return;
            }

            if (string.IsNullOrEmpty(MainWindow.Settings.CaptchaApiKey))
            {
                ConsoleLog("Please set your 2captcha api key under settings");

                return;
            }

            RsWebHelper = new RsWebHelper();

            var balanceResult = await RsWebHelper.GetCaptchaBalance();
            if (balanceResult == "ERROR_ZERO_BALANCE")
            {
                ConsoleLog("Not enough balance to perform 2captcha operations");

                return;
            }

            var item = Invoke(() =>
                ComboBoxAccountCreationMethod.SelectedItem) as PairValueModel;
            if (item is null)
                return;

            var accountService = (Service = new AccountService(this)) as AccountService;
            accountService.RsWebHelper = RsWebHelper;
            accountService.OnStatusUpdate += AccountService_OnStatusUpdate;

            switch ((int)item.Key)
            {
                case 0:
                    QueueImportedAccounts();
                    break;

                case 1:

                    break;
            }

            var result = await Service.Start();
            if (!result)
            {
                ConsoleLog(Service.LastError);

                return;
            }

            ConsoleLog("Started creating accounts!");
            Invoke(() => 
                ServiceTabs.SelectedIndex = 2);
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

        #endregion

        private void GenerateAndQueueAccounts()
        {
            var random = new Random();
            int getRandom(int min, int max) => random.Next(min, max);

            var amount = MainWindow.Settings.AccountCreateLimit != 0 ? MainWindow.Settings.AccountCreateLimit : 100;
            var names = Properties.Resources.names.Split
                (new string[] { "\n" }, StringSplitOptions.None).Select(x => x.Replace("\r", "")).ToArray();
            var emails = Properties.Resources.emails.Split
                (new string[] { "\n" }, StringSplitOptions.None).Select(x => x.Replace("\r", "")).ToArray();

            for (int i = 0; i < amount; i++)
            {
                var name = names[getRandom(0, names.Length)];
                var email = emails[getRandom(0, emails.Length)];

                var salt = Util.RandomString(getRandom(2, 4));
                var password = Util.RandomString(getRandom(7, 14));

                var username = ((name + salt).Shuffle() + "@" + email).ToLower();
                var account = new RSAccountForm()
                {
                    Email = username,
                    Password = password
                };

                // Queue account for creation
                (Service as AccountService).QueueAccount(account);
            }
        }

        private void QueueImportedAccounts()
        {
            var accounts = Invoke(() => AccountsImportList).OfType<AccountModel>();
            if (accounts.Count() < 1)
                return;
            
            foreach (var account in accounts)
            {
                (Service as AccountService).QueueAccount(new RSAccountForm
                {
                    Email = account.Username,
                    Password = account.Password
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

            if (settings.AccountDefaultEnableProxy 
                && !string.IsNullOrEmpty(settings.AccountDefaultProxy))
            {
                accountModel.ProxyName = settings.AccountDefaultProxy;
                accountModel.ProxyEnabled = 1;
            }

            if (!string.IsNullOrEmpty
                (settings.AccountDefaultScript) 
                && string.IsNullOrEmpty(accountModel.Script))
            {
                accountModel.Script = settings.AccountDefaultScript;
            }
            
            if (accountModel.Id == 0) 
            {
                if (DataProvider.SaveAccount(accountModel))
                {
                    accountModel = DataProvider.GetAccounts().FirstOrDefault(x => x.Username == accountModel.Username);
                    if (accountModel != null)
                    {
                        Host.AddAccountToList(accountModel);
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
                Task.Run(async () => await Host.LaunchAccount(accountModel, true));
            }
        }

        #region Helpers

        private void ConsoleLog(string text)
        {
            Invoke(() => Console.AppendText(text + Environment.NewLine));
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
