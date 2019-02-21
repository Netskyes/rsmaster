using RSMaster.RuneScape.Models;
using RSMaster.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RSMaster.Services
{
    using Helpers;
    using Utility;

    internal enum CreationStatusCode
    {
        Error,
        Created,
        Started,
        Updated,
        Complete
    }

    internal class AccountService : ServiceBase
    {
        public RsWebHelper RsWebHelper { get; set; }
        public event CreationStatusUpdate OnStatusUpdate;
        public delegate void CreationStatusUpdate(CreationStatusCode statusCode, RSAccountForm account = null, string message = null);

        private ServicesWindow Host { get; set; }
        private readonly Queue<RSAccountForm> accountsQueue = new Queue<RSAccountForm>();
        private readonly object accountsQueueLock = new object();

        private List<string> runningRequests;
        private DateTime? lastBatchTime;

        public AccountService(ServicesWindow host)
        {
            Host = host;
            Name = "Account Service";
            Description = "Account Creation Service";

            lastBatchTime = null;
            runningRequests = new List<string>();
        }

        public void QueueAccount(RSAccountForm account)
        {
            account.RequestId = Guid.NewGuid().ToString();

            lock (accountsQueueLock)
            {
                accountsQueue.Enqueue(account);
            }
        }

        public IEnumerable<RSAccountForm> GetQueuedAccounts()
        {
            lock (accountsQueueLock)
            {
                foreach (var account in accountsQueue)
                {
                    yield return account;
                }
            }
        }

        protected override bool ServiceStartup()
        {
            var anyQueued = GetQueuedAccounts().Any();
            if (!anyQueued)
            {
                LastError = "No accounts in the queue, please import some";
            }

            return anyQueued;
        }

        protected override void RunService()
        {
            while (IsTokenAlive() && IsRunning)
            {
                lock (accountsQueueLock)
                {
                    if (accountsQueue.Count < 1 && runningRequests.Count < 1)
                    {
                        Stop(); break;
                    }

                    if (lastBatchTime.HasValue 
                        && (DateTime.Now - lastBatchTime.Value).TotalMinutes < MainWindow.Settings.AccountCreateBreakTime)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    if (accountsQueue.Count > 0)
                    {
                        if (MainWindow.Settings.AccountCreateQueueLimit < 1
                            || runningRequests.Count < MainWindow.Settings.AccountCreateQueueLimit)
                        {
                            var account = accountsQueue.Dequeue();
                            if (account != null)
                            {
                                // Add to requests
                                runningRequests.Add(account.RequestId);
                                Task.Run(() => Process(account), GetCancelToken());
                            }
                        }
                        else
                        {
                            lastBatchTime = DateTime.Now;
                        }
                    }
                }
            }

            StatusUpdate
                (CreationStatusCode.Complete);
        }

        private void Process(RSAccountForm account)
        {
            StatusUpdate(CreationStatusCode.Started, account);

            // Start creating account
            var result = CreateAccount(account).Result;
            if (result)
            {
                StatusUpdate
                    (CreationStatusCode.Created, account);
            }

            // Request complete
            lock (accountsQueueLock)
            {
                runningRequests.Remove(account.RequestId);
            }
        }

        private async Task<bool> CreateAccount(RSAccountForm account)
        {
            var captchaResult = string.Empty;
            if (account != null)
            {
                StatusUpdate
                    (CreationStatusCode.Updated, account, "Requesting captcha solve");

                var captchaId = await RsWebHelper.RequestSolveCaptcha();
                if (captchaId == string.Empty
                    || captchaId == "NO_GOOGLE_KEY")
                {
                    var message = (captchaId == "NO_GOOGLE_KEY")
                        ? "Error processing request on RuneScape page" : "An error occured requesting captcha solve";

                    StatusUpdate(CreationStatusCode.Updated, account, message);

                    return false;
                }

                for (int i = 0; i < 60; i++)
                {
                    StatusUpdate
                        (CreationStatusCode.Updated, account, "Awaiting captcha to be solved");

                    var response = await RsWebHelper.GetSolveResult(captchaId);
                    if (response != string.Empty
                        && !response.Contains("CAPCHA_NOT_READY") && !response.Contains("ERROR"))
                    {
                        captchaResult = response;
                        break;
                    }

                    Thread.Sleep(5 * 1000);
                }

                if (captchaResult != string.Empty)
                {
                    StatusUpdate
                        (CreationStatusCode.Updated, account, "Creating account...");

                    var response = await
                        RsWebHelper.PostCreateAccount(account, captchaResult);

                    var errorMessage = GetErrorMessage(response);
                    if (errorMessage != string.Empty)
                    {
                        StatusUpdate(CreationStatusCode.Updated, account, errorMessage);
                    }

                    var success = IsCreateSuccess(response);
                    if (!success && errorMessage == string.Empty)
                    {
                        StatusUpdate(CreationStatusCode.Updated, account, "An unknown error occured");
                        Util.Log(response);
                    }

                    return success;
                }
            }

            return false;
        }

        private string GetErrorMessage(string response)
        {
            if (response.Contains("This email address has already been used to play."))
            {
                return "An account with this email address already exists";
            }
            else if (response.Contains("Sorry, there was an error processing your request."))
            {
                return "Error processing your request (probably temporary block)";
            }
            else if (response.Contains("The email address is not valid."))
            {
                return "The email address is not valid";
            }
            else if (response.Contains("Your password contains invalid characters."))
            {
                return "Your password contains invalid characters";
            }
            else if (response.Contains("Your password is a common scam password."))
            {
                return "An error occured, your password is a common scam password";
            }

            return string.Empty;
        }

        private bool IsCreateSuccess(string response)
        {
            return response.Contains("You can now begin your adventure with your new account.")
                || response.Contains("uc-download-options__option")
                || response.Contains("Account Created");
        }

        protected virtual void StatusUpdate(CreationStatusCode statusCode, RSAccountForm account = null, string message = null)
        {
            OnStatusUpdate?.Invoke(statusCode, account, message);
        }
    }
}
