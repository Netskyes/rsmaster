using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;

namespace RSMaster.UI.Dialogs
{
    using Utility;
    using Security;

    public partial class LoginDialog : BaseMetroDialog
    {
        public string Username { get => authHandler?.Username ?? null; }
        public string Password { get => authHandler?.Password ?? null; }
        public string LicenseKey { get => authHandler?.LicenseKey ?? null; }

        private MainWindow Host { get; set; }
        private AuthHandler authHandler;
        private bool authorizing;
        private bool registering;

        public LoginDialog(MainWindow host)
        {
            InitializeComponent();

            Host = host;
            this.Width = Host.Width;

            authHandler = new AuthHandler();
            authHandler.OnStatusCode += StatusCodeCallback;
            authHandler.OnConnectError += ConnectErrorCallback;

            LoadSettings();
        }

        private void LoadSettings()
        {
            Invoke(() =>
            {
                var username = MainWindow.Settings.RSMasterUsername;
                var password = MainWindow.Settings.RSMasterPassword;
                var licenseKey = MainWindow.Settings.LicenseKey;

                TxtBoxUsername.Text = string.IsNullOrEmpty(username) ? string.Empty : Cryptography.AES.Decrypt(username);
                TxtBoxPassword.Password = string.IsNullOrEmpty(password) ? string.Empty : Cryptography.AES.Decrypt(password);
                TxtBoxLicenseKey.Text = string.IsNullOrEmpty(licenseKey) ? string.Empty : Cryptography.AES.Decrypt(licenseKey);

                ChkBoxRememberDetails.IsChecked = MainWindow.Settings.RememberLoginDetails;
            });
        }

        private void SaveLoginDetails()
        {
            Invoke(() =>
            {
                var remember = (ChkBoxRememberDetails.IsChecked == true);
                if (remember)
                {
                    MainWindow.Settings.RSMasterUsername = Cryptography.AES.Encrypt(TxtBoxUsername.Text);
                    MainWindow.Settings.RSMasterPassword = Cryptography.AES.Encrypt(TxtBoxPassword.Password);
                    MainWindow.Settings.LicenseKey = Cryptography.AES.Encrypt(TxtBoxLicenseKey.Text);
                    MainWindow.Settings.RememberLoginDetails = remember;

                    MainWindow.Settings.Save();
                }
            });
        }

        private void DisplayErrorMessage(string message)
        {
            Invoke(() =>
            {
                ErrorMessage.Height = Double.NaN;
                ErrorMessage.Visibility = Visibility.Visible;
                ErrorMessage.Text = message;
            });
        }

        private void HideErrors()
        {
            Invoke(() =>
            {
                ErrorMessage.Height = 0;
                ErrorMessage.Visibility = Visibility.Hidden;
                ErrorMessage.Text = string.Empty;
            });
        }

        private void ConnectErrorCallback()
        {
            DisplayErrorMessage("Connection timeout error");
        }

        private void StatusCodeCallback(byte code)
        {
            string message = string.Empty;
            switch (code)
            {
                case 1:
                    SaveLoginDetails();
                    Invoke(() => Host.HideMetroDialogAsync(this));
                    Host.LoginSuccessPostEvent();
                    break;

                case 2:
                    message = "Invalid username or password";
                    break;

                case 3:
                    message = "Invalid or expired license key";
                    break;

                case 4:
                    message = "License key not associate with this user account";
                    break;

                case 5:
                    message = "You're not allowed to login from this machine";
                    break;

                case 16:
                    message = "A user with that name already exists";
                    break;
                case 15:
                    HandleRegisterSuccess();
                    SwapContext();
                    break;

                default:
                    message = "An unknown error occured";
                    break;
            }

            if (message != string.Empty)
            {
                DisplayErrorMessage(message);
            }
        }

        private void HandleRegisterSuccess()
        {
            Invoke(() =>
            {
                TxtBoxUsername.Text = TxtBoxNewUsername.Text;
                TxtBoxPassword.Password = TxtBoxNewPassword.Password;

                TxtBoxNewUsername.Clear();
                TxtBoxNewPassword.Clear();
                TxtBoxNewPasswordConfirm.Clear();
            });
        }

        private void SwapContext()
        {
            Invoke(() =>
            {
                HideErrors();

                bool loginVisible = (LoginDialogArea.IsVisible);

                LoginDialogArea.Visibility = (loginVisible) ? Visibility.Hidden : Visibility.Visible;
                RegisterDialogArea.Visibility = (loginVisible) ? Visibility.Visible : Visibility.Hidden;
            });
        }

        private async void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            if (authorizing)
                return;

            var username = TxtBoxUsername.Text;
            var password = TxtBoxPassword.Password;
            var licenseKey = TxtBoxLicenseKey.Text;

            if (Util.AnyStringNullOrEmpty(username, password, licenseKey))
            {
                return;
            }

            HideErrors();
            authorizing = true;

            authHandler.SetAuth(username, password, licenseKey);
            await authHandler.RequestAuth();

            authorizing = false;
        }

        private void ButtonRegister_Click(object sender, RoutedEventArgs e)
        {
            SwapContext();
        }

        private void ButtonReturnToLogin_Click(object sender, RoutedEventArgs e)
        {
            SwapContext();
        }

        private async void ButtonConfirmRegister_Click(object sender, RoutedEventArgs e)
        {
            if (registering)
                return;

            var username = TxtBoxNewUsername.Text;
            var password = TxtBoxNewPassword.Password;
            var password2 = TxtBoxNewPasswordConfirm.Password;

            if (Util.AnyStringNullOrEmpty(username, password, password2))
            {
                DisplayErrorMessage("Please fill in all the fields");
                return;
            }

            if (username.Length < 3 || password.Length < 3)
            {
                DisplayErrorMessage("The length of username/password must be at least 3");
                return;
            }

            if (!password.Equals(password2))
            {
                DisplayErrorMessage("Passwords do not match");
                return;
            }

            HideErrors();
            registering = true;

            authHandler.SetAuth(username, password, null);
            await authHandler.RequestRegister();

            registering = false;
        }

        private void Invoke(Action action) => Dispatcher.Invoke(action);
    }
}
