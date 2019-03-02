using RSMaster.Extensions;
using System;
using System.ComponentModel;

namespace RSMaster.UI.Models
{
    public class GroupModel : IViewModel, INotifyPropertyChanged
    {
        #region Fields

        private int id;
        private string name;
        private string color;

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

        [PropUpdateIgnore, PropInsertIgnore]
        public DateTime Created { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
