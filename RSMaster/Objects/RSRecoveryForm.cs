using System;
using System.Collections.Generic;

namespace RSMaster.Objects
{
    using Interfaces;

    internal enum AccRecoveryStage
    {
        Request,
        Complete
    }

    internal class RSRecoveryForm : IRuneScapeForm
    {
        #region Fields

        private string requestUrl = "https://secure.runescape.com/m=accountappeal/passwordrecovery?webAppeal=true";

        #endregion

        public AccRecoveryStage RecoveryStage { get; set; } = AccRecoveryStage.Request;

        public string RequestUrl
        {
            get => requestUrl;
            set => requestUrl = value;
        }

        public string RequestId { get; set; }
        public string CaptchaSolve { get; set; }

        public string Email { get; set; }
        public string EmailPassword { get; set; }
        public string MasterEmail { get; set; }
        public string NewPassword { get; set; }
        public string Provider { get; set; }
        public string AccountId { get; set; }

        public Dictionary<string, string> Build()
        {
            if (RecoveryStage == AccRecoveryStage.Request)
            {
                return new Dictionary<string, string>()
                {
                    { "email", Email },
                    { "password-recovery-submit", "password-recovery" },
                    { "g-recaptcha-response", CaptchaSolve }
                };
            }

            return new Dictionary<string, string>()
            {
                { "password", NewPassword },
                { "confirm", NewPassword },
                { "submit", "Change Password" },
                { "account_id", AccountId }
            };
        }
    }
}
