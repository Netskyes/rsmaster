using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RSMaster.Helpers
{
    using UI;
    using UI.Models;
    using RuneScape.Models;
    using Utility;

    internal class RsWebHelper
    {
        private HttpHelper httpHelper;
        private readonly string CreateAccountUrl = "https://secure.runescape.com/m=account-creation/create_account";

        public RsWebHelper()
        {
            var settings = MainWindow.Settings;

            if (settings.CreateAccountUseHttpProxy 
                && !string.IsNullOrEmpty(settings.CreateAccountHttpProxy))
            {
                var proxy = DataProvider.GetModels<ProxyModel>("proxies").FirstOrDefault
                    (x => x.Alias == settings.CreateAccountHttpProxy);
                if (proxy != null)
                {
                    int.TryParse(proxy.Port, out int port);
                    httpHelper = new HttpHelper($"{proxy.Type.ToLower()}://{proxy.Host}", port, proxy.Username, proxy.Password);
                }
            }
            
            if (httpHelper is null)
                httpHelper = new HttpHelper();

            // Browser imitation
            httpHelper.InitRsWebSupport();
        }

        public async Task<string> PostCreateAccount(RSAccountForm account, string captchaResult)
        {
            var requestForm = account.Build(captchaResult);
            return await httpHelper.PostRequest
                (CreateAccountUrl, new FormUrlEncodedContent(requestForm));
        }

        public async Task<string> RequestSolveCaptcha()
        {
            var googleKey = await GrabGoogleKey();
            if (googleKey == "")
            {
                return "NO_GOOGLE_KEY";
            }

            // Logic for 2captcha or Anticaptcha
            return await CaptchaTwoHelper.RequestSolveCaptcha(googleKey, CreateAccountUrl);
        }

        public async Task<string> GetSolveResult(string captchaId)
        {
            // Logic for 2captcha or Anticaptcha
            return await CaptchaTwoHelper.GetSolveResult(captchaId);
        }

        public static async Task<string> GetCaptchaBalance()
        {
            // Logic for 2captcha or Anticaptcha
            return await CaptchaTwoHelper.GetCaptchaBalance();
        }

        public async Task<string> GrabGoogleKey()
        {
            var response = await Util.GetRequest(CreateAccountUrl);
            var regex = new Regex("'sitekey'\\s+:\\s+'(.*)'");
            var match = regex.Match(response);

            return (match.Success) ? match.Groups[1].Value : string.Empty;
        }
    }
}
