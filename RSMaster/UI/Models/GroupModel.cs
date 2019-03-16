using System;
using System.ComponentModel;

namespace RSMaster.UI.Models
{
    using Data;

    public class GroupModel : IViewModel, INotifyPropertyChanged
    {
        #region Fields

        private int id;
        private int? world;
        private int proxyEnabled;
        private string name;
        private string color;
        private string proxyName;
        private string script;
        private int overrideLaunch;

        #endregion

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

        public string Name
        {
            get => name;
            set
            {
                name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public string Color
        {
            get => color;
            set
            {
                color = value;
                NotifyPropertyChanged("Color");
            }
        }

        public int? World
        {
            get => world;
            set
            {
                world = value;
                NotifyPropertyChanged("World");
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

        public int Override
        {
            get => overrideLaunch;
            set
            {
                overrideLaunch = value;
                NotifyPropertyChanged("Override");
            }
        }

        [PropUpdateIgnore, PropInsertIgnore]
        public DateTime Created { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
