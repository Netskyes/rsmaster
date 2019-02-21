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

namespace RSMaster.UI
{
    public partial class AuthWindow : MetroWindow
    {
        private MainWindow Host { get; set; }

        public AuthWindow(MainWindow host)
        {
            InitializeComponent();

            Host = host;
            Host.Closed += Host_Closed;
        }

        private void Host_Closed(object sender, EventArgs e) => Close();
    }
}
