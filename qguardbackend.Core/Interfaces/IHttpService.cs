namespace qguardbackend.Core.Interfaces
{
    public interface IHttpService
    {
        Task<HttpResponseMessage> Post(string url, HttpContent content, IDictionary<string, string> headers, string token, string httpClientName, bool validateHeaders = true);
        Task<HttpResponseMessage> Post(string url, HttpContent content, IDictionary<string, string> headers, string httpClientName, bool validateHeaders = true);
        Task<HttpResponseMessage> Post(string url, FormUrlEncodedContent content, IDictionary<string, string> headers, string token, string httpClientName, bool validateHeaders = true);
        Task<HttpResponseMessage> Get(string url, IDictionary<string, string> headers, string token, string httpClientName, bool validateHeaders = true);
        Task<HttpResponseMessage> Get(string url, IDictionary<string, string> headers, string httpClientName, bool validateHeaders = true);
        Task<HttpResponseMessage> Put(string url, HttpContent content, IDictionary<string, string> headers, string token, string httpClientName, bool validateHeaders = true);
        Task<HttpResponseMessage> Put(string url, HttpContent content, IDictionary<string, string> headers, string httpClientName, bool validateHeaders = true);
    }
}