using System;
using System.ComponentModel;

namespace RSMaster.UI.Models
{
    using Data;

    public class UnlockModel : IViewModel, INotifyPropertyChanged
    {
        #region Fields

        private int id;
        private int? subemail;
        private string email;
        private string emailpassword;
        private string password;
        private string newpassword;
        private string emailprovider;

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

        public int? SubEmail
        {
            get => subemail;
            set
            {
                subemail = value;
                NotifyPropertyChanged("SubEmail");
            }
        }

        public string Email
        {
            get => email;
            set
            {
                email = value;
                NotifyPropertyChanged("Email");
            }
        }

        public string EmailPassword
        {
            get => emailpassword;
            set
            {
                emailpassword = value;
                NotifyPropertyChanged("EmailPassword");
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

        public string NewPassword
        {
            get => newpassword;
            set
            {
                newpassword = value;
                NotifyPropertyChanged("NewPassword");
            }
        }

        public string EmailProvider
        {
            get => emailprovider;
            set
            {
                emailprovider = value;
                NotifyPropertyChanged("EmailProvider");
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
