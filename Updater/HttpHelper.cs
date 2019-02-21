using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Updater
{
    public static class HttpHelper
    {
        public static HttpClient Client { get; set; }

        static HttpHelper()
        {
            Client = new HttpClient();
        }

        public static void SetBearerToken(string token)
        {
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        public static async Task<string> GetRequest(string requestUri)
        {
            try
            {
                var response = await Client.GetAsync(requestUri);
                return await
                    response.Content.ReadAsStringAsync();
            }
            catch
            {
                return string.Empty;
            }
        }

        public static async Task<string> PostRequest(string requestUri, HttpContent content)
        {
            try
            {
                var request = await Client.PostAsync(requestUri, content);
                return await
                    request.Content.ReadAsStringAsync();
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string GetHeader(string name, HttpContent content)
        {
            try
            {
                return content.Headers.FirstOrDefault(h => h.Key.Equals(name)).Value.First();
            }
            catch
            {
                return null;
            }
        }
    }
}
