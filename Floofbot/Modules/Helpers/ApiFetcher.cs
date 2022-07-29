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
        private static readonly HttpClient HTTP_CLIENT;
        private static readonly int MAX_SUPPORTED_EMBED_FETCH_ATTEMPTS = 1;
        private static readonly List<string> SUPPORTED_EMBED_EXTENSIONS = new List<string>
        {
            ".jpg", ".gif", ".png"
        };

        static ApiFetcher()
        {
            HTTP_CLIENT = new HttpClient();
            HTTP_CLIENT.Timeout = new TimeSpan(0, 0, 0, 2); // 2 seconds
        }

        public static async Task<string> RequestEmbeddableUrlFromApi(string apiUrl, string key)
        {
            var url = string.Empty;
            
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
            var json = await RequestSiteContentAsString(apiUrl);
            
            if (string.IsNullOrEmpty(json))
            {
                return string.Empty;
            }
            
            using (var jsonDocument = JsonDocument.Parse(json))
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
                return await HTTP_CLIENT.GetStringAsync(apiUrl);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                
                return string.Empty;
            }
        }
    }
}
