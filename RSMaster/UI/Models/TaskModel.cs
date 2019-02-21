using System.ComponentModel;

namespace RSMaster.UI.Models
{
    public class TaskModel : IViewModel, INotifyPropertyChanged
    {
        #region Fields

        private int id;
        private string guid;
        private string name;
        private string description;
        private bool isrunning;

        #endregion

        public int Id
        {
            get => id;
            set
            {
                id = value;
                NotifyPropertyChanged("Id");
            }
        }

        public string GUID
        {
            get => guid;
            set
            {
                guid = value;
                NotifyPropertyChanged("GUID");
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

        public string Description
        {
            get => description;
            set
            {
                description = value;
                NotifyPropertyChanged("Description");
            }
        }

        public bool IsRunning
        {
            get => isrunning;
            set
            {
                isrunning = value;
                NotifyPropertyChanged("IsRunning");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
