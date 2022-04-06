using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketSharp.Net
{
    public class HttpVersion
    {
        public const string HTTP1 = "1.1";
        public const string HTTP2 = "2.0";
    }
    public class Http2Client : WinHttpHandler
    {
        private string _version;
        public Http2Client(string version)
        {
            _version = version;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Version = new Version(_version);
            return base.SendAsync(request, cancellationToken);
        }

        public async Task<Bitmap> LoadImage(string url)
        {
            Bitmap image = null;
            using (HttpClient client = new HttpClient(this))
            {
                client.Timeout = TimeSpan.FromMilliseconds(5000);
                try
                {
                    HttpResponseMessage message = await client.GetAsync(url);
                    using (Stream stream = await message.Content.ReadAsStreamAsync())
                    {
                        image = new Bitmap(stream);
                    }
                }
                catch
                {
                }
            }
            return image;
        }

        public async Task<string> SendAsync(string url, HttpMethod method, string data, string auth)
        {
            string response = null;
            using (HttpClient client = new HttpClient(this))
            {
                client.Timeout = TimeSpan.FromMilliseconds(5000);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth);

                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(method, url);

                    if (data != null)
                    {
                        StringContent content = new StringContent(data, Encoding.UTF8, "application/json");
                        request.Content = content;
                    }

                    HttpResponseMessage message = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                    response = await message.Content.ReadAsStringAsync();
                }
                catch
                {
                }
            }
            return response;
        }

        public async Task<string> PutAsync(string url, string data, string auth)
        {
            string response = null;
            using (HttpClient client = new HttpClient(this))
            {
                client.Timeout = TimeSpan.FromMilliseconds(5000);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth);

                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, url);

                    if (data != null)
                    {
                        StringContent content = new StringContent(data, Encoding.UTF8, "application/json");
                        request.Content = content;
                    }

                        HttpResponseMessage message = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                    response = await message.Content.ReadAsStringAsync();
                }
                catch
                {
                }
            }
            return response;
        }

        public async Task<string> DeleteAsync(string url, string data, string auth)
        {
            string response = null;
            using (HttpClient client = new HttpClient(this))
            {
                client.Timeout = TimeSpan.FromMilliseconds(5000);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth);

                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, url);

                    if (data != null)
                    {
                        StringContent content = new StringContent(data, Encoding.UTF8, "application/json");
                        request.Content = content;
                    }

                    HttpResponseMessage message = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                    response = await message.Content.ReadAsStringAsync();
                }
                catch
                {
                }
            }
            return response;
        }

        public async Task<string> GetAsync(string url, Dictionary<string, string> header, string auth)
        {
            string response = null;
            using (HttpClient client = new HttpClient(this))
            {
                client.Timeout = TimeSpan.FromMilliseconds(5000);

                if (auth != null)
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth);

                if (header != null)
                {
                    foreach (KeyValuePair<string, string> kvp in header)
                        client.DefaultRequestHeaders.TryAddWithoutValidation(kvp.Key, kvp.Value);
                }

                try
                {
                    HttpResponseMessage message = await client.GetAsync(url);
                    response =  await message.Content.ReadAsStringAsync();
                }
                catch
                {
                }
            }
            return response;
        }

        public async Task<string> GetAsync(string url, Dictionary<string, string> header)
        {
            return await GetAsync(url, header, null);
        }

        public async Task<string> GetAsync(string url)
        {
            return await GetAsync(url, null, null);
        }

        public async Task<string> PostAsync(string url, string data)
        {
            return await PostAsync(url, null, null, data);
        }


        public async Task<string> PostAsync(string url, Dictionary<string, string> header, string auth, string data)
        {
            string response = null;
            using (HttpClient client = new HttpClient(this))
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (auth != null)
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth);

                if (header != null)
                {
                    foreach (KeyValuePair<string, string> kvp in header)
                        client.DefaultRequestHeaders.TryAddWithoutValidation(kvp.Key, kvp.Value);
                }

                StringContent content = new StringContent(data, Encoding.UTF8, "application/json") ;
                HttpResponseMessage message = await client.PostAsync(url, content);
                response = await message.Content.ReadAsStringAsync();
                return response;
            }
        }

        public async Task<string> PostUrlEncodeAsync(string url, string data, string auth)
        {
            string response = null;
            using (HttpClient client = new HttpClient(this))
            {
                if (auth != null)
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth);

                StringContent content = new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded");

                HttpResponseMessage message = await client.PostAsync(url, content);
                response = await message.Content.ReadAsStringAsync();
                return response;
            }
        }
    }
}
