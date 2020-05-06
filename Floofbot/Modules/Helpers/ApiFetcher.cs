using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace Floofbot.Modules.Helpers
{
    class ApiFetcher
    {
        private static HttpClient httpClient;
        private static readonly int MAX_SUPPORTED_EMBED_FETCH_ATTEMPTS = 1;
        private static readonly List<string> SUPPORTED_EMBED_EXTENSIONS = new List<string>
        {
            ".jpg", ".gif", ".png"
        };

        static ApiFetcher()
        {
            httpClient = new HttpClient();
            httpClient.Timeout = new TimeSpan(0, 0, 0, 2); // 2 seconds
        }

        public static async Task<string> RequestEmbeddableUrlFromApi(string apiUrl, string key)
        {
            string url;
            for (int attempts = 0; attempts < MAX_SUPPORTED_EMBED_FETCH_ATTEMPTS; attempts++)
            {
                url = await RequestStringFromApi(apiUrl, key);
                if (!string.IsNullOrEmpty(url) && SUPPORTED_EMBED_EXTENSIONS.Any(ext => url.EndsWith(ext)))
                {
                    return url;
                }
            }
            return string.Empty;
        }

        public static async Task<string> RequestStringFromApi(string apiUrl, string key)
        {
            string json = await RequestSiteContentAsString(apiUrl);
            if (string.IsNullOrEmpty(json))
            {
                return string.Empty;
            }
            using (JsonDocument jsonDocument = JsonDocument.Parse(json))
            {
                try
                {
                    return jsonDocument.RootElement.GetProperty(key).ToString();
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);
                    return string.Empty;
                }
            }
        }

        public static async Task<string> RequestSiteContentAsString(string apiUrl)
        {
            try
            {
                return await httpClient.GetStringAsync(apiUrl);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return string.Empty;
            }
        }
    }
}
