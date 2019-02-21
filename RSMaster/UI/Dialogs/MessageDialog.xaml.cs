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
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace RSMaster.UI.Dialogs
{
    public partial class MessageDialog : BaseMetroDialog
    {
        public bool Result { get; set; }

        private MetroWindow Host { get; set; }

        public MessageDialog
            (MetroWindow host, string title, string description, MessageDialogStyle messageDialogStyle = MessageDialogStyle.AffirmativeAndNegative)
        {
            InitializeComponent();
            Host = host;
            this.Width = Host.Width;

            DialogTitle.Text = title;
            DialogDescription.Text = description;

            if (messageDialogStyle == MessageDialogStyle.Affirmative)
            {
                ButtonNo.Visibility = Visibility.Hidden;
                ButtonYes.Content = "Ok";
            }
        }

        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            Host.HideMetroDialogAsync(this);
        }

        private void ButtonNo_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            Host.HideMetroDialogAsync(this);
        }
    }
}
