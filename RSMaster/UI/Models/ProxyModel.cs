using System.ComponentModel;

namespace RSMaster.UI.Models
{
    using Data;

    public class ProxyModel : IViewModel, INotifyPropertyChanged
    {
        #region Fields

        private int id;
        private string alias;
        private string host;
        private string port;
        private string username;
        private string password;
        private string type;

        #endregion

        public ProxyModel()
        {
        }

        public ProxyModel(string alias)
        {
            Alias = alias;
        }

        [PropUpdateIgnore]
        [PropInsertIgnore]
        public int Id
        {
            get => id;
            set
            {
                id = value;
                NotifyPropertyChanged("Id");
            }
        }

        public string Alias
        {
            get => alias;
            set
            {
                alias = value;
                NotifyPropertyChanged("Alias");
            }
        }

        public string Host
        {
            get => host;
            set
            {
                host = value;
                NotifyPropertyChanged("Host");
            }
        }

        public string Port
        {
            get => port;
            set
            {
                port = value;
                NotifyPropertyChanged("Port");
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

        public string Type
        {
            get => type;
            set
            {
                type = value;
                NotifyPropertyChanged("Type");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
