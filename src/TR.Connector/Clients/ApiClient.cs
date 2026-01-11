using System.Net.Http.Headers;
using TR.Connector.Interfaces;

namespace TR.Connector.Clients
{
    internal class ApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ISerializer _serializer;
        private bool _disposed = false;

        public ApiClient(string baseUrl, ISerializer serializer)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            _serializer = serializer;
        }

        public T Post<T>(string endpoint, object body) where T : class
        {
            var content = _serializer.CreateContent(body);
            var response = _httpClient.PostAsync(endpoint, content).Result;

            EnsureSuccessResponse(response, endpoint);

            var responseString = response.Content.ReadAsStringAsync().Result;
            return _serializer.Deserialize<T>(responseString);
        }

        public T Get<T>(string endpoint) where T : class
        {
            var response = _httpClient.GetAsync(endpoint).Result;

            EnsureSuccessResponse(response, endpoint);

            var responseString = response.Content.ReadAsStringAsync().Result;
            return _serializer.Deserialize<T>(responseString);
        }

        public void Put(string endpoint, object? body = null)
        {
            var content = body != null
                ? _serializer.CreateContent(body)
                : null;

            var response = _httpClient.PutAsync(endpoint, content).Result;
            EnsureSuccessResponse(response, endpoint);
        }

        public void Delete(string endpoint)
        {
            var response = _httpClient.DeleteAsync(endpoint).Result;
            EnsureSuccessResponse(response, endpoint);
        }

        public void SetAccessToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private void EnsureSuccessResponse(HttpResponseMessage response, string endpoint)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = response.Content.ReadAsStringAsync().Result;
                var message = $"Request to '{endpoint}' failed with status code {(int)response.StatusCode} ({response.StatusCode})";

                if (!string.IsNullOrEmpty(errorContent))
                {
                    message += $". Response: {errorContent}";
                }

                throw new HttpRequestException(message, null, response.StatusCode);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}