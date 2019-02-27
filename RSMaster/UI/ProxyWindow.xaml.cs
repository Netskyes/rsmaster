using System;
using System.Linq;
using System.Windows;
using MahApps.Metro.Controls;

namespace RSMaster.UI
{
    using Utility;
    using UI.Models;

    public partial class ProxyWindow : MetroWindow
    {
        private MainWindow Host { get; set; }

        public ProxyWindow(MainWindow host)
        {
            InitializeComponent();

            Host = host;
            Host.Closed += Host_Closed;

            ProxiesList.DataContext = Host;
            ComboBoxProxyType.Items.Add("SOCKS");
            ComboBoxProxyType.Items.Add("HTTP");
            //ComboBoxProxyType.Items.Add("HTTPS");
        }

        private void Host_Closed(object sender, EventArgs e) 
            => Close();

        private ProxyModel GetSelectedProxy()
        {
            return Invoke(() => ProxiesList.SelectedItem) as ProxyModel;
        }

        private ProxyModel GetProxyDetails()
        {
            var proxy = new ProxyModel();
            Invoke(() =>
            {
                proxy.Alias = TxtBoxProxyAlias.Text != string.Empty ? TxtBoxProxyAlias.Text : null;
                proxy.Host = TxtBoxProxyHost.Text != string.Empty ? TxtBoxProxyHost.Text : null;
                proxy.Port = TxtBoxProxyPort.Text != string.Empty ? TxtBoxProxyPort.Text : null;
                proxy.Username = TxtBoxProxyUsername.Text != string.Empty ? TxtBoxProxyUsername.Text : null;
                proxy.Password = TxtBoxProxyPassword.Text != string.Empty ? TxtBoxProxyPassword.Text : null;
                proxy.Type = ComboBoxProxyType.Text;
            });

            return proxy;
        }

        private void SetProxyDetails(ProxyModel model)
        {
            if (model is null)
                return;

            Invoke(() =>
            {
                TxtBoxProxyAlias.Text = model.Alias;
                TxtBoxProxyHost.Text = model.Host;
                TxtBoxProxyPort.Text = model.Port;
                TxtBoxProxyUsername.Text = model.Username;
                TxtBoxProxyPassword.Text = model.Password;
                ComboBoxProxyType.SelectedIndex = Math.Max(ComboBoxProxyType.Items.IndexOf(model.Type), 0);
            });
        }

        private void ClearProxyDetails()
        {
            Invoke(() =>
            {
                TxtBoxProxyAlias.Clear();
                TxtBoxProxyHost.Clear();
                TxtBoxProxyPort.Clear();
                TxtBoxProxyUsername.Clear();
                TxtBoxProxyPassword.Clear();
                ComboBoxProxyType.SelectedIndex = 0;
            });
        }

        private void ButtonSaveProxy_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedProxy();
            var proxyDetails = GetProxyDetails();

            if (proxyDetails.Alias is null 
                || proxyDetails.Host is null || proxyDetails.Port is null)
                return;

            if (selected != null)
            {
                proxyDetails.Id = selected.Id;
                if (DataProvider.UpdateModel(proxyDetails, "proxies"))
                {
                    Invoke(() =>
                    {
                        var item = Host.proxyListItems.FirstOrDefault(x => x.Id == selected.Id);
                        if (item != null)
                        {
                            Invoke(() =>
                            {
                                Util.UpdateObjByProps(proxyDetails, item, false);

                                Host.SocksProxyListItems.Refresh();
                                Host.ProxyListItems.Refresh();
                            });
                        }
                    });
                }
            }
            else
            {
                var existing = DataProvider.GetModels<ProxyModel>("proxies").FirstOrDefault
                    (x => x.Alias == proxyDetails.Alias);

                if (existing is null 
                    && DataProvider.SaveModel(proxyDetails, "proxies"))
                {
                    selected = DataProvider.GetModels<ProxyModel>("proxies").FirstOrDefault();
                    if (selected != null)
                    {
                        ClearProxyDetails();
                        Invoke(() => Host.proxyListItems.Add(selected));
                    }
                }
            }
        }

        private void ButtonDeleteProxy_Click(object sender, RoutedEventArgs e)
        {
            var item = Invoke(() => ProxiesList.SelectedItem) as ProxyModel;
            if (item != null)
            {
                if (DataProvider.DeleteModel(item.Id, "proxies"))
                {
                    ClearProxyDetails();
                    Invoke(() => Host.proxyListItems.Remove(item));
                }
            }
        }

        private void ButtonProxySettings_Click(object sender, RoutedEventArgs e)
        {
            var item = GetSelectedProxy();
            if (item != null)
            {
                SetProxyDetails(item);
            }
        }

        private void ButtonAddProxy_Click(object sender, RoutedEventArgs e)
        {
            ClearProxyDetails();
            Invoke(() =>
            {
                TxtBoxProxyHost.Focus();
                ProxiesList.UnselectAll();
            });
        }

        #region Helpers

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
