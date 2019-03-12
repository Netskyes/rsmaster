using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using MahApps.Metro.Controls;
using System.Collections.ObjectModel;

namespace RSMaster.UI
{
    using Data;
    using Models;
    using Utility;

    public partial class GroupsWindow : MetroWindow
    {
        public GroupModel OpenAccountGroup { get; set; } = new GroupModel();

        private MainWindow Host { get; set; }
        private bool groupLoading;

        public GroupsWindow(MainWindow host)
        {
            InitializeComponent();
            Host = host;

            GroupsList.DataContext = host;
            GroupSettings.DataContext = OpenAccountGroup;
        }
        
        private void NotifyGroupUpdate(int groupId)
        {
            foreach (var acc in Host.AccountsListItems)
            {
                var account = (acc as AccountModel);
                if (account != null 
                    && account.GroupId.HasValue && groupId == account.GroupId)
                {
                    account.GroupUpdated();
                }
            }
        }

        private void GroupDetailsChanged()
        {
            if (groupLoading)
                return;

            var group = Invoke(() => OpenAccountGroup);
            if (group != null)
            {
                if (DataProvider.UpdateModel(group, "groups"))
                {
                    Invoke(() =>
                    {
                        var item = Host.groupListItems.FirstOrDefault(x => x.Id == group.Id);
                        if (item != null)
                        {
                            Invoke(() =>
                            {
                                Util.UpdateObjByProps(group, item, false);
                                NotifyGroupUpdate(item.Id);
                            });
                        }
                    });
                }
            }
        }

        private void ButtonAddGroup_Click(object sender, RoutedEventArgs e)
        {
            var group = new GroupModel() { Name = "New Group", Color = "#000" };

            if (DataProvider.SaveModel(group, "groups"))
            {
                group = DataProvider.GetModels<GroupModel>("groups").FirstOrDefault();
                if (group != null)
                {
                    Invoke(() =>
                    {
                        Host.groupListItems.Add(group);
                        GroupsList.SelectedItem = group;
                    });
                }
            }
        }

        private void ButtonDeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            var item = Invoke(() => GroupsList.SelectedItem) as GroupModel;
            if (item != null)
            {
                if (DataProvider.DeleteModel(item.Id, "groups"))
                {
                    Invoke(() =>
                    {
                        Host.groupListItems.Remove(item);
                        NotifyGroupUpdate(item.Id);
                    });
                }
            }
        }

        private void TxtBoxGroupColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            var groupColor = (e.Source as TextBox).Text;

            try
            {
                var color = ColorConverter.ConvertFromString(groupColor);
                if (color != null)
                {
                    Invoke(() => CanvasColorView.Background = new SolidColorBrush((Color)color));
                }

                GroupDetailsChanged();
            }
            catch
            {
                Invoke(() => CanvasColorView.Background = Brushes.Black);
            }
        }

        private void TxtBoxGroupName_LostFocus(object sender, RoutedEventArgs e)
        {
            GroupDetailsChanged();
        }

        private void GroupsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            groupLoading = true;
            var item = Invoke(() => GroupsList.SelectedItem) as GroupModel;
            if (item != null)
            {
                var timeEvent = DataProvider.GetModels<GroupModel>("groups").FirstOrDefault(x => x.Id == item.Id);
                if (timeEvent != null)
                {
                    Invoke(() => Util.UpdateObjByProps(item, OpenAccountGroup, false));
                }
            }

            groupLoading = false;
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
