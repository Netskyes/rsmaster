using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RSMaster.Services
{
    using UI;
    using Enums;
    using Helpers;
    using Utility;
    using Objects;
    using Interfaces;

    internal class AccountService : ServiceBase
    {
        public AccountServiceType AccountServiceType { get; set; } = AccountServiceType.Creation;
        public event CreationStatusUpdate OnStatusUpdate;
        public delegate void CreationStatusUpdate(ServiceStatusCode statusCode, IRuneScapeForm account = null, string message = null);

        private DateTime? lastBatchTime;
        private List<string> runningRequests;

        private readonly Queue<IRuneScapeForm> reqFormsQueue = new Queue<IRuneScapeForm>();
        private readonly object reqFormsQueueLock = new object();
        
        public AccountService()
        {
            Name = "Account Service";
            Description = "A service to create accounts";

            lastBatchTime = null;
            runningRequests = new List<string>();
        }

        public void QueueReqForm(IRuneScapeForm account)
        {
            account.RequestId = Guid.NewGuid().ToString();

            lock (reqFormsQueueLock)
            {
                reqFormsQueue.Enqueue(account);
            }
        }

        public IEnumerable<IRuneScapeForm> GetQueuedReqForm()
        {
            lock (reqFormsQueueLock)
            {
                foreach (var account in reqFormsQueue)
                {
                    yield return account;
                }
            }
        }

        protected override bool ServiceStartup()
        {
            var anyQueued = GetQueuedReqForm().Any();
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
                lock (reqFormsQueueLock)
                {
                    if (reqFormsQueue.Count < 1 && runningRequests.Count < 1)
                    {
                        Stop(); break;
                    }

                    if (lastBatchTime.HasValue 
                        && (DateTime.Now - lastBatchTime.Value).TotalMinutes < MainWindow.Settings.AccountServiceBreakTime)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    if (reqFormsQueue.Count > 0)
                    {
                        if (MainWindow.Settings.AccountServiceQueueLimit < 1
                            || runningRequests.Count < MainWindow.Settings.AccountServiceQueueLimit)
                        {
                            var account = reqFormsQueue.Dequeue();
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
                (ServiceStatusCode.Complete);
        }

        private void Process(IRuneScapeForm reqForm)
        {
            StatusUpdate
                (ServiceStatusCode.Started, reqForm);

            var result = false;
            RsWebHelper rsHelper = null;

            if (AccountServiceType == AccountServiceType.Creation)
            {
                var accountForm = (reqForm as RSAccountForm);
                rsHelper = new RsWebHelper(accountForm.ProxyName);

                result = CreateAccount(accountForm, rsHelper).Result;
            }
            else
            {
                rsHelper = new RsWebHelper();
                result = UnlockAccount(reqForm as RSRecoveryForm, rsHelper).Result;
            }

            if (result)
            {
                StatusUpdate(ServiceStatusCode.Success, reqForm);
            }

            // Request complete
            lock (reqFormsQueueLock)
            {
                runningRequests.Remove(reqForm.RequestId);
            }

            rsHelper?.Dispose();
        }

        private async Task<string> GetCaptchaSolveKey(IRuneScapeForm form, RsWebHelper rsHelper)
        {
            var captchaResult = string.Empty;
            StatusUpdate(ServiceStatusCode.Updated, form, "Requesting captcha solve");

            var googleKey = await rsHelper.GrabGoogleKey(form.RequestUrl);
            if (googleKey == string.Empty 
                || googleKey == "NO_GOOGLE_KEY")
            {
                StatusUpdate(ServiceStatusCode.Updated, form, "Error obtaining Google Key from requested page");
                return string.Empty;
            }

            var captchaId = await
                CaptchaHelper.RequestSolveCaptcha(googleKey, form.RequestUrl);
            if (captchaId == string.Empty)
            {
                StatusUpdate(ServiceStatusCode.Updated, form, "An error occured requesting captcha solve");
                return string.Empty;
            }

            for (int i = 0; i < 60; i++)
            {
                StatusUpdate
                    (ServiceStatusCode.Updated, form, "Waiting for captcha to be solved");

                var response = await CaptchaHelper.GetSolveResult(captchaId);
                if (response != string.Empty
                    && !response.Contains("CAPCHA_NOT_READY") && !response.Contains("ERROR"))
                {
                    captchaResult = response;
                    break;
                }

                Thread.Sleep(5 * 1000);
            }

            return captchaResult;
        }

        private string GetUnlockUrl(string content)
        {
            var regex = new Regex("redirectStr\\s+=\\s+'(.*)'");
            var match = regex.Match(content);

            return (match.Success) ? match.Groups[1].Value : string.Empty;
        }

        private string GetResetPassUrl(string content)
        {
            var regex = new Regex("(https://secure.runescape.com.*?code=.*?id=.*?)\\\">reset");
            var match = regex.Match(content);

            return (match.Success) ? match.Groups[1].Value : string.Empty;
        }

        private string GetAccountId(string content)
        {
            var regex = new Regex(@"id\/(.*?)/");
            var match = regex.Match(content);

            return (match.Success) ? match.Groups[1].Value : string.Empty;
        }

        private async Task<bool> UnlockAccount(RSRecoveryForm recovery, RsWebHelper rsHelper)
        {
            var captchaRes = await GetCaptchaSolveKey(recovery, rsHelper);
            if (captchaRes == string.Empty)
                return false;

            recovery.CaptchaSolve = captchaRes;
            StatusUpdate(ServiceStatusCode.Updated, recovery, "Requesting account recovery");

            var response = await
                rsHelper.PostRequest(recovery);

            if (!response.Contains("Account Identified"))
            {
                StatusUpdate(ServiceStatusCode.Updated, recovery, "An error occured requesting recovery page");
                return false;
            }

            var requestTime = DateTime.Now;
            var redirectUrl = GetUnlockUrl(response);
            response = (await rsHelper.GetRequest(redirectUrl)).message;

            if (!response.Contains("Proceeded to e-mail confirmation screen"))
            {
                StatusUpdate(ServiceStatusCode.Updated, recovery, "An error occured requesting account recovery");
                return false;
            }
            

            StatusUpdate(ServiceStatusCode.Updated, recovery, "Connecting to mail provider");

            var mail = new MailHelper(MailProvider.Gmail);
            if (!mail.Connect
                (recovery.MasterEmail ?? recovery.Email, recovery.EmailPassword))
            {
                StatusUpdate(ServiceStatusCode.Updated, recovery, "Failed to connect to mail provider");
                return false;
            }

            StatusUpdate
                (ServiceStatusCode.Updated, recovery, "Searching for activation mail");

            var messageContent = string.Empty;
            for (int i = 0; i < 30; i++)
            {
                messageContent = mail.FindMailBySubjToHtml("Reset your Jagex password", requestTime);
                if (messageContent != string.Empty)
                {
                    break;
                }

                Thread.Sleep(5*1000);
            }

            if (messageContent == string.Empty)
            {
                StatusUpdate(ServiceStatusCode.Updated, recovery, "Unable to find recovery mail");
                return false;
            }

            var resetPassUrl = GetResetPassUrl(messageContent);
            if (resetPassUrl == string.Empty)
            {
                StatusUpdate(ServiceStatusCode.Updated, recovery, "Error obtaining Password Reset link from email");
                return false;
            }

            var passResetRequest = await rsHelper.GetRequest(resetPassUrl);
            if (passResetRequest.response == null)
            {
                StatusUpdate(ServiceStatusCode.Updated, recovery, "Error obtaining Password Reset page");
                return false;
            }

            var passResetPage = passResetRequest.response.RequestMessage.RequestUri;
            var accountId = GetAccountId(passResetPage.AbsoluteUri);

            recovery.AccountId = accountId;
            recovery.RequestUrl = passResetPage.AbsoluteUri;
            recovery.RecoveryStage = AccRecoveryStage.Complete;

            response = await rsHelper.PostRequest(recovery);
            return response.Contains("Successfully set a new password and completed the process");
        }

        private async Task<bool> CreateAccount(RSAccountForm account, RsWebHelper rsHelper)
        {
            if (account != null)
            {
                var captchaRes = await GetCaptchaSolveKey(account, rsHelper);
                if (captchaRes != string.Empty)
                {
                    account.CaptchaSolve = captchaRes;

                    StatusUpdate
                        (ServiceStatusCode.Updated, account, "Creating account...");

                    var response = await 
                        rsHelper.PostRequest(account);

                    var errorMessage = GetErrorMessage(response);
                    if (errorMessage != string.Empty)
                    {
                        StatusUpdate(ServiceStatusCode.Updated, account, errorMessage);
                    }

                    var success = IsCreateSuccess(response);
                    if (!success && errorMessage == string.Empty)
                    {
                        StatusUpdate(ServiceStatusCode.Updated, account, "An unknown error occured");
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

        protected virtual void StatusUpdate(ServiceStatusCode statusCode, IRuneScapeForm account = null, string message = null)
        {
            OnStatusUpdate?.Invoke(statusCode, account, message);
        }
    }
}
