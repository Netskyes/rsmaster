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
    using Data;
    using Interfaces;
    using UI.Models;

    internal class RsWebHelper : IDisposable
    {
        private readonly HttpHelper httpHelper;

        public RsWebHelper(string proxyName = null)
        {
            var settings = MainWindow.Settings;

            if (settings.CreateAccountUseProxy 
                && (!string.IsNullOrEmpty(settings.CreateAccountProxyName) 
                || (!string.IsNullOrEmpty(proxyName) && settings.CreateAccountUseImportedProxy)))
            {
                var proxy = DataProvider.GetModels<ProxyModel>("proxies").FirstOrDefault
                    (x => x.Alias == (proxyName ?? settings.CreateAccountProxyName));
                if (proxy != null)
                {
                    int.TryParse(proxy.Port, out int port);
                    var socksProxy = proxy.Type.Equals("SOCKS");
                    var host = (socksProxy) ? proxy.Host : $"{proxy.Type.ToLower()}://{proxy.Host}";

                    httpHelper = new HttpHelper
                        (socksProxy, host, port, proxy.Username, proxy.Password);
                }
            }
            
            if (httpHelper is null)
                httpHelper = new HttpHelper();

            // Browser imitation
            httpHelper.InitRsWebSupport();
        }

        public async Task<string> PostRequest(IRuneScapeForm requestForm)
        {
            var webForms = requestForm.Build();
            return await httpHelper.PostRequest(requestForm.RequestUrl, new FormUrlEncodedContent(webForms));
        }

        public async Task<(string message, HttpResponseMessage response)> GetRequest(string requestUri)
        {
            return await httpHelper.GetRequest(requestUri);
        }

        public void Dispose()
        {
            if (httpHelper != null) httpHelper.Dispose();
        }
    }
}
