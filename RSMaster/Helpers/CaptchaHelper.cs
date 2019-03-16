using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSMaster.Helpers
{
    using UI;
    using Utility;

    internal static class CaptchaHelper
    {
        public static string CaptchaApiKey
        {
            get => MainWindow.Settings?.CaptchaApiKey ?? string.Empty;
        }

        public static async Task<string> RequestSolveCaptcha(string googleKey, string pageUrl)
        {
            var requestUrl = string.Format("http://2captcha.com/in.php?key={0}&method=userrecaptcha&googlekey={1}&pageurl={2}",
                CaptchaApiKey, googleKey, pageUrl);

            var response = await Util.GetRequest(requestUrl);
            var result = response.Split('|');

            return (result.Count() > 1 && result[0] == "OK") ? result[1] : string.Empty;
        }

        public static async Task<string> GetSolveResult(string captchaId)
        {
            var requestUrl = $"http://2captcha.com/res.php?key={CaptchaApiKey}&action=get&id={captchaId}";
            var response = await Util.GetRequest(requestUrl);
            var result = response.Split('|');

            return (result.Count() > 1 && result[0] == "OK") ? result[1] : response;
        }

        public static async Task<string> GetCaptchaBalance()
        {
            var requestUrl = $"http://2captcha.com/res.php?key={CaptchaApiKey}&action=getbalance";
            return await Util.GetRequest(requestUrl);
        }
    }
}
