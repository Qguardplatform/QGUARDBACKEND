using System.Net.Http.Headers;
using qguardbackend.Core.Interfaces;

namespace SISService.BoilerPlate.Service.Implementations
{
    public class HttpService : IHttpService
    {
        public static readonly string timeoutDuration = "180";
        private readonly IHttpClientFactory _clientFactory;

        public HttpService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<HttpResponseMessage> Get(string url, IDictionary<string, string> headers, string token, string httpClientName, bool validateHeaders = true)
        {
            var client = _clientFactory.CreateClient(httpClientName);
            int timeout = int.Parse(timeoutDuration);
            client.Timeout = TimeSpan.FromSeconds(timeout);
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            foreach (var tm in headers)
            {
                if (validateHeaders)
                    client.DefaultRequestHeaders.Add(tm.Key, tm.Value);
                else
                    client.DefaultRequestHeaders.TryAddWithoutValidation(tm.Key, tm.Value);
            }
            return await client.GetAsync(url);
        }

        public async Task<HttpResponseMessage> Get(string url, IDictionary<string, string> headers, string httpClientName, bool validateHeaders = true)
        {
            var client = _clientFactory.CreateClient(httpClientName);
            int timeout = int.Parse(timeoutDuration);
            client.Timeout = TimeSpan.FromSeconds(timeout);

            foreach (var tm in headers)
            {
                if (validateHeaders)
                    client.DefaultRequestHeaders.Add(tm.Key, tm.Value);
                else
                    client.DefaultRequestHeaders.TryAddWithoutValidation(tm.Key, tm.Value);
            }

            return await client.GetAsync(url);
        }

        public async Task<HttpResponseMessage> Post(string url, HttpContent content, IDictionary<string, string> headers, string token, string httpClientName, bool validateHeaders = true)
        {
            var client = _clientFactory.CreateClient(httpClientName);
            int timeout = int.Parse(timeoutDuration);
            client.Timeout = TimeSpan.FromSeconds(timeout);
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            if (headers != null)
            {
                foreach (var tm in headers)
                {
                    if (validateHeaders)
                        client.DefaultRequestHeaders.Add(tm.Key, tm.Value);
                    else
                        client.DefaultRequestHeaders.TryAddWithoutValidation(tm.Key, tm.Value);
                }

            }
            return await client.PostAsync(url, content);
        }

        public async Task<HttpResponseMessage> Post(string url, HttpContent content, IDictionary<string, string> headers, string httpClientName, bool validateHeaders = true)
        {
            var client = _clientFactory.CreateClient(httpClientName);
            int timeout = int.Parse(timeoutDuration);
            client.Timeout = TimeSpan.FromSeconds(timeout);
            if (headers != null)
            {
                foreach (var tm in headers)
                {
                    if (validateHeaders)
                        client.DefaultRequestHeaders.Add(tm.Key, tm.Value);
                    else
                        client.DefaultRequestHeaders.TryAddWithoutValidation(tm.Key, tm.Value);
                }

            }
            return await client.PostAsync(url, content);
        }

        public async Task<HttpResponseMessage> Post(string url, FormUrlEncodedContent content, IDictionary<string, string> headers, string token, string httpClientName, bool validateHeaders = true)
        {
            var client = _clientFactory.CreateClient(httpClientName);
            int timeout = int.Parse(timeoutDuration);
            client.Timeout = TimeSpan.FromSeconds(timeout);
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            if (headers != null)
            {
                foreach (var tm in headers)
                {
                    if (validateHeaders)
                        client.DefaultRequestHeaders.Add(tm.Key, tm.Value);
                    else
                        client.DefaultRequestHeaders.TryAddWithoutValidation(tm.Key, tm.Value);
                }
            }
            return await client.PostAsync(url, content);
        }

        public async Task<HttpResponseMessage> Put(string url, HttpContent content, IDictionary<string, string> headers, string token, string httpClientName, bool validateHeaders = true)
        {
            var client = _clientFactory.CreateClient(httpClientName);
            int timeout = int.Parse(timeoutDuration);
            client.Timeout = TimeSpan.FromSeconds(timeout);
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            foreach (var tm in headers)
            {
                if (validateHeaders)
                    client.DefaultRequestHeaders.Add(tm.Key, tm.Value);
                else
                    client.DefaultRequestHeaders.TryAddWithoutValidation(tm.Key, tm.Value);
            }
            return await client.PutAsync(url, content);
        }

        public async Task<HttpResponseMessage> Put(string url, HttpContent content, IDictionary<string, string> headers, string httpClientName, bool validateHeaders = true)
        {
            var client = _clientFactory.CreateClient(httpClientName);
            int timeout = int.Parse(timeoutDuration);
            client.Timeout = TimeSpan.FromSeconds(timeout);
            foreach (var tm in headers)
            {
                if (validateHeaders)
                    client.DefaultRequestHeaders.Add(tm.Key, tm.Value);
                else
                    client.DefaultRequestHeaders.TryAddWithoutValidation(tm.Key, tm.Value);
            }
            return await client.PutAsync(url, content);
        }
    }
}