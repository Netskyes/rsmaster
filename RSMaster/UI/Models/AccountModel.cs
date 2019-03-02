using System;
using System.ComponentModel;
using RSMaster.Extensions;

namespace RSMaster.UI.Models
{
    public class AccountModel : IViewModel, INotifyPropertyChanged
    {
        #region Fields

        private string name;
        private string username;
        private string password;
        private string script;
        private string proxyName;
        private int proxyEnabled;
        private string bankPIN;
        private int? world;
        private int? pid;
        private int temporary;
        private int? groupId;
        private bool visible;

        #endregion

        public AccountModel()
        {
        }

        public AccountModel(string name)
        {
            Name = name;
        }

        [PropUpdateIgnore, PropInsertIgnore]
        public int Id { get; set; }

        public int? World
        {
            get => world;
            set
            {
                world = value;
                NotifyPropertyChanged("World");
            }
        }

        public string Name
        {
            get => name;
            set
            {
                name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public string Username
        {
            get => username;
            set
            {
                username = value;
                NotifyPropertyChanged("Username");
            }
        }

        public string Password
        {
            get => password;
            set
            {
                password = value;
                NotifyPropertyChanged("Password");
            }
        }

        public string Script
        {
            get => script;
            set
            {
                script = value;
                NotifyPropertyChanged("Script");
            }
        }

        public string ProxyName
        {
            get => proxyName;
            set
            {
                proxyName = value;
                NotifyPropertyChanged("ProxyName");
            }
        }

        public int ProxyEnabled
        {
            get => proxyEnabled;
            set
            {
                proxyEnabled = value;
                NotifyPropertyChanged("ProxyEnabled");
            }
        }

        public string BankPIN
        {
            get => bankPIN;
            set
            {
                bankPIN = value;
                NotifyPropertyChanged("BankPIN");
            }
        }

        public int Temporary
        {
            get => temporary;
            set
            {
                temporary = value;
                NotifyPropertyChanged("Temporary");
            }
        }

        public int? GroupId
        {
            get => groupId;
            set
            {
                groupId = value;
                NotifyPropertyChanged("GroupId");
            }
        }

        [PropUpdateIgnore, PropInsertIgnore]
        public int? PID
        {
            get => pid;
            set
            {
                pid = value;
                NotifyPropertyChanged("Status");
            }
        }

        [PropUpdateIgnore, PropInsertIgnore]
        public DateTime Created { get; set; }

        [PropUpdateIgnore, PropInsertIgnore]
        public ProxyModel Proxy { get; set; }

        [PropUpdateIgnore, PropInsertIgnore]
        public string Status
        {
            get { return (PID.HasValue) ? "Online" : "Offline"; }
        }

        [PropUpdateIgnore, PropInsertIgnore]
        public bool Visible
        {
            get => visible;
            set
            {
                visible = value;
                NotifyPropertyChanged("Visible");
            }
        }

        [PropUpdateIgnore, PropInsertIgnore]
        public bool AutoLaunched { get; set; }

        [PropUpdateIgnore, PropInsertIgnore]
        public IntPtr WindowHandle { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public void GroupUpdated() => NotifyPropertyChanged("GroupId");
    }
}
