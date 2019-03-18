using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using MahApps.Metro.Controls.Dialogs;

namespace RSMaster.UI.Dialogs
{
    using Data;
    using Models;
    using System.ComponentModel;

    public partial class CustomDetailsDialog : BaseMetroDialog
    {
        public string Script { get; set; }
        public string ProxyName { get; set; }
        public int World { get; set; }
        public int EnableRandomWorld { get; set; }
        public int EnableProxy { get; set; }
        public ICollectionView SocksProxyListItems { get; set; }

        private MainWindow Host { get; set; }

        public CustomDetailsDialog(MainWindow host)
        {
            InitializeComponent();
            Host = host;

            SocksProxyListItems = CollectionViewSource.GetDefaultView(host.SocksProxyListItems);
            AccountDetails.DataContext = this;
        }

        private void ButtonSetDetails_Click(object sender, RoutedEventArgs e)
        {
            Update();
            Host.HideMetroDialogAsync(this);
        }

        private void Update()
        {
            var items = Invoke(() => Host.AccountsList.SelectedItems.OfType<AccountModel>().ToList());
            foreach (var account in items)
            {
                if (EnableRandomWorld > 0)
                {
                    account.World = Worlds.GetRandom();
                }
                else if (World > 0)
                {
                    account.World = World;
                }

                if (!string.IsNullOrEmpty(Script))
                {
                    account.Script = Script;
                }

                if (EnableProxy > 0)
                {
                    account.ProxyEnabled = EnableProxy;
                }

                if (!string.IsNullOrEmpty(ProxyName))
                {
                    account.ProxyName = ProxyName;
                }

                MainWindow.UpdateAccountHandler(account);
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Host.HideMetroDialogAsync(this);
        }

        private void Invoke(Action action) => Dispatcher.Invoke(action);

        private T Invoke<T>(Func<T> action)
        {
            T result = default(T);
            Dispatcher.Invoke(() => result = action.Invoke());
            return result;
        }
    }
}
