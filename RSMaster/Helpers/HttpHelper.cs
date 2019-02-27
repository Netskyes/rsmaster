using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Raindropz;

namespace RSMaster.Helpers
{
    using Utility;

    public class HttpHelper
    {
        private HttpClient client;
        private HttpClientHandler clientHandler;
        private CookieContainer cookies;

        public void SetBearerToken(string token)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        public void InitRsWebSupport()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            cookies = new CookieContainer();

            if (clientHandler is null)
                clientHandler = new HttpClientHandler();

            clientHandler.UseCookies = true;
            clientHandler.CookieContainer = cookies;
            clientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            client = new HttpClient(clientHandler);
            client.DefaultRequestHeaders.Add("Host", "secure.runescape.com");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36");
        }

        public HttpHelper()
        {
            client = new HttpClient();
        }

        public HttpHelper(bool socksProxy, string proxyHost, int proxyPort, string proxyUser = null, string proxyPass = null)
        {
            clientHandler = new HttpClientHandler();
            IWebProxy proxy = null;

            if (socksProxy)
            {
                proxy = new HttpToSocks5Proxy(proxyHost, proxyPort);
            }
            else
            {
                proxy = new WebProxy
                {
                    Address = new Uri($"{proxyHost}:{proxyPort}"),
                    BypassProxyOnLocal = false,
                    UseDefaultCredentials = false
                };
            }

            if (!Util.AnyStringNullOrEmpty(proxyUser, proxyPass))
            {
                proxy.Credentials = new NetworkCredential(proxyUser, proxyPass);
                clientHandler.PreAuthenticate = true;
                clientHandler.UseDefaultCredentials = false;
            }

            clientHandler.Proxy = proxy;
            // Client using proxy
            client = new HttpClient(clientHandler);
        }

        public async Task<string> GetRequest(string requestUri)
        {
            try
            {
                var response = await client.GetAsync(requestUri);
                return await
                    response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                Util.LogException(e);
                return string.Empty;
            }
        }

        public async Task<string> PostRequest(string requestUri, HttpContent content)
        {
            try
            {
                var request = await client.PostAsync(requestUri, content);
                return await
                    request.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                Util.LogException(e);
                return string.Empty;
            }
        }
    }
}
